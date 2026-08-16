using System.Collections.Generic;
using GemSlimes;
using Il2Cpp;
using MelonLoader;
using SR2Kit;
using UnityEngine;

[assembly: MelonInfo(typeof(GemSlimes.Main), "Gem Slimes SR2", "1.5.0", "Baz")]
[assembly: MelonGame("MonomiPark", "SlimeRancher2")]

namespace GemSlimes;

/// <summary>
/// Port of the Slime Rancher 1 mod "GemSlimes" by Baz to Slime Rancher 2
/// (Il2Cpp + MelonLoader, no modding framework — see the shared SR2Kit).
///
/// Five gem slimes and their plorts. Garnets and sapphires are found in the wild; the other three
/// are grown from them by feeding, each step worth more than the last. The three cut from the
/// crystal slime shatter into their plort when the player walks into them.
/// </summary>
public class Main : MelonMod
{
    /// <summary>
    /// The gems, in progression order. Colours are the ones the Slime Rancher 1 mod used.
    /// </summary>
    // Plort values and saturations are the ones the Slime Rancher 1 mod registered. The garnet had no
    // plort of its own there — it produced the mosaic plort, a slime Slime Rancher 2 does not have —
    // so it gets its own, priced below the sapphire to keep the chain's ramp intact.
    private static readonly Gem Garnet = new("Garnet", "Garnet Slime", "crystal", true, 90, 70f, "FF000B", "FF003D");
    private static readonly Gem Sapphire = new("Sapphire", "Sapphire Slime", "pink", false, 125, 80f, "1504C1", "2536AC");
    private static readonly Gem Emerald = new("Emerald", "Emerald Slime", "pink", false, 300, 215f, "169E36", "1D953C");
    private static readonly Gem Amethyst = new("Amethyst", "Amethyst Slime", "crystal", true, 450, 360f, "7F006E", "7F0092");
    private static readonly Gem Diamond = new("Diamond", "Diamond Slime", "crystal", true, 600, 495f, "6DD6EE", "00A2FF");

    private static readonly Gem[] Gems = { Garnet, Sapphire, Emerald, Amethyst, Diamond };

    public static Main Instance { get; private set; }
    public static MelonLogger.Instance Log => Instance.LoggerInstance;

    /// <summary>Player of the running save, cached so shattering gems do not chase a singleton.</summary>
    public static Transform PlayerTransform { get; private set; }

    public override void OnInitializeMelon()
    {
        Instance = this;
        Hooks.OnLookupDirectorReady(CreateContent);
        Hooks.OnSceneContextReady(OnSceneReady);
    }

    /// <summary>Plort a gem slime leaves behind when it shatters, or null if it is not a gem.</summary>
    public static IdentifiableType PlortOf(IdentifiableType slime)
    {
        if (slime == null) return null;

        foreach (Gem gem in Gems)
        {
            if (gem.Definition == slime) return gem.Plort;
        }
        return null;
    }

    // ---------------------------------------------------------------- Creation

    private void CreateContent(LookupDirector director)
    {
        IdentifiableType plortTemplate = Lookup.FindIdentifiable("plort", "pink");
        if (plortTemplate?.prefab == null)
        {
            Log.Error("Pink plort not found; nothing can be created.");
            return;
        }

        SlimeDefinitions definitions = GameContext.Instance?.SlimeDefinitions ?? director._slimeDefinitions;
        SlimeDefinition pink = Lookup.FindSlimeDefinition(definitions, "slime", "pink");
        if (pink?.prefab == null)
        {
            Log.Error("Pink slime not found; the gems cannot be created.");
            return;
        }

        // Crystal slimes carry the spikes and the launch behaviour that make a gem read as one; when
        // the game has no crystal slime the gems fall back to the pink slime and simply lose them.
        SlimeDefinition crystal = Lookup.FindSlimeDefinition(definitions, "slime", "crystal");
        if (crystal?.prefab == null)
        {
            Log.Warning("Crystal slime not found; the crystal gems fall back to the pink slime.");
            crystal = pink;
        }

        foreach (Gem gem in Gems)
        {
            SlimeDefinition template = gem.BaseSlimeKeyword == "crystal" ? crystal : pink;
            gem.Build(director, definitions, plortTemplate, template);
        }

        // The original mod gave every gem the mint mango as its favourite (SR1 id 9).
        IdentifiableType favorite = Lookup.FindIdentifiable("mango")
                                    ?? Lookup.FindIdentifiable("pogo")
                                    ?? Lookup.FindIdentifiable("cuberry");
        foreach (Gem gem in Gems)
        {
            gem.SetDiet(favorite);
            if (gem.Shatters) AttachShatter(gem);
        }

        SetUpProgression();

        // Only the two entry gems are found in the wild; the rest are grown by the player.
        SlimeSpawns.Register(Garnet.Definition, 0.02f, "crystal");
        SlimeSpawns.Register(Sapphire.Definition, 0.04f);

        Log.Msg("Five gem slimes created.");
    }

    /// <summary>
    /// The upgrade chain from the original mod: a gem eats one specific slime and grows into the
    /// next gem up. Slime Rancher 1 fed a lucky slime to the sapphire, a garnet to the emerald and a
    /// gold slime to the amethyst — all three meals are whole slimes, which is what made the chain
    /// expensive. The lucky slime exists here too; the gold slime does not, so the last step falls
    /// back to Gilded Ginger, the rarest thing a rancher can gather in Slime Rancher 2.
    /// </summary>
    private void SetUpProgression()
    {
        IdentifiableType lucky = Lookup.FindIdentifiable("slime", "lucky");
        MakeEdible(lucky);
        Sapphire.AddTransformation(lucky, Emerald);

        MakeEdible(Garnet.Definition);
        Emerald.AddTransformation(Garnet.Definition, Amethyst);

        IdentifiableType gold = Lookup.FindIdentifiable("slime", "gold");
        if (gold != null) MakeEdible(gold);
        else Log.Msg("No gold slime in this game; amethysts grow into diamonds on Gilded Ginger instead.");

        Amethyst.AddTransformation(gold ?? FindGinger(), Diamond);
    }

    /// <summary>
    /// Gilded Ginger is not in the lookup director's reference-id map — like the other gathered
    /// resources it is only reachable as a loaded asset — so it is found by asset name.
    /// </summary>
    private static IdentifiableType FindGinger()
    {
        foreach (IdentifiableType type in Resources.FindObjectsOfTypeAll<IdentifiableType>())
        {
            if (type != null && type.name == "GingerVeggie") return type;
        }
        return Lookup.FindIdentifiable("ginger");
    }

    /// <summary>
    /// Lets a slime be eaten by another slime. Slime prefabs are never edible in the base game, so
    /// every meal in the gem chain has to be opened up first.
    /// </summary>
    private static void MakeEdible(IdentifiableType type)
    {
        if (type?.prefab == null) return;

        foreach (Identifiable identifiable in type.prefab.GetComponentsInChildren<Identifiable>(true))
            identifiable.Edible = true;
    }

    private static void AttachShatter(Gem gem)
    {
        GameObject prefab = gem.Definition?.prefab;
        if (prefab == null || prefab.GetComponent<ShatterOnTouch>() != null) return;

        prefab.AddComponent<ShatterOnTouch>();
    }

    // ---------------------------------------------------------------- Per-save setup

    private void OnSceneReady(SceneContext context)
    {
        Translations.Flush();
        PlayerTransform = context.player != null ? context.player.transform : null;

        foreach (Gem gem in Gems)
        {
            if (gem.Plort != null)
                PlortEconomy.Register(context.PlortEconomyDirector, gem.Plort, gem.PlortValue, gem.PlortSaturation);
        }

        // A definition added after the appearance director built its map has no chosen appearance
        // yet, which would leave the gems rendering as the slime they were cut from.
        context.SlimeAppearanceDirector?.RefreshDefaultChosenSlimes();

        SlimeSpawns.Reset();
    }
}
