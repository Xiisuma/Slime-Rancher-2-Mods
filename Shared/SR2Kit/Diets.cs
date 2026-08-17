using Il2Cpp;
using Il2CppInterop.Runtime.InteropTypes.Arrays;

namespace SR2Kit;

/// <summary>Editing what a slime eats, without disturbing the rest of its diet.</summary>
public static class Diets
{
    /// <summary>
    /// Makes <paramref name="food"/> a favourite of <paramref name="slime"/>.
    ///
    /// The food lists are additions to the slime's food groups, never a replacement, so a slime that
    /// eats everything goes on eating everything — it simply prefers this one. The food is named in
    /// both lists because a favourite the slime is not willing to bite is no favourite at all: for
    /// anything outside its groups, being listed as edible is what makes it a meal.
    /// </summary>
    /// <returns>False when the slime already had it, or when either is missing.</returns>
    public static bool AddFavorite(SlimeDefinition slime, IdentifiableType food)
    {
        SlimeDiet diet = slime?.Diet;
        if (diet == null || food == null) return false;
        if (Contains(diet.FavoriteIdents, food)) return false;

        diet.AdditionalFoodIdents = Append(diet.AdditionalFoodIdents, food);
        diet.FavoriteIdents = Append(diet.FavoriteIdents, food);
        return true;
    }

    /// <summary>Adds an identifiable to one of the diet's lists, leaving it alone if already there.</summary>
    public static Il2CppReferenceArray<IdentifiableType> Append(
        Il2CppReferenceArray<IdentifiableType> list, IdentifiableType type)
    {
        list ??= new Il2CppReferenceArray<IdentifiableType>(0);
        if (Contains(list, type)) return list;

        Il2CppReferenceArray<IdentifiableType> grown = new(list.Length + 1);
        for (int i = 0; i < list.Length; i++) grown[i] = list[i];
        grown[list.Length] = type;
        return grown;
    }

    private static bool Contains(Il2CppReferenceArray<IdentifiableType> list, IdentifiableType type)
    {
        if (list == null) return false;

        foreach (IdentifiableType existing in list)
        {
            if (existing == type) return true;
        }
        return false;
    }
}
