using Il2Cpp;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using HarmonyLib;
using SR2Kit;
using UnityEngine;

namespace KookadobaGingerGrower;

/// <summary>
/// Feeds the gold slime.
///
/// Left alone it has no food groups and no eat map at all: it swallows whatever is thrown at it and
/// produces from the diet's produce list, which is how the game gets a gold plort out of it. That
/// emptiness is also why the only thing its appetite could reach was the edible-plort group every
/// diet carries. It is given the groups of an ordinary slime here — fruit, veggies, meat, plus
/// nectar and the ginger it favours — while its produce list is left exactly as the game wrote it.
///
/// Plorts are refused outright rather than by data: clearing the group was not enough in play, so
/// <see cref="Patch_SlimeEat_MaybeChomp"/> turns the bite down before it starts.
/// </summary>
public static class GoldSlimeTaste
{
    /// <summary>Slime whose food groups the gold one borrows: it eats the ordinary lot.</summary>
    private const string DietModel = "pink";

    /// <summary>Nectar and ginger are gathered resources, not members of a food group.</summary>
    private const string Nectar = "nectar";

    /// <summary>Reference id of the gold slime, for the bite that has to be refused.</summary>
    internal static string GoldReferenceId { get; private set; }

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
        GoldReferenceId = gold.referenceId;

        SlimeDefinition model = Lookup.FindSlimeDefinition(definitions, "slime", DietModel);
        if (model?.Diet == null)
        {
            Main.Log.Warning($"No {DietModel} slime to copy a diet from; the gold slime is left as it was.");
            return;
        }

        SlimeDiet diet = gold.Diet;

        // Fruit, veggies and meat, the three groups the ordinary slime lives on.
        diet.MajorFoodIdentifiableTypeGroups = model.Diet.MajorFoodIdentifiableTypeGroups;

        // Its produce list is what turns a meal into a gold plort. Untouched.
        // Its eat map stays empty, as the game wrote it: an empty map means every meal goes through
        // the produce list instead of a per-food rule, which is the gold slime's whole trick.

        // It cannot become a largo, so the group that lets a slime eat plorts has nothing to do.
        diet.EdiblePlortIdentifiableTypeGroup = null;

        IdentifiableType nectar = Lookup.FindIdentifiable(Nectar);
        if (nectar != null) diet.AdditionalFoodIdents = Diets.Append(diet.AdditionalFoodIdents, nectar);
        Diets.AddFavorite(gold, GingerCrop.Ginger);

        Main.Log.Msg($"{gold.ReferenceId} eats fruit, veggies, meat" +
                     $"{(nectar == null ? string.Empty : ", " + nectar.name)} and " +
                     $"{GingerCrop.Ginger.name}; produces {Names(diet.ProduceIdents)}; plorts refused.");
    }

    /// <summary>Whether this slime is the gold one, by reference id rather than by asset.</summary>
    internal static bool IsGold(SlimeDefinition slime)
        => GoldReferenceId != null && slime != null && slime.referenceId == GoldReferenceId;

    private static string Names(Il2CppReferenceArray<IdentifiableType> list)
    {
        if (list == null || list.Length == 0) return "nothing";

        string names = string.Empty;
        foreach (IdentifiableType type in list) names += (type == null ? "null" : type.name) + " ";
        return names.Trim();
    }
}

/// <summary>
/// Turns down a gold slime's bite at a plort.
///
/// Emptying the diet's edible-plort group left plorts on the menu in play, so the refusal is made
/// where the game asks the question instead: no chomp is started, and nothing else about the slime
/// changes.
/// </summary>
[HarmonyPatch(typeof(SlimeEat), nameof(SlimeEat.MaybeChomp))]
internal static class Patch_SlimeEat_MaybeChomp
{
    private static bool Prefix(SlimeEat __instance, GameObject obj, ref bool __result)
    {
        if (obj == null || !GoldSlimeTaste.IsGold(__instance.SlimeDefinition)) return true;

        Identifiable identifiable = obj.GetComponentInParent<Identifiable>();
        if (identifiable?.identType == null || !identifiable.identType.IsPlort) return true;

        __result = false;
        return false;
    }
}

/// <summary>The other door into a meal: a slime that spins before it bites.</summary>
[HarmonyPatch(typeof(SlimeEat), nameof(SlimeEat.MaybeSpinAndChomp))]
internal static class Patch_SlimeEat_MaybeSpinAndChomp
{
    private static bool Prefix(SlimeEat __instance, GameObject obj, ref bool __result)
    {
        if (obj == null || !GoldSlimeTaste.IsGold(__instance.SlimeDefinition)) return true;

        Identifiable identifiable = obj.GetComponentInParent<Identifiable>();
        if (identifiable?.identType == null || !identifiable.identType.IsPlort) return true;

        __result = false;
        return false;
    }
}
