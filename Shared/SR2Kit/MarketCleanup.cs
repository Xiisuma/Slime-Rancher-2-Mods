using System.Collections.Generic;
using HarmonyLib;
using Il2Cpp;
using UnityEngine;

namespace SR2Kit;

/// <summary>
/// Removes a modded plort from the world once the market has paid for it.
///
/// The plort market pays for anything with a price in the economy, but the port that swallows the
/// plort only destroys the actor for types the game shipped with. A modded plort therefore sells,
/// credits the newbucks, and stays lying in the machine — sellable again on the next touch. This
/// finishes the job the vanilla path skips.
/// </summary>
public static class MarketCleanup
{
    private static readonly HashSet<string> Sellable = new();

    /// <summary>Marks a modded plort as one the market is expected to consume.</summary>
    internal static void Track(IdentifiableType plort)
    {
        if (plort?.referenceId != null) Sellable.Add(plort.referenceId);
    }

    internal static bool IsTracked(IdentifiableType type)
        => type?.referenceId != null && Sellable.Contains(type.referenceId);
}

/// <summary>Destroys the plort the market just bought, when the game left it behind.</summary>
[HarmonyPatch(typeof(ScorePlort), nameof(ScorePlort.OnTriggerEnter))]
internal static class Patch_ScorePlort_OnTriggerEnter
{
    private static void Postfix(Collider col)
    {
        if (col == null) return;

        GameObject actor = col.gameObject;
        if (actor == null) return;

        Identifiable identifiable = actor.GetComponentInParent<Identifiable>();
        if (identifiable == null || !MarketCleanup.IsTracked(identifiable.identType)) return;

        // Still here after the vanilla deposit ran: the sale went through, the actor did not.
        Destroyer.DestroyActor(identifiable.gameObject, "SR2Kit_MarketCleanup", true);
    }
}
