using System;
using Il2Cpp;
using Il2CppInterop.Runtime;
using MelonLoader;
using UnityEngine;

namespace MoreCorralUpgrades;

/// <summary>
/// Makes the corral accept water, which charges the Plort Protector battery.
///
/// <c>ILiquidConsumer.ApplyLiquid</c> looks for consumers around the impact point, so implementing
/// the game's interface is what lets the player refill the protector with the vacpack.
/// </summary>
[RegisterTypeInIl2CppWithInterfaces(typeof(ILiquidConsumer))]
public class PlortProtectorBattery : MonoBehaviour
{
    public PlortProtectorBattery(IntPtr pointer) : base(pointer) { }

    private CorralUpgradeHandler _handler;

    public void AddLiquid(LiquidDefinition liquidId, float units, Vector3 center)
    {
        if (liquidId == null || !liquidId.IsWater) return;
        if (_handler == null) _handler = GetComponent<CorralUpgradeHandler>();
        _handler?.ChargeBattery(units);
    }

    /// <summary>Adds the consumer to the corral, or returns the existing one.</summary>
    public static PlortProtectorBattery AttachTo(CorralUpgradeHandler handler)
    {
        try
        {
            PlortProtectorBattery battery = handler.GetComponent<PlortProtectorBattery>();
            if (battery == null)
                battery = handler.gameObject.AddComponent(Il2CppType.Of<PlortProtectorBattery>())
                                 .Cast<PlortProtectorBattery>();
            return battery;
        }
        catch (Exception e)
        {
            Main.Log.Warning($"Could not attach the Plort Protector battery ({e.Message}); " +
                             "the protector will run without needing water.");
            return null;
        }
    }
}
