using HarmonyLib;
using Il2Cpp;
using Il2CppMonomiPark.SlimeRancher.UI.Plot;
using Il2CppMonomiPark.SlimeRancher.World;

namespace PondUpgrades;

/// <summary>Attaches the handler to every pond and keeps it in sync with the plot's upgrades.</summary>
[HarmonyPatch(typeof(LandPlot), nameof(LandPlot.SetModel))]
internal static class Patch_LandPlot_SetModel
{
    private static void Postfix(LandPlot __instance) => PondUpgradeHandler.EnsureOn(__instance)?.Refresh();
}

[HarmonyPatch(typeof(LandPlot), nameof(LandPlot.ApplyUpgrades))]
internal static class Patch_LandPlot_ApplyUpgrades
{
    private static void Postfix(LandPlot __instance) => PondUpgradeHandler.EnsureOn(__instance)?.Refresh();
}

[HarmonyPatch(typeof(LandPlot), nameof(LandPlot.AddUpgrade))]
internal static class Patch_LandPlot_AddUpgrade
{
    private static void Postfix(LandPlot __instance) => PondUpgradeHandler.EnsureOn(__instance)?.Refresh();
}

/// <summary>
/// The vanilla availability rules know nothing about the mod's upgrades, so their purchase entries
/// get their own rules: buyable once, and hidden until their prerequisites are owned.
/// </summary>
[HarmonyPatch(typeof(PlotUpgradePurchaseItemModel), nameof(PlotUpgradePurchaseItemModel.UpdateAvailability))]
internal static class Patch_UpdateAvailability
{
    private static void Postfix(PlotUpgradePurchaseItemModel __instance, IPlotInfoProvider plotInfoProvider)
    {
        if (!UpgradeShop.TryGetEntry(__instance._upgrade, out UpgradeShop.Entry _)) return;

        bool available = !plotInfoProvider.HasUpgrade(__instance._upgrade);
        __instance.IsAvailable = UpgradeShop.Func(() => available);
    }
}

[HarmonyPatch(typeof(PlotUpgradePurchaseItemModel), nameof(PlotUpgradePurchaseItemModel.UpdateHidden))]
internal static class Patch_UpdateHidden
{
    private static void Postfix(PlotUpgradePurchaseItemModel __instance, IPlotInfoProvider plotInfoProvider)
    {
        if (!UpgradeShop.TryGetEntry(__instance._upgrade, out UpgradeShop.Entry entry)) return;

        bool hidden = plotInfoProvider.HasUpgrade(__instance._upgrade)
                      || (entry.Prerequisite != null && !entry.Prerequisite(plotInfoProvider));
        __instance.IsHidden = UpgradeShop.Func(() => hidden);
    }
}

/// <summary>Registers the shop entries as soon as the plot upgrade assets exist in memory.</summary>
[HarmonyPatch(typeof(LandPlotUIRoot), nameof(LandPlotUIRoot.SetActivator))]
internal static class Patch_LandPlotUIRoot_SetActivator
{
    private static void Prefix() => UpgradeShop.Register();
}

/// <summary>Slime Capacity doubles the pond's slime density, Ancient Blessing multiplies it by six.</summary>
[HarmonyPatch(typeof(SlimeEatWater), nameof(SlimeEatWater.CalcMaximumSlimeDensity))]
internal static class Patch_CalcMaximumSlimeDensity
{
    private static void Postfix(SlimeEatWater __instance, ref int __result)
        => __result *= Density.Multiplier(__instance, Upgrades.SlimeCapacity);
}

/// <summary>Plort Capacity doubles the pond's plort density, Ancient Blessing multiplies it by six.</summary>
[HarmonyPatch(typeof(SlimeEatWater), nameof(SlimeEatWater.CalcMaximumPlortDensity))]
internal static class Patch_CalcMaximumPlortDensity
{
    private static void Postfix(SlimeEatWater __instance, ref int __result)
        => __result *= Density.Multiplier(__instance, Upgrades.PlortCapacity);
}

internal static class Density
{
    /// <summary>
    /// Highest multiplier granted by the ponds the slime is standing in: x2 for the matching
    /// capacity upgrade, x6 for the Ancient Blessing.
    /// </summary>
    internal static int Multiplier(SlimeEatWater eater, LandPlot.Upgrade capacityUpgrade)
    {
        if (eater._waters == null) return 1;

        int multiplier = 1;
        foreach (LiquidSourceSurface water in eater._waters)
        {
            if (water == null) continue;
            LandPlot plot = water.GetComponentInParent<LandPlot>();
            if (plot == null) continue;

            if (multiplier < 2 && plot.HasUpgrade(capacityUpgrade)) multiplier = 2;
            if (multiplier < 6 && plot.HasUpgrade(Upgrades.AncientBlessing)) multiplier = 6;
        }
        return multiplier;
    }
}
