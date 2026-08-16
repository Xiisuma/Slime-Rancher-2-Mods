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
/// Lucky Slimes produce a golden Lucky Plort when they eat a Stony Hen.
/// </summary>
public class Main : MelonMod
{
    private const string PlortReferenceId = "LuckyPlorts_PlortLucky";
    private const int PlortValue = 60;

    private static readonly Color GoldTop = new(1.0f, 0.85f, 0.20f, 1f);
    private static readonly Color GoldMiddle = new(1.0f, 0.75f, 0.10f, 1f);
    private static readonly Color GoldBottom = new(0.9f, 0.60f, 0.05f, 1f);

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

        LuckyPlort.color = GoldMiddle;

        GameObject prefab = PrefabHost.Clone(pinkPlort.prefab, "PlortLucky");
        Recolor(prefab);
        IdentifiableRegistry.SetPrefab(LuckyPlort, prefab);

        Log.Msg("Lucky Plort created.");
    }

    private static void Recolor(GameObject prefab)
    {
        foreach (MeshRenderer renderer in prefab.GetComponentsInChildren<MeshRenderer>(true))
        {
            foreach (Material material in renderer.materials)
            {
                if (material == null) continue;
                if (material.HasProperty("_TopColor")) material.SetColor("_TopColor", GoldTop);
                if (material.HasProperty("_MiddleColor")) material.SetColor("_MiddleColor", GoldMiddle);
                if (material.HasProperty("_BottomColor")) material.SetColor("_BottomColor", GoldBottom);
                if (material.HasProperty("_Color")) material.SetColor("_Color", GoldMiddle);
            }
        }
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
