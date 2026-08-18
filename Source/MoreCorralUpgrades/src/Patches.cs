using HarmonyLib;
using Il2Cpp;
using Il2CppMonomiPark.SlimeRancher.UI.Plot;

namespace MoreCorralUpgrades;

/// <summary>Attaches the handler to every corral and keeps it in sync with the plot's upgrades.</summary>
[HarmonyPatch(typeof(LandPlot), nameof(LandPlot.SetModel))]
internal static class Patch_LandPlot_SetModel
{
    private static void Postfix(LandPlot __instance)
    {
        CorralUpgradeHandler.EnsureOn(__instance)?.Refresh();
    }
}

[HarmonyPatch(typeof(LandPlot), nameof(LandPlot.ApplyUpgrades))]
internal static class Patch_LandPlot_ApplyUpgrades
{
    private static void Postfix(LandPlot __instance)
    {
        CorralUpgradeHandler.EnsureOn(__instance)?.Refresh();
    }
}

[HarmonyPatch(typeof(LandPlot), nameof(LandPlot.AddUpgrade))]
internal static class Patch_LandPlot_AddUpgrade
{
    private static void Postfix(LandPlot __instance)
    {
        CorralUpgradeHandler.EnsureOn(__instance)?.Refresh();
    }
}

/// <summary>
/// The vanilla availability rules know nothing about the mod's upgrades, so their purchase entries
/// get their own rules: buyable once, and only when their prerequisite upgrade is owned.
/// </summary>
[HarmonyPatch(typeof(PlotUpgradePurchaseItemModel), nameof(PlotUpgradePurchaseItemModel.UpdateAvailability))]
internal static class Patch_UpdateAvailability
{
    private static void Postfix(PlotUpgradePurchaseItemModel __instance, IPlotInfoProvider plotInfoProvider)
    {
        if (!UpgradeShop.TryGetEntry(__instance._upgrade, out UpgradeShop.Entry entry)) return;

        // Evaluated on every call rather than captured: the menu asks again after a purchase, and a
        // snapshot taken when the page was built would still claim the upgrade is for sale.
        LandPlot.Upgrade upgrade = __instance._upgrade;
        __instance.IsAvailable = UpgradeShop.Func(() =>
            !plotInfoProvider.HasUpgrade(upgrade)
            && (entry.Prerequisite == null || entry.Prerequisite(plotInfoProvider)));
    }
}

[HarmonyPatch(typeof(PlotUpgradePurchaseItemModel), nameof(PlotUpgradePurchaseItemModel.UpdateHidden))]
internal static class Patch_UpdateHidden
{
    private static void Postfix(PlotUpgradePurchaseItemModel __instance, IPlotInfoProvider plotInfoProvider)
    {
        if (!UpgradeShop.TryGetEntry(__instance._upgrade, out UpgradeShop.Entry entry)) return;

        // Owned upgrades stay on the page: that is where the game draws the "bought" check mark.
        // Only an upgrade whose prerequisite is missing is worth hiding.
        __instance.IsHidden = UpgradeShop.Func(() =>
            entry.Prerequisite != null
            && !plotInfoProvider.HasUpgrade(__instance._upgrade)
            && !entry.Prerequisite(plotInfoProvider));
    }
}

/// <summary>Registers the shop entries as soon as the plot upgrade assets exist in memory.</summary>
[HarmonyPatch(typeof(LandPlotUIRoot), nameof(LandPlotUIRoot.SetActivator))]
internal static class Patch_LandPlotUIRoot_SetActivator
{
    private static void Prefix()
    {
        UpgradeShop.Register();
    }
}
