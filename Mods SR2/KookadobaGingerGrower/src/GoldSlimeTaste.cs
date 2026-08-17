using Il2Cpp;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Il2CppMonomiPark.SlimeRancher.Slime;
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

        IdentifiableTypeGroup veggies = VeggieGroup(model.Diet);
        if (veggies == null)
        {
            Main.Log.Warning("No veggie group found; the gold slime is left as it was.");
            return;
        }

        // Vegetables, and nothing else.
        Il2CppReferenceArray<IdentifiableTypeGroup> groups = new(1);
        groups[0] = veggies;
        diet.MajorFoodIdentifiableTypeGroups = groups;

        // The groups say what may be bitten; the eat map is what turns a bite into a swallowed meal.
        // Without it the gold slime chomped food and left it lying there. The model's map is copied
        // rather than shared — writing into the entries would strip the model slime's own meals —
        // keeping only the vegetables, and stripped of everything a meal produced or became: a gold
        // slime eats, and that is all. The plort entries of that map are exactly what had it
        // swallowing plorts.
        diet.EatMap = Feed(model.Diet.EatMap, veggies);
        diet.ProduceIdents = new Il2CppReferenceArray<IdentifiableType>(0);

        // It cannot become a largo, so the group that lets a slime eat plorts has nothing left to do.
        diet.EdiblePlortIdentifiableTypeGroup = null;
        diet.AdditionalFoodIdents = new Il2CppReferenceArray<IdentifiableType>(0);
        diet.FavoriteIdents = new Il2CppReferenceArray<IdentifiableType>(0);

        // Gilded Ginger is a gathered resource rather than a member of the veggie group, so it needs
        // naming in the food lists and an entry of its own to be swallowed.
        Diets.AddFavorite(gold, GingerCrop.Ginger);
        diet.EatMap.Add(Meal(GingerCrop.Ginger, true));

        Main.Log.Msg($"{gold.ReferenceId} eats vegetables only ({diet.EatMap.Count} of them), " +
                     $"favours {GingerCrop.Ginger.name}, and leaves plorts alone.");
    }

    /// <summary>The group of vegetables among the ones the model slime feeds on.</summary>
    private static IdentifiableTypeGroup VeggieGroup(SlimeDiet model)
    {
        if (model.MajorFoodIdentifiableTypeGroups == null) return null;

        foreach (IdentifiableTypeGroup group in model.MajorFoodIdentifiableTypeGroups)
        {
            if (group != null && group.name.ToLowerInvariant().Contains("veggie")) return group;
        }
        return null;
    }

    /// <summary>Copies the vegetable meals of an eat map, keeping only what each entry eats.</summary>
    private static Il2CppSystem.Collections.Generic.List<SlimeDiet.EatMapEntry> Feed(
        Il2CppSystem.Collections.Generic.List<SlimeDiet.EatMapEntry> model, IdentifiableTypeGroup veggies)
    {
        Il2CppSystem.Collections.Generic.List<SlimeDiet.EatMapEntry> copy = new();
        if (model == null) return copy;

        foreach (SlimeDiet.EatMapEntry entry in model)
        {
            if (entry?.EatsIdent == null || entry.EatsIdent.IsPlort) continue;
            if (!veggies.IsMember(entry.EatsIdent)) continue;

            copy.Add(Meal(entry.EatsIdent, false));
        }
        return copy;
    }

    /// <summary>One meal that yields nothing: the food is swallowed and that is the end of it.</summary>
    private static SlimeDiet.EatMapEntry Meal(IdentifiableType food, bool favorite) => new()
    {
        EatsIdent = food,
        BecomesIdent = null,
        ProducesIdent = null,
        IsFavorite = favorite,
        ProductionCount = 0,
        FavoriteProductionCount = 0,
        Driver = SlimeEmotions.Emotion.HUNGER,
        ExtraDrive = favorite ? 1f : 0f,
        MinDrive = 0f
    };
}
