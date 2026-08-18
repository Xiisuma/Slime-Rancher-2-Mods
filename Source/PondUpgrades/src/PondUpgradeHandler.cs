using System;
using Il2Cpp;
using Il2CppInterop.Runtime;
using MelonLoader;
using UnityEngine;

namespace PondUpgrades;

/// <summary>
/// Applies the visual part of the pond upgrades. The capacity part is handled by the
/// <see cref="Patches"/> on <c>SlimeEatWater</c>, which read the plot's upgrades directly.
/// </summary>
[RegisterTypeInIl2Cpp]
public class PondUpgradeHandler : MonoBehaviour
{
    public PondUpgradeHandler(IntPtr pointer) : base(pointer) { }

    private LandPlot _plot;
    private bool _blessed;

    private void Awake() => _plot = GetComponent<LandPlot>();

    /// <summary>Called after the game applied upgrades to the plot, and once at attach time.</summary>
    public void Refresh()
    {
        if (_plot == null || _blessed) return;
        if (!_plot.HasUpgrade(Upgrades.AncientBlessing)) return;

        Material material = GameResources.AncientWaterMaterial;
        if (material == null)
        {
            Main.Log.Warning("Ancient water material not found; the pond keeps its normal water.");
            _blessed = true;
            return;
        }

        foreach (MeshRenderer renderer in GetComponentsInChildren<MeshRenderer>(true))
        {
            // The pond surface is the only renderer sitting under the water scaler.
            if (renderer.transform.parent == null) continue;
            if (!renderer.name.ToLowerInvariant().Contains("surface")) continue;
            renderer.material = material;
        }
        _blessed = true;
    }

    /// <summary>Attaches (or returns) the handler of a pond.</summary>
    public static PondUpgradeHandler EnsureOn(LandPlot plot)
    {
        if (plot == null || plot.TypeId != LandPlot.Id.POND) return null;

        PondUpgradeHandler handler = plot.GetComponent<PondUpgradeHandler>();
        if (handler == null)
            handler = plot.gameObject.AddComponent(Il2CppType.Of<PondUpgradeHandler>())
                          .Cast<PondUpgradeHandler>();
        return handler;
    }
}
