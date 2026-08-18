using System.Collections.Generic;
using HarmonyLib;
using Il2Cpp;
using UnityEngine;

namespace GemSlimes;

/// <summary>
/// Makes the growth chain actually fire.
///
/// An eat map entry can name what a meal turns into, but Slime Rancher 2 only takes its
/// transformation branch for the meals it treats as largo material. Everything else goes down the
/// produce branch — which is why a sapphire that swallowed a lucky slime dropped two sapphire plorts
/// and stayed a sapphire. The growth meals are therefore resolved here, before the game picks a
/// branch, and handed straight to the game's own transformation.
/// </summary>
internal static class Transformations
{
    private sealed class Growth
    {
        public string Eater;
        public string Food;
        public SlimeDiet.EatMapEntry Entry;
    }

    private static readonly List<Growth> Chain = new();

    /// <summary>Records that <paramref name="eater"/> grows when it eats <paramref name="food"/>.</summary>
    public static void Register(SlimeDefinition eater, IdentifiableType food, SlimeDiet.EatMapEntry entry)
    {
        if (eater == null || food == null || entry == null) return;

        Chain.Add(new Growth
        {
            Eater = eater.referenceId,
            Food = food.referenceId,
            Entry = entry
        });
    }

    /// <summary>
    /// The growth entry for this meal, or null when the pair is not part of the chain. Reference ids
    /// are compared rather than the assets themselves: a slime being eaten is an actor, and the
    /// identity the game hands over is not guaranteed to be the same managed wrapper.
    /// </summary>
    public static SlimeDiet.EatMapEntry Find(SlimeDefinition eater, IdentifiableType food)
    {
        if (Chain.Count == 0 || eater == null || food == null) return null;

        string eaterId = eater.referenceId;
        string foodId = food.referenceId;
        foreach (Growth growth in Chain)
        {
            if (growth.Eater == eaterId && growth.Food == foodId) return growth.Entry;
        }
        return null;
    }
}

/// <summary>Sends a gem's growth meal down the transformation branch instead of the produce one.</summary>
[HarmonyPatch(typeof(SlimeEat), nameof(SlimeEat.FinishChomp))]
internal static class Patch_SlimeEat_FinishChomp
{
    private static bool Prefix(SlimeEat __instance, GameObject chomping, IdentifiableType chompingId)
    {
        SlimeDiet.EatMapEntry entry = Transformations.Find(__instance.SlimeDefinition, chompingId);
        if (entry == null) return true;

        Main.Log.Msg($"{__instance.SlimeDefinition.referenceId} ate {chompingId.referenceId} " +
                     $"and becomes {entry.BecomesIdent?.referenceId}.");
        __instance.EatAndTransform(chomping, entry, false);
        return false;
    }
}
