using Il2Cpp;
using Il2CppMonomiPark.SlimeRancher.Slime;
using LuckyPlorts;
using MelonLoader;
using SR2Kit;
using UnityEngine;

[assembly: MelonInfo(typeof(LuckyPlorts.Main), "Lucky Plorts SR2", "1.0.0", "DogeisCut")]
[assembly: MelonGame("MonomiPark", "SlimeRancher2")]

namespace LuckyPlorts;

/// <summary>
/// Port of the Slime Rancher 1 mod "Lucky Plorts" by DogeisCut to Slime Rancher 2
/// (Il2Cpp + MelonLoader, no modding framework — see the shared SR2Kit).
///
/// Lucky Slimes produce a pale Lucky Plort when they eat a Stony Hen.
/// </summary>
public class Main : MelonMod
{
    private const string PlortReferenceId = "LuckyPlorts_PlortLucky";
    private const int PlortValue = 60;

    // Off-white, barely grey where the light does not reach: the plort reads as pale stone rather
    // than as the gold slime it comes from.
    private static readonly Color PaleTop = new(0.98f, 0.98f, 0.99f, 1f);
    private static readonly Color PaleMiddle = new(0.87f, 0.88f, 0.91f, 1f);
    private static readonly Color PaleBottom = new(0.72f, 0.74f, 0.78f, 1f);

    public static Main Instance { get; private set; }
    public static MelonLogger.Instance Log => Instance.LoggerInstance;

    /// <summary>The lucky plort, created once the lookup director is ready.</summary>
    public static IdentifiableType LuckyPlort { get; private set; }

    public override void OnInitializeMelon()
    {
        Instance = this;
        Hooks.OnLookupDirectorReady(CreateLuckyPlort);
        Hooks.OnSceneContextReady(OnSceneReady);
    }

    // ---------------------------------------------------------------- Creation

    private void CreateLuckyPlort(LookupDirector director)
    {
        Translations.Add("Actor", "l.lucky_plort", "Lucky Plort");

        IdentifiableType pinkPlort = Lookup.FindIdentifiable("plort", "pink");
        if (pinkPlort == null || pinkPlort.prefab == null)
        {
            Log.Error("Pink plort not found; the Lucky Plort cannot be created.");
            return;
        }

        LuckyPlort = IdentifiableRegistry.Create(director, pinkPlort, PlortReferenceId, "Actor", "l.lucky_plort");
        if (LuckyPlort == null) return;

        LuckyPlort.color = PaleMiddle;

        GameObject prefab = PrefabHost.Clone(pinkPlort.prefab, "PlortLucky");
        // Through the kit, which gives the clone its own material copies: painting the ones it
        // inherited would turn every vanilla pink plort pale as well.
        Recolor.Apply(prefab, PaleTop, PaleMiddle, PaleBottom);
        IdentifiableRegistry.SetPrefab(LuckyPlort, prefab);

        Log.Msg("Lucky Plort created.");
    }

    // ---------------------------------------------------------------- Per-save setup

    private void OnSceneReady(SceneContext context)
    {
        Translations.Flush();
        if (LuckyPlort == null) return;

        PlortEconomy.Register(context.PlortEconomyDirector, LuckyPlort, PlortValue);
        FeedStonyHenToLuckySlimes();
    }

    /// <summary>Makes the Lucky Slime turn Stony Hens into Lucky Plorts.</summary>
    private void FeedStonyHenToLuckySlimes()
    {
        SlimeDefinitions definitions = GameContext.Instance?.SlimeDefinitions;
        if (definitions == null)
        {
            Log.Error("Slime definitions not available.");
            return;
        }

        IdentifiableType stonyHen = Lookup.FindIdentifiable("hen", "stony") ?? Lookup.FindIdentifiable("stony", "chick");
        if (stonyHen == null)
        {
            Log.Error("Stony Hen not found; the Lucky Slime diet is unchanged.");
            return;
        }

        SlimeDefinition luckySlime = Lookup.FindSlimeDefinition(definitions, "lucky");
        if (luckySlime == null || luckySlime.Diet == null)
        {
            Log.Error("Lucky Slime definition not found; the diet is unchanged.");
            return;
        }

        foreach (SlimeDiet.EatMapEntry existing in luckySlime.Diet.EatMap)
        {
            if (existing != null && existing.ProducesIdent == LuckyPlort) return;
        }

        luckySlime.Diet.EatMap.Add(new SlimeDiet.EatMapEntry
        {
            EatsIdent = stonyHen,
            ProducesIdent = LuckyPlort,
            IsFavorite = true,
            ProductionCount = 1,
            FavoriteProductionCount = 2,
            Driver = SlimeEmotions.Emotion.HUNGER,
            ExtraDrive = 0f,
            MinDrive = 0f
        });

        Log.Msg($"Lucky Slimes now turn {stonyHen.referenceId} into Lucky Plorts.");
    }
}
