using BubbleSlimes;
using Il2Cpp;
using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using MelonLoader;
using SR2Kit;
using UnityEngine;

[assembly: MelonInfo(typeof(BubbleSlimes.Main), "Bubble Slimes SR2", "1.0.0", "Xiisuma")]
[assembly: MelonGame("MonomiPark", "SlimeRancher2")]

namespace BubbleSlimes;

/// <summary>
/// Port of the Slime Rancher 1 mod "Bubble Slimes" to Slime Rancher 2
/// (Il2Cpp + MelonLoader, no modding framework — see the shared SR2Kit).
///
/// Adds a fragile water-blue slime that pops when the player bumps into it, and its plort.
/// </summary>
public class Main : MelonMod
{
    private const string SlimeReferenceId = "BubbleSlimes_SlimeBubble";
    private const string PlortReferenceId = "BubbleSlimes_PlortBubble";

    private const int PlortValue = 210;
    private const float PlortSaturation = 189f;

    // Palette of the original mod: pale water blue.
    private static readonly Color SlimeTop = Color32ToColor(155, 251, 255);
    private static readonly Color SlimeMiddle = Color32ToColor(146, 197, 242);
    private static readonly Color SlimeBottom = Color32ToColor(118, 172, 226);

    private static readonly Color PlortTop = Color32ToColor(186, 230, 255);
    private static readonly Color PlortMiddle = Color32ToColor(112, 202, 243);
    private static readonly Color PlortBottom = Color32ToColor(94, 158, 238);

    public static Main Instance { get; private set; }
    public static MelonLogger.Instance Log => Instance.LoggerInstance;

    public static IdentifiableType BubblePlort { get; private set; }
    public static SlimeDefinition BubbleSlime { get; private set; }

    private static LiquidDefinition _water;

    /// <summary>Water, used for the splash a popping slime leaves behind.</summary>
    public static LiquidDefinition Water
    {
        get
        {
            if (_water != null) return _water;
            foreach (LiquidDefinition liquid in Resources.FindObjectsOfTypeAll<LiquidDefinition>())
            {
                if (liquid.IsWater) return _water = liquid;
            }
            return null;
        }
    }

    public override void OnInitializeMelon()
    {
        Instance = this;
        Hooks.OnLookupDirectorReady(CreateContent);
        Hooks.OnSceneContextReady(OnSceneReady);
    }

    // ---------------------------------------------------------------- Creation

    private void CreateContent(LookupDirector director)
    {
        Translations.Add("Actor", "l.bubble_slime", "Bubble Slime");
        Translations.Add("Actor", "l.bubble_plort", "Bubble Plort");

        IdentifiableType pinkPlort = Lookup.FindIdentifiable("plort", "pink");
        if (pinkPlort == null || pinkPlort.prefab == null)
        {
            Log.Error("Pink plort not found; nothing can be created.");
            return;
        }

        BubblePlort = IdentifiableRegistry.Create(director, pinkPlort, PlortReferenceId, "Actor", "l.bubble_plort");
        if (BubblePlort != null)
        {
            BubblePlort.color = PlortMiddle;
            GameObject plortPrefab = PrefabHost.Clone(pinkPlort.prefab, "PlortBubble");
            Recolor(plortPrefab, PlortTop, PlortMiddle, PlortBottom);
            IdentifiableRegistry.SetPrefab(BubblePlort, plortPrefab);
        }

        SlimeDefinitions definitions = GameContext.Instance?.SlimeDefinitions ?? director._slimeDefinitions;
        SlimeDefinition pinkSlime = Lookup.FindSlimeDefinition(definitions, "slime", "pink");
        if (pinkSlime == null || pinkSlime.prefab == null)
        {
            Log.Error("Pink slime not found; the Bubble Slime cannot be created.");
            return;
        }

        BubbleSlime = IdentifiableRegistry
            .Create(director, pinkSlime, SlimeReferenceId, "Actor", "l.bubble_slime")
            ?.TryCast<SlimeDefinition>();
        if (BubbleSlime == null)
        {
            Log.Error("Could not clone the pink slime definition.");
            return;
        }

        BubbleSlime.color = SlimeMiddle;
        BubbleSlime.CanLargofy = false;   // a bubble is too fragile to fuse with anything
        SetDiet(BubbleSlime, BubblePlort);

        GameObject slimePrefab = PrefabHost.Clone(pinkSlime.prefab, "SlimeBubble");
        Recolor(slimePrefab, SlimeTop, SlimeMiddle, SlimeBottom);
        slimePrefab.AddComponent(Il2CppType.Of<BubblePop>());
        IdentifiableRegistry.SetPrefab(BubbleSlime, slimePrefab);

        IdentifiableRegistry.AddSlimeDefinition(definitions, BubbleSlime);

        // Rare member of the pink slime's spawn sets, the weight the original mod used.
        SlimeSpawns.Register(BubbleSlime, 0.04f);
        Log.Msg("Bubble Slime created.");
    }

    /// <summary>Keeps the pink slime's food, but every meal produces a bubble plort.</summary>
    private static void SetDiet(SlimeDefinition slime, IdentifiableType plort)
    {
        SlimeDiet diet = slime.Diet;
        if (diet == null || plort == null) return;

        if (diet.EatMap != null)
        {
            foreach (SlimeDiet.EatMapEntry entry in diet.EatMap)
            {
                if (entry == null) continue;
                if (entry.ProducesIdent != null) entry.ProducesIdent = plort;
                // A modded slime must never turn into a vanilla one when it eats.
                entry.BecomesIdent = null;
            }
        }

        Il2CppReferenceArray<IdentifiableType> produce = new(1);
        produce[0] = plort;
        diet.ProduceIdents = produce;
    }

    private static void Recolor(GameObject prefab, Color top, Color middle, Color bottom)
        => SR2Kit.Recolor.Apply(prefab, top, middle, bottom);

    // ---------------------------------------------------------------- Per-save setup

    private void OnSceneReady(SceneContext context)
    {
        Translations.Flush();
        PlortEconomy.Register(context.PlortEconomyDirector, BubblePlort, PlortValue, PlortSaturation);
        SlimeSpawns.Reset();
    }

    private static Color Color32ToColor(byte r, byte g, byte b) => new(r / 255f, g / 255f, b / 255f, 1f);
}
