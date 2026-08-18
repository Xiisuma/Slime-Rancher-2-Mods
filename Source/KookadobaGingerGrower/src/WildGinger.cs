using System.Collections.Generic;
using HarmonyLib;
using Il2Cpp;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using UnityEngine;

namespace KookadobaGingerGrower;

/// <summary>
/// Lets Gilded Ginger come up in the wild, among the pogo fruit.
///
/// The game grows it at authored patch nodes only, a handful per zone, which is why a rancher can
/// play for hours without seeing one. Here it joins the pogo fruit trees: those pick what to spawn
/// from a weighted table, so ginger is added to the table as a thin share of it — rare enough that
/// finding one is still worth something.
///
/// Every other patch is left alone, and so is anything growing inside a plot, so a garden still
/// yields exactly what was planted in it.
/// </summary>
public static class WildGinger
{
    /// <summary>Share of a tree's table the ginger takes: about one pogo fruit in fifty.</summary>
    private const float Share = 0.02f;

    /// <summary>Reference-id fragment of the crop the ginger hides among.</summary>
    private const string Host = "pogo";

    /// <summary>How many patches are worth naming in the log before it turns into noise.</summary>
    private const int LocationLogLimit = 10;

    /// <summary>
    /// Grower definitions are shared assets, so each one is copied once and the copy is what the wild
    /// beds are pointed at. Editing the originals would put ginger in the gardens as well.
    /// </summary>
    private static readonly Dictionary<string, ResourceGrowerDefinition> Sown = new();

    private static int _patched;

    /// <summary>Drops the per-save counter when another save is loaded.</summary>
    public static void Reset() => _patched = 0;

    /// <summary>Adds ginger to a wild patch that is starting up.</summary>
    internal static void Inject(SpawnResource spawner)
    {
        if (GingerCrop.Ginger == null || spawner == null) return;

        // A plot grows what the rancher planted, nothing else. The field is read as well as the
        // hierarchy: a bed built into a plot after the plot itself may not have been handed its
        // land plot yet when it starts.
        if (spawner._landPlot != null || spawner.GetComponentInParent<LandPlot>(true) != null) return;

        ResourceGrowerDefinition definition = spawner._resourceGrowerDefinition;
        if (definition == null || !IsHostPatch(definition)) return;

        string key = definition.name;
        if (!Sown.TryGetValue(key, out ResourceGrowerDefinition sown))
        {
            sown = Sow(definition);
            Sown[key] = sown;
        }
        if (sown == null || spawner._resourceGrowerDefinition == sown) return;

        spawner._resourceGrowerDefinition = sown;
        _patched++;

        if (_patched <= LocationLogLimit)
        {
            Vector3 position = spawner.transform.position;
            Main.Log.Msg($"Wild ginger may now come up in {spawner.gameObject.scene.name} " +
                         $"at ({position.x:F0}, {position.y:F0}, {position.z:F0}), among {key}.");
        }
    }

    /// <summary>Copies a grower definition and adds ginger to its table.</summary>
    private static ResourceGrowerDefinition Sow(ResourceGrowerDefinition definition)
    {
        Il2CppReferenceArray<ResourceSpawnerDefinition.WeightedResourceEntry> resources = definition._resources;
        if (resources == null || resources.Length == 0) return null;

        float total = 0f;
        foreach (ResourceSpawnerDefinition.WeightedResourceEntry entry in resources)
        {
            if (entry == null) continue;
            if (entry.ResourceIdentifiableType == GingerCrop.Ginger) return definition;   // already grows it
            total += entry.Weight;
        }
        if (total <= 0f) return null;

        ResourceGrowerDefinition sown = Object.Instantiate(definition);
        sown.hideFlags = HideFlags.HideAndDontSave;
        sown.name = definition.name;   // the save stores this, so the copy answers to the same name

        Il2CppReferenceArray<ResourceSpawnerDefinition.WeightedResourceEntry> grown = new(resources.Length + 1);
        for (int i = 0; i < resources.Length; i++) grown[i] = resources[i];
        grown[resources.Length] = new ResourceSpawnerDefinition.WeightedResourceEntry
        {
            ResourceIdentifiableType = GingerCrop.Ginger,
            Weight = total * Share,
            MinimumAmount = 1
        };
        sown._resources = grown;

        return sown;
    }

    /// <summary>Whether this is a patch of the crop the ginger hides among.</summary>
    private static bool IsHostPatch(ResourceGrowerDefinition definition)
    {
        string refId = definition._primaryResourceType?.ReferenceId;
        return refId != null && refId.ToLowerInvariant().Contains(Host);
    }
}

/// <summary>Catches every resource patch as its cell streams in.</summary>
[HarmonyPatch(typeof(SpawnResource), nameof(SpawnResource.Start))]
internal static class Patch_SpawnResource_Start
{
    private static void Postfix(SpawnResource __instance) => WildGinger.Inject(__instance);
}
