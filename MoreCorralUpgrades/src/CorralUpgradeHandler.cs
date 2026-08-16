using System;
using System.Collections.Generic;
using Il2Cpp;
using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.Attributes;
using MelonLoader;
using UnityEngine;

namespace MoreCorralUpgrades;

/// <summary>
/// Runs every custom upgrade of a single corral.
///
/// The vanilla upgrades each ship their own <c>PlotUpgrader</c> component baked into the plot
/// prefab. A mod cannot add components to those prefabs before they are instantiated, so instead one
/// handler is attached to each corral at runtime and re-reads the plot's upgrade set whenever the
/// game applies upgrades.
///
/// Actor tracking piggybacks on the corral's own <see cref="TrackContainedIdentifiables"/>, which
/// already knows everything inside the walls — no extra trigger colliders needed.
/// </summary>
[RegisterTypeInIl2Cpp]
public class CorralUpgradeHandler : MonoBehaviour
{
    private const float ProtectorTickSeconds = 0.5f;
    private const float SprinklerPeriodSeconds = 5f;
    private const float SprinklerWaterUnits = 1f;
    private const float BaseBatteryHours = 48f;
    private const float BatteryUpgradeFactor = 3f;
    private const float StorageCapacityFactor = 3f;
    private const float AirNetFactor = 2f;

    public CorralUpgradeHandler(IntPtr pointer) : base(pointer) { }

    private LandPlot _plot;
    private TrackContainedIdentifiables _tracker;
    private PlortProtectorBattery _battery;

    private bool _airNetApplied;
    private bool _capacityApplied;
    private bool _batteryChecked;

    private float _protectorTimer;
    private float _sprinklerTimer;

    /// <summary>Actors currently shrunk by the Miniturizer, and their untouched local scale.</summary>
    private readonly Dictionary<int, ShrinkState> _shrinking = new();

    /// <summary>Plorts whose <c>Edible</c> flag the Plort Protector turned off.</summary>
    private readonly Dictionary<int, Identifiable> _protected = new();

    private sealed class ShrinkState
    {
        public Transform Transform;
        public Vector3 OriginalScale;
        /// <summary>0 = original size, 1 = fully miniturized.</summary>
        public float Progress;
        public bool Growing;
    }

    private bool HasUpgrade(LandPlot.Upgrade upgrade) => _plot != null && _plot.HasUpgrade(upgrade);

    private void Awake()
    {
        _plot = GetComponent<LandPlot>();
        _tracker = GetComponentInChildren<TrackContainedIdentifiables>(true);
    }

    /// <summary>Called after the game applied upgrades to the plot, and once at attach time.</summary>
    public void Refresh()
    {
        if (_plot == null) return;
        if (_tracker == null) _tracker = GetComponentInChildren<TrackContainedIdentifiables>(true);

        if (HasUpgrade(Upgrades.AirNetBooster)) ApplyAirNetBooster();
        if (HasUpgrade(Upgrades.CapacityBooster)) ApplyCapacityBooster();
        if (HasUpgrade(Upgrades.PlortProtector)) EnsureBattery();
        if (HasUpgrade(Upgrades.MiniGarden)) InternalGarden.Build(_plot);
        if (HasUpgrade(Upgrades.ClearCrops)) ClearCrops();
    }

    private void Update()
    {
        if (_plot == null) return;

        if (HasUpgrade(Upgrades.Miniturizer) || _shrinking.Count > 0) UpdateMiniturizer();

        _protectorTimer += Time.deltaTime;
        if (_protectorTimer >= ProtectorTickSeconds)
        {
            _protectorTimer = 0f;
            UpdatePlortProtector();
        }

        if (HasUpgrade(Upgrades.SlimeSprinkler))
        {
            _sprinklerTimer += Time.deltaTime;
            if (_sprinklerTimer >= SprinklerPeriodSeconds)
            {
                _sprinklerTimer = 0f;
                Sprinkle();
            }
        }
    }

    private void OnDestroy()
    {
        foreach (ShrinkState state in _shrinking.Values)
        {
            if (state.Transform != null) state.Transform.localScale = state.OriginalScale;
        }
        _shrinking.Clear();

        foreach (Identifiable plort in _protected.Values)
        {
            if (plort != null) plort.Edible = true;
        }
        _protected.Clear();
    }

    // ---------------------------------------------------------------- Air Net

    private void ApplyAirNetBooster()
    {
        if (_airNetApplied) return;

        foreach (AirNet net in GetComponentsInChildren<AirNet>(true))
        {
            net.HitForceToDestroy = Mathf.Max(1, (int)(net.HitForceToDestroy / AirNetFactor));
            net._dmgPerImpulse /= AirNetFactor;
        }
        _airNetApplied = true;
    }

    // ---------------------------------------------------------------- Storage

    private void ApplyCapacityBooster()
    {
        if (_capacityApplied) return;

        foreach (SiloStorage storage in GetComponentsInChildren<SiloStorage>(true))
        {
            if (storage.Ammo == null) continue;
            int max = storage.GetSlotMaxCount(0);
            if (max <= 0) continue;
            storage.Ammo.UpdateMaxCount((int)(max * StorageCapacityFactor));
        }
        _capacityApplied = true;
    }

    // ---------------------------------------------------------------- Sprinkler

    private void Sprinkle()
    {
        LiquidDefinition water = GameResources.Water;
        if (water == null || _tracker == null || _tracker._collider == null) return;

        Bounds bounds = _tracker._collider.bounds;
        ILiquidConsumer.ApplyLiquid(bounds.center, bounds.extents.magnitude, water, SprinklerWaterUnits);
    }

    // ---------------------------------------------------------------- Miniturizer

    private void UpdateMiniturizer()
    {
        bool active = HasUpgrade(Upgrades.Miniturizer);
        float step = Time.deltaTime / Mathf.Max(0.01f, Config.MiniturizerAnimationTime);

        // Anything still inside the corral keeps shrinking, anything that left grows back.
        foreach (ShrinkState state in _shrinking.Values) state.Growing = true;

        if (active)
        {
            foreach (Identifiable actor in TrackedActors())
            {
                if (!IsShrinkable(actor)) continue;
                int key = actor.GetInstanceID();
                if (!_shrinking.TryGetValue(key, out ShrinkState state))
                {
                    state = new ShrinkState
                    {
                        Transform = actor.transform,
                        OriginalScale = actor.transform.localScale
                    };
                    _shrinking[key] = state;
                }
                state.Growing = false;
            }
        }

        List<int> finished = null;
        foreach (KeyValuePair<int, ShrinkState> pair in _shrinking)
        {
            ShrinkState state = pair.Value;
            if (state.Transform == null)
            {
                (finished ??= new List<int>()).Add(pair.Key);
                continue;
            }

            state.Progress = Mathf.Clamp01(state.Progress + (state.Growing ? -step : step));
            state.Transform.localScale =
                state.OriginalScale * Mathf.Lerp(1f, Config.MiniturizerSize, state.Progress);

            if (state.Growing && state.Progress <= 0f)
            {
                state.Transform.localScale = state.OriginalScale;
                (finished ??= new List<int>()).Add(pair.Key);
            }
        }

        if (finished != null)
        {
            foreach (int key in finished) _shrinking.Remove(key);
        }
    }

    private static bool IsShrinkable(Identifiable actor)
    {
        IdentifiableType type = actor.identType;
        if (type == null) return false;

        // Slimes, plorts, animals, toys and anything edible (fruit, veggies, meat).
        return type.TryCast<SlimeDefinition>() != null
               || type.TryCast<ToyDefinition>() != null
               || type.IsPlort
               || type.IsAnimal
               || actor.Edible;
    }

    // ---------------------------------------------------------------- Plort Protector

    private void UpdatePlortProtector()
    {
        // Without a working liquid consumer the battery could never be refilled, so the protector
        // stays permanently on instead of being dead weight.
        bool charged = (_batteryChecked && _battery == null) || BatteryCharge() > 0f;
        bool active = HasUpgrade(Upgrades.PlortProtector) && charged;

        if (active)
        {
            foreach (Identifiable actor in TrackedActors())
            {
                if (actor.identType == null || !actor.identType.IsPlort) continue;
                int key = actor.GetInstanceID();
                if (_protected.ContainsKey(key)) continue;
                actor.Edible = false;
                _protected[key] = actor;
            }
            return;
        }

        if (_protected.Count == 0) return;
        foreach (Identifiable plort in _protected.Values)
        {
            if (plort != null) plort.Edible = true;
        }
        _protected.Clear();
    }

    private void EnsureBattery()
    {
        if (_batteryChecked) return;
        _batteryChecked = true;
        _battery = PlortProtectorBattery.AttachTo(this);
    }

    /// <summary>Battery capacity in in-game hours, tripled by the Battery Upgrade.</summary>
    public float BatteryCapacityHours =>
        HasUpgrade(Upgrades.ProtectorBattery) ? BaseBatteryHours * BatteryUpgradeFactor : BaseBatteryHours;

    /// <summary>
    /// Remaining charge in [0,1]. Stored as a world time in the plot model's
    /// <c>attachedDeathTime</c>, which is already persisted by the save system.
    /// </summary>
    public float BatteryCharge()
    {
        if (_plot == null || _plot._model == null) return 0f;
        TimeDirector time = SceneContext.Instance?.TimeDirector;
        if (time == null) return 0f;
        return Mathf.Clamp01((float)(time.HoursUntil(_plot._model.attachedDeathTime) / BatteryCapacityHours));
    }

    /// <summary>Adds water to the battery. <paramref name="units"/> is in vac units.</summary>
    public void ChargeBattery(float units)
    {
        if (_plot == null || _plot._model == null) return;
        TimeDirector time = SceneContext.Instance?.TimeDirector;
        if (time == null) return;

        float capacity = BatteryCapacityHours;
        float charge = Mathf.Clamp01(BatteryCharge() + 4f / capacity * units);
        _plot._model.attachedDeathTime = time.HoursFromNow(charge * capacity);
    }

    // ---------------------------------------------------------------- Clear crops

    private void ClearCrops()
    {
        _plot.DestroyAttached();
        if (_plot._model != null) _plot._model.upgrades.Remove(Upgrades.ClearCrops);
    }

    // ---------------------------------------------------------------- Helpers

    /// <summary>Every identifiable the corral currently contains.</summary>
    [HideFromIl2Cpp]
    private IEnumerable<Identifiable> TrackedActors()
    {
        if (_tracker == null || _tracker._trackedObjects == null) yield break;

        foreach (Il2CppSystem.Collections.Generic.KeyValuePair<IdentifiableType, Il2CppSystem.Collections.Generic.HashSet<Identifiable>> pair
                 in _tracker._trackedObjects)
        {
            if (pair.Value == null) continue;
            foreach (Identifiable actor in pair.Value)
            {
                if (actor != null) yield return actor;
            }
        }
    }

    /// <summary>Attaches (or returns) the handler of a corral.</summary>
    public static CorralUpgradeHandler EnsureOn(LandPlot plot)
    {
        if (plot == null || plot.TypeId != LandPlot.Id.CORRAL) return null;

        CorralUpgradeHandler handler = plot.GetComponent<CorralUpgradeHandler>();
        if (handler == null)
            handler = plot.gameObject.AddComponent(Il2CppType.Of<CorralUpgradeHandler>())
                          .Cast<CorralUpgradeHandler>();
        return handler;
    }
}
