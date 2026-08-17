using Il2Cpp;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Il2CppMonomiPark.SlimeRancher.Slime;
using SR2Kit;
using UnityEngine;

namespace GemSlimes;

/// <summary>
/// One gem: its recipe (which vanilla slime it is cut from, its colours, what its plort is worth)
/// and the assets built from it.
///
/// The five gems only differ by data, so a single builder covers all of them; the Slime Rancher 1
/// mod repeated the same 130-line method once per gem.
/// </summary>
public sealed class Gem
{
    /// <summary>Gems fade to black at the bottom — that is what makes them read as cut stone.</summary>
    private static readonly Color Facet = Color.black;

    public Gem(string key, string displayName, string baseSlime, bool shatters, int plortValue,
        float plortSaturation, string top, string middle)
    {
        Key = key;
        DisplayName = displayName;
        BaseSlimeKeyword = baseSlime;
        Shatters = shatters;
        PlortValue = plortValue;
        PlortSaturation = plortSaturation;
        Top = Hex(top);
        Middle = Hex(middle);
    }

    public string Key { get; }
    public string DisplayName { get; }

    /// <summary>Reference-id fragment of the vanilla slime this gem is cloned from.</summary>
    public string BaseSlimeKeyword { get; }

    /// <summary>Whether the gem breaks into its plort when the player touches it.</summary>
    public bool Shatters { get; }

    public int PlortValue { get; }

    /// <summary>Market saturation point, the value the original mod shipped for each plort.</summary>
    public float PlortSaturation { get; }

    public Color Top { get; }
    public Color Middle { get; }

    public SlimeDefinition Definition { get; private set; }
    public IdentifiableType Plort { get; private set; }

    public string SlimeReferenceId => $"GemSlimes_Slime{Key}";
    public string PlortReferenceId => $"GemSlimes_Plort{Key}";

    // ---------------------------------------------------------------- Creation

    /// <summary>Builds the plort and the slime, and registers both with the game.</summary>
    public void Build(LookupDirector director, SlimeDefinitions definitions,
        IdentifiableType plortTemplate, SlimeDefinition slimeTemplate)
    {
        Plort = BuildPlort(director, plortTemplate);
        Definition = BuildSlime(director, definitions, slimeTemplate);
    }

    private IdentifiableType BuildPlort(LookupDirector director, IdentifiableType template)
    {
        string nameKey = Translations.Add("Actor", $"l.{Key.ToLowerInvariant()}_plort", $"{DisplayName.Replace(" Slime", string.Empty)} Plort");

        IdentifiableType plort = IdentifiableRegistry.Create(director, template, PlortReferenceId, "Actor", nameKey);
        if (plort == null) return null;

        plort.color = Middle;
        IdentifiableRegistry.SetPrefab(plort, BuildPrefab(template.prefab, $"Plort{Key}"));
        return plort;
    }

    /// <summary>
    /// A <see cref="SlimeDefinition"/> is itself an <see cref="IdentifiableType"/>, so cloning one
    /// yields the slime's data and its identity in a single asset.
    /// </summary>
    private SlimeDefinition BuildSlime(LookupDirector director, SlimeDefinitions definitions,
        SlimeDefinition template)
    {
        string nameKey = Translations.Add("Actor", $"l.{Key.ToLowerInvariant()}_slime", DisplayName);

        IdentifiableType created = IdentifiableRegistry.Create(director, template, SlimeReferenceId, "Actor", nameKey);
        SlimeDefinition slime = created?.TryCast<SlimeDefinition>();
        if (slime == null)
        {
            Main.Log.Error($"Could not clone a slime definition for {SlimeReferenceId}.");
            return null;
        }

        slime.Name = DisplayName;
        slime.color = Middle;

        // Gems are cut stone: they neither merge into largos nor keep the base slime's toys.
        slime.CanLargofy = false;
        slime.FavoriteToyIdents = new Il2CppReferenceArray<ToyDefinition>(0);

        SlimeAppearance appearance = BuildAppearance(template, $"Slime{Key}Appearance");
        if (appearance != null)
        {
            Il2CppReferenceArray<SlimeAppearance> appearances = new(1);
            appearances[0] = appearance;
            slime.AppearancesDefault = appearances;
        }

        GameObject prefab = BuildPrefab(template.prefab, $"Slime{Key}");
        RetargetPrefab(prefab, slime, appearance);
        IdentifiableRegistry.SetPrefab(slime, prefab);

        IdentifiableRegistry.AddSlimeDefinition(definitions, slime);
        return slime;
    }

    /// <summary>Every meal turns into this gem's plort, whatever the base slime used to produce.</summary>
    public void SetDiet(IdentifiableType favorite)
    {
        SlimeDiet diet = Definition?.Diet;
        if (diet == null || Plort == null) return;

        // Crystal and pink slimes both ship an eat map; retargeting it keeps their food while
        // replacing the produce. Entries that turned the base slime into another vanilla slime are
        // cleared, otherwise a gem would silently become a vanilla slime after a meal.
        if (diet.EatMap != null)
        {
            foreach (SlimeDiet.EatMapEntry entry in diet.EatMap)
            {
                if (entry == null) continue;
                if (entry.ProducesIdent != null) entry.ProducesIdent = Plort;
                entry.BecomesIdent = null;
            }
        }

        Il2CppReferenceArray<IdentifiableType> produce = new(1);
        produce[0] = Plort;
        diet.ProduceIdents = produce;

        if (favorite == null) return;
        Il2CppReferenceArray<IdentifiableType> favorites = new(1);
        favorites[0] = favorite;
        diet.FavoriteIdents = favorites;
    }

    /// <summary>Feeding <paramref name="food"/> to this gem grows it into <paramref name="into"/>.</summary>
    public void AddTransformation(IdentifiableType food, Gem into)
    {
        SlimeDiet diet = Definition?.Diet;
        if (diet?.EatMap == null || into?.Definition == null) return;

        if (food == null)
        {
            Main.Log.Warning($"No food found to grow a {DisplayName} into a {into.DisplayName}.");
            return;
        }

        foreach (SlimeDiet.EatMapEntry existing in diet.EatMap)
        {
            if (existing != null && existing.BecomesIdent == into.Definition) return;
        }

        // No produce on a transformation entry: a meal that yields plorts yields plorts, and the
        // slime stays what it was. The original mod's growth entries name only what to eat and what
        // to become.
        SlimeDiet.EatMapEntry growth = new()
        {
            EatsIdent = food,
            BecomesIdent = into.Definition,
            ProducesIdent = null,
            IsFavorite = true,
            ProductionCount = 0,
            FavoriteProductionCount = 0,
            Driver = SlimeEmotions.Emotion.HUNGER,
            ExtraDrive = 1f,
            MinDrive = 0f
        };
        diet.EatMap.Add(growth);

        // The entry alone is not enough: the game decides between transforming and producing on its
        // own terms, and a whole slime is not one of the meals it transforms on. See Transformations.
        Transformations.Register(Definition, food, growth);

        // The eat map only says what a meal turns into. What a slime is willing to bite comes from
        // its food lists, so the prey has to be named there too or the gem simply ignores it.
        diet.AdditionalFoodIdents = Append(diet.AdditionalFoodIdents, food);
        diet.FavoriteIdents = Append(diet.FavoriteIdents, food);

        Main.Log.Msg($"{DisplayName} eating {food.referenceId} now becomes {into.DisplayName}.");
    }

    /// <summary>Adds an identifiable to one of the diet's lists, leaving it alone if already there.</summary>
    private static Il2CppReferenceArray<IdentifiableType> Append(
        Il2CppReferenceArray<IdentifiableType> list, IdentifiableType type)
    {
        if (list == null) list = new Il2CppReferenceArray<IdentifiableType>(0);

        foreach (IdentifiableType existing in list)
        {
            if (existing == type) return list;
        }

        Il2CppReferenceArray<IdentifiableType> grown = new(list.Length + 1);
        for (int i = 0; i < list.Length; i++) grown[i] = list[i];
        grown[list.Length] = type;
        return grown;
    }

    // ---------------------------------------------------------------- Prefabs and appearance

    /// <summary>
    /// Clones a vanilla prefab into the mod's prefab host and tints it.
    ///
    /// <see cref="PrefabHost.Clone"/> deactivates the clone; the host it lives under is already
    /// inactive, so reactivating the object keeps it out of the running scene while restoring the
    /// <c>activeSelf</c> a real prefab asset has — without it every actor spawned from this prefab
    /// would come out of the spawner disabled.
    /// </summary>
    private GameObject BuildPrefab(GameObject template, string name)
    {
        GameObject prefab = PrefabHost.Clone(template, name);
        prefab.SetActive(true);
        Recolor(prefab);
        return prefab;
    }

    /// <summary>Points the cloned prefab's slime components at this gem instead of the template.</summary>
    private static void RetargetPrefab(GameObject prefab, SlimeDefinition slime, SlimeAppearance appearance)
    {
        foreach (SlimeAppearanceApplicator applicator in prefab.GetComponentsInChildren<SlimeAppearanceApplicator>(true))
        {
            applicator.SlimeDefinition = slime;
            if (appearance != null) applicator.Appearance = appearance;
        }

        foreach (SlimeEat eat in prefab.GetComponentsInChildren<SlimeEat>(true))
            eat.SlimeDefinition = slime;

        // The spikes a crystal slime throws are not read from the appearance: the launcher holds its
        // own two prefabs, and a clone still holds the vanilla ones. Without this the gem is cut
        // stone and throws crystal-slime blue.
        CrystalAppearance crystals = appearance?.CrystalAppearance;
        if (crystals == null) return;

        foreach (CrystalSlimeLaunch launcher in prefab.GetComponentsInChildren<CrystalSlimeLaunch>(true))
        {
            if (crystals.LargeCrystalPrefab != null) launcher._launchSpawnLarge = crystals.LargeCrystalPrefab;
            if (crystals.SmallCrystalPrefab != null) launcher._launchSpawnSmall = crystals.SmallCrystalPrefab;
        }
    }

    /// <summary>
    /// Builds this gem's look from the base slime's default appearance.
    ///
    /// Materials are Unity object references, so a cloned appearance still points at the vanilla
    /// ones: the structure array and every material in it are rebuilt rather than edited in place,
    /// otherwise tinting a gem would repaint the slime it was cloned from.
    /// </summary>
    private SlimeAppearance BuildAppearance(SlimeDefinition template, string name)
    {
        SlimeAppearance source = template.AppearancesDefault != null && template.AppearancesDefault.Length > 0
            ? template.AppearancesDefault[0]
            : null;
        if (source == null)
        {
            Main.Log.Warning($"{DisplayName} has no base appearance to clone; it keeps the vanilla colours.");
            return null;
        }

        SlimeAppearance appearance = Object.Instantiate(source);
        appearance.hideFlags = HideFlags.HideAndDontSave;
        appearance.name = name;

        Il2CppReferenceArray<SlimeAppearanceStructure> structures = source.Structures;
        if (structures != null)
        {
            Il2CppReferenceArray<SlimeAppearanceStructure> tinted = new(structures.Length);
            for (int i = 0; i < structures.Length; i++)
            {
                tinted[i] = new SlimeAppearanceStructure(structures[i])
                {
                    DefaultMaterials = TintedCopy(structures[i].DefaultMaterials)
                };
            }
            appearance.Structures = tinted;
        }

        // The game exposes the writable side of these under the "New"/"GetSet" names; the plain
        // properties are read-only accessors over the same serialized fields.
        appearance.ColorPaletteNew = new SlimeAppearance.Palette
        {
            Top = Top,
            Middle = Middle,
            Bottom = Facet,
            Ammo = Middle
        };
        appearance.SplatColorGetSet = Middle;
        appearance.CrystalAppearanceNew = TintCrystals(source.CrystalAppearance, name);

        return appearance;
    }

    /// <summary>
    /// Gives the crystal spikes a crystal gem launches the gem's own colour. Cloning the appearance
    /// asset and its two prefabs keeps the vanilla crystal slime's spikes untouched.
    /// </summary>
    private CrystalAppearance TintCrystals(CrystalAppearance source, string name)
    {
        if (source == null) return null;

        CrystalAppearance crystals = Object.Instantiate(source);
        crystals.hideFlags = HideFlags.HideAndDontSave;
        crystals.name = $"{name}Crystals";

        if (source.LargeCrystalPrefab != null)
            crystals.LargeCrystalPrefab = BuildPrefab(source.LargeCrystalPrefab, $"{name}CrystalLarge");
        if (source.SmallCrystalPrefab != null)
            crystals.SmallCrystalPrefab = BuildPrefab(source.SmallCrystalPrefab, $"{name}CrystalSmall");

        return crystals;
    }

    private Il2CppReferenceArray<Material> TintedCopy(Il2CppReferenceArray<Material> source)
    {
        if (source == null) return null;

        Il2CppReferenceArray<Material> tinted = new(source.Length);
        for (int i = 0; i < source.Length; i++)
        {
            if (source[i] == null) continue;
            Material material = Object.Instantiate(source[i]);
            Tint(material);
            tinted[i] = material;
        }
        return tinted;
    }

    private void Recolor(GameObject prefab) => SR2Kit.Recolor.Apply(prefab, Top, Middle, Facet, glossy: true);

    private void Tint(Material material) => SR2Kit.Recolor.Tint(material, Top, Middle, Facet, glossy: true);

    private static Color Hex(string hex)
    {
        float r = System.Convert.ToInt32(hex.Substring(0, 2), 16) / 255f;
        float g = System.Convert.ToInt32(hex.Substring(2, 2), 16) / 255f;
        float b = System.Convert.ToInt32(hex.Substring(4, 2), 16) / 255f;
        return new Color(r, g, b, 1f);
    }
}
