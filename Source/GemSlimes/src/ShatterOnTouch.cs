using System;
using Il2Cpp;
using Il2CppInterop.Runtime.Attributes;
using Il2CppMonomiPark.SlimeRancher;
using Il2CppMonomiPark.SlimeRancher.Player;
using Il2CppMonomiPark.SlimeRancher.Regions;
using MelonLoader;
using UnityEngine;

namespace GemSlimes;

/// <summary>
/// The gem gimmick: a crystal gem is brittle, so bumping into it breaks it into its plort.
///
/// Slime Rancher 1 got this for free by implementing the game's <c>ControllerCollisionListener</c>
/// interface, which the player's character controller calls on whatever it walks into. Il2CppInterop
/// emits Slime Rancher 2's <c>IControllerCollisionListener</c> as a class rather than a C# interface,
/// so an injected managed behaviour cannot implement it; a short-range check against the player
/// reproduces the same trigger without touching the game's collision code.
/// </summary>
[RegisterTypeInIl2Cpp]
public sealed class ShatterOnTouch : MonoBehaviour
{
    public ShatterOnTouch(IntPtr pointer) : base(pointer) { }

    /// <summary>Roughly the player's capsule plus a slime — the distance a bump happens at.</summary>
    private const float TouchRadius = 1.0f;

    /// <summary>The player cannot cross a metre between checks, so polling beats a per-frame test.</summary>
    private const float CheckInterval = 0.15f;

    private float _nextCheck;

    private void Update()
    {
        if (Time.time < _nextCheck) return;
        _nextCheck = Time.time + CheckInterval;

        Transform player = Main.PlayerTransform;
        if (player == null) return;

        // A gem held in the vacpack sits on top of the player; it breaks when walked into, not when
        // it is being carried, launched or stored.
        if (Vacuumable.TryGetVacuumable(gameObject, out Vacuumable vacuumable) && vacuumable != null
            && (vacuumable.IsHeld() || vacuumable.IsCaptive() || vacuumable.IsLaunched()))
            return;

        if ((player.position - transform.position).sqrMagnitude > TouchRadius * TouchRadius) return;

        Shatter();
    }

    [HideFromIl2Cpp]
    private void Shatter()
    {
        Identifiable identifiable = GetComponent<Identifiable>();
        IdentifiableType plort = Main.PlortOf(identifiable?.identType);

        if (plort?.prefab != null)
        {
            RegionMember member = GetComponent<RegionMember>();
            GameObject shard = InstantiationHelpers.InstantiateActor(
                plort.prefab, member?.SceneGroup, transform.position, transform.rotation,
                false, SlimeAppearance.AppearanceSaveSet.NONE, SlimeAppearance.AppearanceSaveSet.NONE,
                new Il2CppSystem.Nullable<AmmoSlot.AmmoMetadata>(), false, false);

            if (shard != null && !shard.activeSelf) shard.SetActive(true);
        }
        else
        {
            MelonLogger.Warning("[GemSlimes] A gem shattered with no plort to leave behind.");
        }

        Destroyer.DestroyActor(gameObject, "GemSlimes_Shatter", false);
    }
}
