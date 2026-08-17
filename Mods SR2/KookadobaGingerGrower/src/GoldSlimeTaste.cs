using Il2Cpp;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using SR2Kit;

namespace KookadobaGingerGrower;

/// <summary>
/// Feeds the gold slime.
///
/// Left alone, a gold slime eats nothing at all: no food groups, no eat map, and the edible-plort
/// group every diet carries. That group is the largo mechanic, and a gold slime has no largo to
/// become — but it is the only thing its appetite could reach, so the first food handed to it came
/// with a taste for plorts. It is given a proper diet here instead: fruit, veggies, meat and nectar,
/// with Gilded Ginger as the favourite, and no plorts.
/// </summary>
public static class GoldSlimeTaste
{
    /// <summary>Slime whose food groups and eat map the gold one borrows: it eats the ordinary lot.</summary>
    private const string DietModel = "pink";

    /// <summary>Nectar is a gathered resource, not a member of a food group.</summary>
    private const string Nectar = "nectar";

    private static bool _done;

    /// <summary>Idempotent, like the rest of the mod's setup: called at both startup moments.</summary>
    public static void Apply()
    {
        if (_done || GingerCrop.Ginger == null) return;

        SlimeDefinitions definitions = GameContext.Instance?.SlimeDefinitions
                                       ?? LookupDirector.GetIfReady()?._slimeDefinitions;
        if (definitions == null) return;

        SlimeDefinition gold = Lookup.FindSlimeDefinition(definitions, "slime", "gold");
        if (gold?.Diet == null)
        {
            Main.Log.Warning("No gold slime in this game; nothing to feed.");
            _done = true;
            return;
        }
        _done = true;

        SlimeDefinition model = Lookup.FindSlimeDefinition(definitions, "slime", DietModel);
        if (model?.Diet == null)
        {
            Main.Log.Warning($"No {DietModel} slime to copy a diet from; the gold slime is left as it was.");
            return;
        }

        SlimeDiet diet = gold.Diet;

        // Fruit, veggies and meat, the three groups the ordinary slime lives on.
        diet.MajorFoodIdentifiableTypeGroups = model.Diet.MajorFoodIdentifiableTypeGroups;

        // The groups say what may be bitten; the eat map is what turns a bite into a swallowed meal.
        // Without it the gold slime chomped food and left it lying there. The model's map is copied
        // rather than shared, and stripped of everything it produced or became: a gold slime eats,
        // and that is all.
        diet.EatMap = Feed(model.Diet.EatMap);
        diet.ProduceIdents = new Il2CppReferenceArray<IdentifiableType>(0);

        // No plorts. It cannot become a largo, so the group has nothing left to do.
        diet.EdiblePlortIdentifiableTypeGroup = null;

        IdentifiableType nectar = Lookup.FindIdentifiable(Nectar);
        if (nectar != null) diet.AdditionalFoodIdents = Diets.Append(diet.AdditionalFoodIdents, nectar);

        Diets.AddFavorite(gold, GingerCrop.Ginger);

        Main.Log.Msg($"{gold.ReferenceId} eats fruit, veggies, meat" +
                     $"{(nectar == null ? string.Empty : " and " + nectar.name)}, favours " +
                     $"{GingerCrop.Ginger.name}, and leaves plorts alone.");
    }

    /// <summary>
    /// Copies an eat map, keeping only what each entry eats. The entries are objects shared with the
    /// slime they belong to, so they are rebuilt rather than edited — writing into them would strip
    /// the model slime's own meals.
    /// </summary>
    private static Il2CppSystem.Collections.Generic.List<SlimeDiet.EatMapEntry> Feed(
        Il2CppSystem.Collections.Generic.List<SlimeDiet.EatMapEntry> model)
    {
        Il2CppSystem.Collections.Generic.List<SlimeDiet.EatMapEntry> copy = new();
        if (model == null) return copy;

        foreach (SlimeDiet.EatMapEntry entry in model)
        {
            if (entry?.EatsIdent == null) continue;

            copy.Add(new SlimeDiet.EatMapEntry
            {
                EatsIdent = entry.EatsIdent,
                BecomesIdent = null,
                ProducesIdent = null,
                IsFavorite = false,
                ProductionCount = 0,
                FavoriteProductionCount = 0,
                Driver = entry.Driver,
                ExtraDrive = entry.ExtraDrive,
                MinDrive = entry.MinDrive
            });
        }
        return copy;
    }
}
