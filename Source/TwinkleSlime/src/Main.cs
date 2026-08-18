using Il2Cpp;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Il2CppMonomiPark.SlimeRancher.Slime;
using MelonLoader;
using SR2Kit;
using TwinkleSlime;
using UnityEngine;

[assembly: MelonInfo(typeof(TwinkleSlime.Main), "Twinkle Slime SR2", "1.0.0", "MegaPiggy")]
[assembly: MelonGame("MonomiPark", "SlimeRancher2")]

namespace TwinkleSlime;

/// <summary>
/// Port of the Slime Rancher 1 mod "Twinkle Slime" to Slime Rancher 2
/// (Il2Cpp + MelonLoader, no modding framework — see the shared SR2Kit).
///
/// Adds a ranchable Twinkle Slime and its purple Lumina variant, each with its own plort.
/// </summary>
public class Main : MelonMod
{
    private const string TwinklePlortId = "TwinkleSlime_PlortTwinkle";
    private const string LuminaPlortId = "TwinkleSlime_PlortLumina";
    private const string TwinkleSlimeId = "TwinkleSlime_SlimeTwinkle";
    private const string LuminaSlimeId = "TwinkleSlime_SlimeLumina";

    private const int TwinklePlortValue = 45;
    private const int LuminaPlortValue = 80;

    private static readonly Color[] TwinkleColors = { Hex("F9B6EF"), Hex("F9DEB6"), Hex("A4AAF6") };
    private static readonly Color[] LuminaColors = { Hex("9E5CF9"), Hex("6137D7"), Hex("5720C9") };

    public static Main Instance { get; private set; }
    public static MelonLogger.Instance Log => Instance.LoggerInstance;

    public static IdentifiableType TwinklePlort { get; private set; }
    public static IdentifiableType LuminaPlort { get; private set; }
    public static SlimeDefinition TwinkleSlimeDefinition { get; private set; }
    public static SlimeDefinition LuminaSlimeDefinition { get; private set; }

    public override void OnInitializeMelon()
    {
        Instance = this;
        Hooks.OnLookupDirectorReady(CreateContent);
        Hooks.OnSceneContextReady(OnSceneReady);
    }

    // ---------------------------------------------------------------- Creation

    private void CreateContent(LookupDirector director)
    {
        Translations.Add("Actor", "l.twinkle_plort", "Twinkle Plort");
        Translations.Add("Actor", "l.lumina_plort", "Lumina Plort");
        Translations.Add("Actor", "l.twinkle_slime", "Twinkle Slime");
        Translations.Add("Actor", "l.lumina_slime", "Lumina Slime");

        IdentifiableType pinkPlort = Lookup.FindIdentifiable("plort", "pink");
        if (pinkPlort == null || pinkPlort.prefab == null)
        {
            Log.Error("Pink plort not found; nothing can be created.");
            return;
        }

        TwinklePlort = CreatePlort(director, pinkPlort, TwinklePlortId, "l.twinkle_plort", "PlortTwinkle", TwinkleColors);
        LuminaPlort = CreatePlort(director, pinkPlort, LuminaPlortId, "l.lumina_plort", "PlortLumina", LuminaColors);

        SlimeDefinitions definitions = GameContext.Instance?.SlimeDefinitions ?? director._slimeDefinitions;
        SlimeDefinition baseSlime = Lookup.FindSlimeDefinition(definitions, "slime", "twinkle")
                                   ?? Lookup.FindSlimeDefinition(definitions, "slime", "pink");
        if (baseSlime == null || baseSlime.prefab == null)
        {
            Log.Error("No base slime to clone; the slimes cannot be created.");
            return;
        }

        TwinkleSlimeDefinition = CreateSlime(director, definitions, baseSlime, TwinkleSlimeId,
            "l.twinkle_slime", "SlimeTwinkle", TwinkleColors, TwinklePlort);
        LuminaSlimeDefinition = CreateSlime(director, definitions, baseSlime, LuminaSlimeId,
            "l.lumina_slime", "SlimeLumina", LuminaColors, LuminaPlort);

        // Shares of a spawn set, not absolute weights: roughly one twinkle in fifty slimes, and a
        // lumina in five hundred — it was the secret variant in Slime Rancher 1.
        SlimeSpawns.Register(TwinkleSlimeDefinition, 0.02f);
        SlimeSpawns.Register(LuminaSlimeDefinition, 0.002f);

        Pedia.Register(TwinkleSlimeDefinition, baseSlime,
            "A slime that took to the night sky. Twinkle slimes glitter in the dark and keep the " +
            "habits of a pink one otherwise: they eat anything and complain about nothing.");
        Pedia.Register(LuminaSlimeDefinition, baseSlime,
            "The rare cousin of the twinkle slime, lit from the inside. Ranchers who find one " +
            "usually find it alone — there is barely one lumina for every ten twinkles.");

        Log.Msg("Twinkle and Lumina slimes created.");
    }

    private static IdentifiableType CreatePlort(LookupDirector director, IdentifiableType template,
        string referenceId, string nameKey, string prefabName, Color[] colors)
    {
        IdentifiableType plort = IdentifiableRegistry.Create(director, template, referenceId, "Actor", nameKey);
        if (plort == null) return null;

        plort.color = colors[1];

        GameObject prefab = PrefabHost.Clone(template.prefab, prefabName);
        Recolor(prefab, colors);
        IdentifiableRegistry.SetPrefab(plort, prefab);
        return plort;
    }

    /// <summary>
    /// A <see cref="SlimeDefinition"/> is itself an <see cref="IdentifiableType"/>, so cloning one
    /// produces both the slime's data and its identity in a single asset.
    /// </summary>
    private static SlimeDefinition CreateSlime(LookupDirector director, SlimeDefinitions definitions,
        SlimeDefinition template, string referenceId, string nameKey, string prefabName,
        Color[] colors, IdentifiableType plort)
    {
        IdentifiableType created = IdentifiableRegistry.Create(director, template, referenceId, "Actor", nameKey);
        SlimeDefinition slime = created?.TryCast<SlimeDefinition>();
        if (slime == null)
        {
            Log.Error($"Could not clone the slime definition for {referenceId}.");
            return null;
        }

        slime.color = colors[1];
        slime.CanLargofy = false;
        SetDiet(slime, plort);

        SlimeAppearance appearance = SlimeBuilder.BuildAppearance(
            slime, template, colors[0], colors[1], colors[2], prefabName + "Appearance");

        GameObject prefab = PrefabHost.Clone(template.prefab, prefabName);
        Recolor(prefab, colors);
        SlimeBuilder.RetargetPrefab(prefab, slime, appearance);
        IdentifiableRegistry.SetPrefab(slime, prefab);

        IdentifiableRegistry.AddSlimeDefinition(definitions, slime);
        return slime;
    }

    /// <summary>Keeps the base slime's food, but every meal produces the mod's plort.</summary>
    private static void SetDiet(SlimeDefinition slime, IdentifiableType plort)
    {
        SlimeDiet diet = slime.Diet;
        if (diet == null || plort == null) return;

        // The vanilla twinkle slime has no eat map at all — it feeds off the food groups instead.
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

    private static void Recolor(GameObject prefab, Color[] colors)
        => SR2Kit.Recolor.Apply(prefab, colors[0], colors[1], colors[2]);

    // ---------------------------------------------------------------- Per-save setup

    private void OnSceneReady(SceneContext context)
    {
        Translations.Flush();
        PlortEconomy.Register(context.PlortEconomyDirector, TwinklePlort, TwinklePlortValue);
        PlortEconomy.Register(context.PlortEconomyDirector, LuminaPlort, LuminaPlortValue);

        SlimeSpawns.Reset();
        AssignFavoriteToy();
    }

    /// <summary>
    /// The Slime Rancher 1 mod shipped a microphone toy the twinkle slimes loved. A brand new toy
    /// cannot be bought in Slime Rancher 2 without a real Addressables asset behind it, so the
    /// slimes adopt an existing musical toy instead — the mechanic is kept, the asset is not.
    /// </summary>
    private void AssignFavoriteToy()
    {
        if (TwinkleSlimeDefinition == null) return;

        ToyDefinition toy = FindToy("chime") ?? FindToy("music") ?? FindToy("bell") ?? FindToy("");
        if (toy == null)
        {
            Log.Warning("No toy found; the slimes keep the favourite toys of the slime they were cloned from.");
            return;
        }

        Il2CppReferenceArray<ToyDefinition> favorites = new(1);
        favorites[0] = toy;
        TwinkleSlimeDefinition.FavoriteToyIdents = favorites;
        if (LuminaSlimeDefinition != null) LuminaSlimeDefinition.FavoriteToyIdents = favorites;

        Log.Msg($"Favourite toy set to {toy.referenceId}.");
    }

    private static ToyDefinition FindToy(string keyword)
    {
        foreach (IdentifiableType type in Lookup.IdentifiableTypes)
        {
            ToyDefinition toy = type.TryCast<ToyDefinition>();
            if (toy == null) continue;
            if (keyword.Length == 0) return toy;
            if (toy.referenceId != null && toy.referenceId.ToLowerInvariant().Contains(keyword)) return toy;
        }
        return null;
    }

    private static Color Hex(string hex)
    {
        float r = System.Convert.ToInt32(hex.Substring(0, 2), 16) / 255f;
        float g = System.Convert.ToInt32(hex.Substring(2, 2), 16) / 255f;
        float b = System.Convert.ToInt32(hex.Substring(4, 2), 16) / 255f;
        return new Color(r, g, b, 1f);
    }
}
