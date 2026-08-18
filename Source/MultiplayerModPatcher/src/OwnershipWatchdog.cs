using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using Il2Cpp;
using UnityEngine;

namespace MultiplayerModPatcher;

/// <summary>
/// Hands back the actors nobody is simulating any more — the slimes and plorts left hanging in
/// mid-air once two players walk away from each other.
///
/// Ranching Together freezes the rigidbody of every actor it does not own locally
/// (<c>RigidbodyConstraints.FreezeAll</c>) and moves it from the packets its owner sends. When an
/// owner walks far enough for its region to hibernate it stops sending and hands the actor back with
/// an unload packet — but that hand-back is dropped whenever the actor has no <c>RegionMember</c>,
/// and its periodic sweep only reclaims actors that are unowned, awake and within 600 units. An
/// actor that falls outside those conditions keeps naming an owner who no longer simulates it:
/// frozen, silent, hanging exactly where it was.
///
/// The watchdog applies the rule SR2MP already believes in — an owner who has not been heard from
/// has lost the actor — to every remote actor the local player is close enough to run, and claims
/// it. SR2MP then unfreezes the rigidbody itself, on the ownership change it was waiting for.
/// </summary>
internal static class OwnershipWatchdog
{
    /// <summary>Seconds between two summary lines in the log.</summary>
    private const float ReportInterval = 10f;

    private static PropertyInfo _locallyOwned;
    private static PropertyInfo _ownerRecentlyHeard;
    private static PropertyInfo _actorId;
    private static FieldInfo _currentOwnerId;
    private static FieldInfo _regionMember;
    private static FieldInfo _hibernating;

    private static FieldInfo _rigidbody;

    private static float _nextReport;
    private static int _seen;
    private static int _claimed;

    /// <summary>What was claimed since the last report, so the log names it rather than counting it.</summary>
    private static readonly Dictionary<string, int> Kinds = new();

    /// <summary>Actors left to describe in the log while a snapshot is being taken.</summary>
    private static int _snapshot;

    /// <summary>False turns the watchdog into a reporter, for telling a fix from a coincidence.</summary>
    public static bool Claiming { get; set; } = true;

    public static bool Install(HarmonyLib.Harmony harmony)
    {
        if (!SR2MPBridge.Available) return false;

        Type networkActor = SR2MPBridge.Type("SR2MP.Components.Actor.NetworkActor");
        MethodInfo update = networkActor?.GetMethod("Update", SR2MPBridge.Any);
        if (update == null)
        {
            Main.Log.Warning("Watchdog idle: NetworkActor.Update was not found.");
            return false;
        }

        _locallyOwned = networkActor.GetProperty("LocallyOwned", SR2MPBridge.Any);
        _ownerRecentlyHeard = networkActor.GetProperty("OwnerRecentlyHeard", SR2MPBridge.Any);
        _actorId = networkActor.GetProperty("ActorId", SR2MPBridge.Any);
        _currentOwnerId = networkActor.GetField("CurrentOwnerId", SR2MPBridge.Any);
        _regionMember = networkActor.GetField("RegionMember", SR2MPBridge.Any);

        if (_locallyOwned == null || _ownerRecentlyHeard == null || _actorId == null
            || _currentOwnerId == null || _regionMember == null)
        {
            Main.Log.Warning("Watchdog idle: NetworkActor is not laid out as expected.");
            return false;
        }

        _hibernating = _regionMember.FieldType.GetField("_hibernating", SR2MPBridge.Any);
        _rigidbody = networkActor.GetField("rigidbody", SR2MPBridge.Any);

        harmony.Patch(update, postfix: new HarmonyMethod(
            typeof(OwnershipWatchdog).GetMethod(nameof(AfterActorUpdate),
                BindingFlags.NonPublic | BindingFlags.Static)));

        Main.Log.Msg("Watchdog installed on Ranching Together's actors.");
        return true;
    }

    /// <summary>
    /// Runs after each actor ticks itself. Cheaper than sweeping the scene: the actors that matter
    /// are exactly the ones still updating, and each one only has to answer for itself.
    /// </summary>
    private static void AfterActorUpdate(object __instance)
    {
        try
        {
            if (__instance == null) return;

            if (_snapshot > 0) Describe(__instance);

            if (!Abandoned(__instance)) return;

            _seen++;
            Remember(__instance);

            if (Claiming && SR2MPBridge.Claim(__instance)) _claimed++;
        }
        catch
        {
            // A single malformed actor must not become a wall of errors, one per frame per actor.
        }
    }

    /// <summary>
    /// True for an actor this game shows but nobody simulates: owned elsewhere, its owner silent
    /// past SR2MP's own patience, and inside a region this game is running.
    /// </summary>
    private static bool Abandoned(object actor)
    {
        if (_locallyOwned.GetValue(actor) is true) return false;
        if (_ownerRecentlyHeard.GetValue(actor) is true) return false;

        // No owner at all is SR2MP's own sweep at work, and it handles that case itself.
        if (_currentOwnerId.GetValue(actor) is not string owner || owner.Length == 0) return false;

        // A hibernating region is not being simulated here either, so claiming would only move the
        // problem to this side of the wire.
        object region = _regionMember.GetValue(actor);
        if (region != null && _hibernating != null && _hibernating.GetValue(region) is true) return false;

        return true;
    }

    /// <summary>
    /// Asks for the next few actors to describe themselves in the log.
    ///
    /// Meant for the moment a player is looking at something hanging in the air: one key press turns
    /// what they see into the state that produced it, which is the only way to tell a stuck owner
    /// from a stuck region or a frozen rigidbody nobody claimed.
    /// </summary>
    public static void Snapshot(int count = 25)
    {
        _snapshot = count;
        Main.Log.Msg($"Describing up to {count} networked actors:");
    }

    private static void Describe(object actor)
    {
        _snapshot--;

        object region = _regionMember.GetValue(actor);
        object body = _rigidbody?.GetValue(actor);
        string owner = _currentOwnerId.GetValue(actor) as string;

        Main.Log.Msg($"  owner={(string.IsNullOrEmpty(owner) ? "none" : owner)} " +
                     $"mine={_locallyOwned.GetValue(actor)} " +
                     $"heard={_ownerRecentlyHeard.GetValue(actor)} " +
                     $"hibernating={(region == null || _hibernating == null ? "n/a" : _hibernating.GetValue(region))} " +
                     $"frozen={(body is Rigidbody rigidbody ? rigidbody.constraints.ToString() : "no body")} " +
                     $"at={((MonoBehaviour)actor).transform.position}");
    }

    /// <summary>Notes what kind of thing an abandoned actor was, for the next report line.</summary>
    private static void Remember(object actor)
    {
        Identifiable identifiable = ((MonoBehaviour)actor).GetComponent<Identifiable>();
        string kind = identifiable?.identType == null ? "unknown" : identifiable.identType.name;

        Kinds[kind] = Kinds.TryGetValue(kind, out int count) ? count + 1 : 1;
    }

    /// <summary>Logs what the watchdog has been doing, at most once every few seconds.</summary>
    public static void Report()
    {
        if (_seen == 0 || Time.time < _nextReport) return;

        _nextReport = Time.time + ReportInterval;
        Main.Log.Msg($"Watchdog: {_seen} actors left without a live owner, {_claimed} claimed back " +
                     $"({string.Join(", ", Kinds.Select(kind => $"{kind.Value} x {kind.Key}"))}).");

        _seen = 0;
        _claimed = 0;
        Kinds.Clear();
    }
}
