using System.Collections.Generic;
using HarmonyLib;
using Il2Cpp;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using UnityEngine;

namespace SR2Kit;

/// <summary>
/// Makes a modded slime appear in the wild, by adding it to the spawn sets the game already runs.
///
/// Sweeping the scene for spawners does not work: Slime Rancher 2 streams them in with their cell,
/// so at the moment a save finishes loading almost none of them exist yet. Each spawner is therefore
/// caught in its own <c>Awake</c>, which is also the last moment before it starts spawning.
/// </summary>
public static class SlimeSpawns
{
    private sealed class Entry
    {
        public IdentifiableType Slime;
        public float Share;
        public string HostKeyword;
    }

    /// <summary>How many spawner locations are worth logging before it becomes noise.</summary>
    private const int LocationLogLimit = 30;

    private static readonly List<Entry> Entries = new();

    private static int _patched;

    /// <summary>
    /// Registers a slime for wild spawning.
    /// </summary>
    /// <param name="share">
    /// Fraction of a spawn set the slime takes, relative to the weights already in it: 0.02 makes it
    /// roughly one spawn in fifty. An absolute weight cannot be used, because the vanilla sets do not
    /// agree on a scale — the same number is a rarity in one and the commonest slime in another.
    /// </param>
    /// <param name="hostKeyword">
    /// Reference-id fragment of the vanilla slime whose spawn sets are joined. Keeping to one host
    /// is what puts the modded slime in a sensible part of the world instead of everywhere.
    /// </param>
    public static void Register(IdentifiableType slime, float share, string hostKeyword = "pink")
    {
        if (slime == null) return;

        foreach (Entry existing in Entries)
        {
            if (existing.Slime == slime) return;
        }
        Entries.Add(new Entry { Slime = slime, Share = share, HostKeyword = hostKeyword });
    }

    /// <summary>Drops the per-save counter when another save is loaded.</summary>
    public static void Reset() => _patched = 0;

    /// <summary>How many spawn sets this mod has joined in the running save.</summary>
    public static int PatchedSets => _patched;

    /// <summary>Adds every registered slime to a spawner that is starting up.</summary>
    internal static void Inject(DirectedActorSpawner spawner)
    {
        if (Entries.Count == 0 || spawner == null || spawner.Constraints == null) return;

        foreach (DirectedActorSpawner.SpawnConstraint constraint in spawner.Constraints)
        {
            SlimeSet set = constraint?.Slimeset;
            if (set == null || set.Members == null || set.Members.Length == 0) continue;

            foreach (Entry entry in Entries)
            {
                if (!Hosts(set, entry.HostKeyword) || Contains(set, entry.Slime)) continue;

                float weight = TotalWeight(set) * entry.Share;
                if (weight <= 0f) continue;

                Il2CppReferenceArray<SlimeSet.Member> grown = new(set.Members.Length + 1);
                for (int i = 0; i < set.Members.Length; i++) grown[i] = set.Members[i];
                grown[set.Members.Length] = new SlimeSet.Member { IdentType = entry.Slime, Weight = weight };

                set.Members = grown;
                _patched++;

                // Location of every joined spawner, so a player can be told where to look. Capped:
                // a full ranch streams in hundreds of them.
                if (_patched <= LocationLogLimit)
                {
                    Vector3 position = spawner.transform.position;
                    MelonLoader.MelonLogger.Msg(
                        $"[SR2Kit] {entry.Slime.referenceId} spawns in {spawner.gameObject.scene.name} " +
                        $"at ({position.x:F0}, {position.y:F0}, {position.z:F0}), " +
                        $"weight {weight:0.###} against {TotalWeight(set) - weight:0.###}.");
                }
            }
        }
    }

    /// <summary>Sum of the weights already in a set, the scale a share is measured against.</summary>
    private static float TotalWeight(SlimeSet set)
    {
        float total = 0f;
        foreach (SlimeSet.Member member in set.Members)
        {
            if (member != null) total += member.Weight;
        }
        return total;
    }

    private static bool Hosts(SlimeSet set, string keyword)
    {
        foreach (SlimeSet.Member member in set.Members)
        {
            string refId = member?.IdentType?.ReferenceId;
            if (refId != null && refId.ToLowerInvariant().Contains(keyword)) return true;
        }
        return false;
    }

    private static bool Contains(SlimeSet set, IdentifiableType slime)
    {
        foreach (SlimeSet.Member member in set.Members)
        {
            if (member != null && member.IdentType == slime) return true;
        }
        return false;
    }
}

/// <summary>Catches every slime spawner as its cell streams in.</summary>
[HarmonyPatch(typeof(DirectedSlimeSpawner), nameof(DirectedSlimeSpawner.Register))]
internal static class Patch_DirectedSlimeSpawner_Register
{
    private static void Postfix(DirectedSlimeSpawner __instance) => SlimeSpawns.Inject(__instance);
}
