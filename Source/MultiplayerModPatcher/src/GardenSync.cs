using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using Il2Cpp;
using Il2CppMonomiPark.SlimeRancher;
using Il2CppMonomiPark.SlimeRancher.DataModel;
using UnityEngine;

namespace MultiplayerModPatcher;

/// <summary>
/// Plants on the other player's side what was planted here — with the grower definition the crop
/// actually grows from.
///
/// Ranching Together resolves that definition by scanning the raw storage behind the game's grower
/// translation and taking the first entry whose primary resource is the crop:
///
/// <code>
/// landPlotModel.resourceGrowerDefinition = translation._resourceGrowerTranslation
///     .RawLookupDictionary._entries
///     .FirstOrDefault(x =&gt; x.value._primaryResourceType == actor).value;   // SR2MP 0.3.8
/// </code>
///
/// Two things go wrong there. Every crop has two definitions — the normal patch and the deluxe one —
/// and which of them comes first is whatever order the hash table happens to store them in, so a
/// plot can end up remembering the wrong patch. And <c>_entries</c> is the raw array behind the
/// dictionary: the slots past the last insertion hold no value, so a crop with no matching
/// definition — a modded one the other player has and this game does not — walks into them and
/// throws, which costs the whole update rather than the grower alone.
///
/// The replacement plants the crop first and then reads the definition back off the patch that was
/// planted, which is the game's own answer and needs no guess about deluxe. Only a plot too far away
/// to be loaded falls back to a lookup, and that one picks by patch prefab rather than by hash
/// order.
/// </summary>
internal static class GardenSync
{
    /// <summary>The type id Ranching Together sends when a plot is being uprooted rather than planted.</summary>
    private const int Uprooted = 9;

    private static FieldInfo _packetId;
    private static FieldInfo _packetActorType;
    private static PropertyInfo _handlingPacket;

    public static bool Install(HarmonyLib.Harmony harmony)
    {
        if (!SR2MPBridge.Available) return false;

        // The handler inherits an abstract Handle and overrides it, so asking by name alone is
        // ambiguous: the one to patch is the override declared on the handler itself.
        MethodInfo handle = SR2MPBridge.Type("SR2MP.Handlers.LandPlots.GardenPlantHandler")
            ?.GetMethods(SR2MPBridge.Any)
            .FirstOrDefault(method => method.Name == "Handle" && method.GetParameters().Length == 2
                                      && !method.IsAbstract);

        Type packet = SR2MPBridge.Type("SR2MP.Packets.LandPlots.GardenPlantPacket");
        _packetId = packet?.GetField("ID", SR2MPBridge.Any);
        _packetActorType = packet?.GetField("ActorType", SR2MPBridge.Any);
        _handlingPacket = SR2MPBridge.Type("SR2MP.GlobalVariables")
            ?.GetProperty("HandlingPacket", SR2MPBridge.Any);

        if (handle == null || _packetId == null || _packetActorType == null || _handlingPacket == null)
        {
            Main.Log.Warning("Gardens left as they are: Ranching Together's land plot code moved.");
            return false;
        }

        harmony.Patch(handle, prefix: new HarmonyMethod(
            typeof(GardenSync).GetMethod(nameof(BeforePlant), BindingFlags.NonPublic | BindingFlags.Static)));

        Main.Log.Msg("Gardens keep the grower definition the crop actually grows from.");
        return true;
    }

    /// <summary>Applies a garden update in place of Ranching Together's own handler.</summary>
    private static bool BeforePlant(object packet)
    {
        try
        {
            if (packet == null) return true;

            string id = _packetId.GetValue(packet) as string;
            int actorType = Convert.ToInt32(_packetActorType.GetValue(packet));

            Il2CppSystem.Collections.Generic.Dictionary<string, LandPlotModel> plots =
                SRSingleton<SceneContext>.Instance?.GameModel?.landPlots;
            if (plots == null || string.IsNullOrEmpty(id)) return true;

            if (!plots.TryGetValue(id, out LandPlotModel plot) || plot == null)
            {
                Main.Log.Warning($"A garden update named plot {id}, which this game does not know.");
                return false;
            }

            if (actorType == Uprooted)
            {
                Uproot(plot);
                return false;
            }

            IdentifiableType crop = Crop(actorType);
            if (crop == null)
            {
                Main.Log.Warning($"A garden was planted with type {actorType}, which this game cannot " +
                                 "name: the other player is not running the same mods.");
                return false;
            }

            Plant(plot, crop);
            return false;
        }
        catch (Exception e)
        {
            Main.Log.Warning($"Garden update left to Ranching Together: {e.Message}");
            return true;
        }
    }

    private static IdentifiableType Crop(int actorType)
    {
        IDictionary<int, IdentifiableType> types = SR2MPBridge.ActorTypes();
        return types != null && types.TryGetValue(actorType, out IdentifiableType crop) ? crop : null;
    }

    private static void Uproot(LandPlotModel plot)
    {
        plot.resourceGrowerDefinition = null;
        if (!plot.gameObj) return;

        LandPlot landPlot = plot.gameObj.GetComponentInChildren<LandPlot>(true);
        if (landPlot == null) return;

        Locally(() => landPlot.DestroyAttached());
    }

    private static void Plant(LandPlotModel plot, IdentifiableType crop)
    {
        bool deluxe = plot.upgrades != null && plot.upgrades.Contains(LandPlot.Upgrade.DELUXE_GARDEN);

        // A plot nobody is close enough to load has no garden to plant in: the model is the whole
        // state, and it is what the plot rebuilds itself from when a player walks back to it.
        if (!plot.gameObj)
        {
            plot.resourceGrowerDefinition = Grower(crop, deluxe);
            return;
        }

        GardenCatcher catcher = plot.gameObj.GetComponentInChildren<GardenCatcher>(true);
        if (catcher == null) return;

        if (!catcher.CanAccept(crop))
        {
            Main.Log.Warning($"A garden was planted with {crop.name}, which this game's gardens do not accept.");
            return;
        }

        Locally(() => catcher.Plant(crop, isReplacement: true));

        plot.resourceGrowerDefinition = Planted(plot) ?? Grower(crop, deluxe);
    }

    /// <summary>The definition the patch that was just planted grows from — the game's own answer.</summary>
    private static ResourceGrowerDefinition Planted(LandPlotModel plot)
    {
        SpawnResource spawner = plot.gameObj.GetComponentInChildren<SpawnResource>(true);
        return spawner == null ? null : spawner._resourceGrowerDefinition;
    }

    /// <summary>
    /// The definition for a crop in a plot that is not loaded, chosen by the patch prefab a garden
    /// would plant rather than by the order a hash table stores its entries in.
    /// </summary>
    private static ResourceGrowerDefinition Grower(IdentifiableType crop, bool deluxe)
    {
        SaveReferenceTranslation translation =
            GameContext.Instance?.AutoSaveDirector?._saveReferenceTranslation;

        Il2CppSystem.Collections.Generic.Dictionary<string, ResourceGrowerDefinition> growers =
            translation?._resourceGrowerTranslation?.RawLookupDictionary;
        if (growers == null) return null;

        GameObject wanted = Patch(crop, deluxe);
        ResourceGrowerDefinition fallback = null;

        Il2CppSystem.Collections.Generic.Dictionary<string, ResourceGrowerDefinition>.Enumerator entries =
            growers.GetEnumerator();

        while (entries.MoveNext())
        {
            ResourceGrowerDefinition grower = entries.Current.value;
            if (grower == null || grower._primaryResourceType != crop) continue;

            if (wanted != null && grower._prefab == wanted) return grower;
            fallback ??= grower;
        }

        return fallback;
    }

    /// <summary>The patch prefab a garden plants for a crop, normal or deluxe.</summary>
    private static GameObject Patch(IdentifiableType crop, bool deluxe)
    {
        LookupDirector director = LookupDirector.GetIfReady();
        GameObject garden = director == null ? null : director.GetPlotPrefab(LandPlot.Id.GARDEN);
        GardenCatcher catcher = garden == null ? null : garden.GetComponentInChildren<GardenCatcher>(true);
        if (catcher?.Plantable == null) return null;

        foreach (GardenCatcher.PlantSlot slot in catcher.Plantable)
        {
            if (slot == null || slot.IdentType != crop) continue;
            return deluxe && slot.DeluxePlantedPrefab != null ? slot.DeluxePlantedPrefab : slot.PlantedPrefab;
        }

        return null;
    }

    /// <summary>
    /// Runs something the way Ranching Together runs its own handlers: with the flag that stops its
    /// patches from sending back what was just received.
    /// </summary>
    private static void Locally(Action action)
    {
        _handlingPacket.SetValue(null, true);
        try
        {
            action();
        }
        finally
        {
            _handlingPacket.SetValue(null, false);
        }
    }
}
