using System.Collections.Generic;
using Il2Cpp;
using Il2CppInterop.Runtime;
using Il2CppMonomiPark.SlimeRancher.Economy;
using Il2CppMonomiPark.SlimeRancher.UI.Plot;
using UnityEngine;

namespace PondUpgrades;

/// <summary>
/// Injects the mod's upgrades into the pond purchase menu.
///
/// Every vanilla entry is a <see cref="PlotUpgradePurchaseItemModel"/> ScriptableObject listed in a
/// <see cref="PlotPurchaseCategory"/>. Cloning the pond's own entry and swapping its upgrade value,
/// cost and strings is enough for the menu, the purchase flow and <c>LandPlot.AddUpgrade</c> to treat
/// the new entry exactly like a vanilla one.
/// </summary>
public static class UpgradeShop
{
    /// <summary>Definition of one shop entry created by the mod.</summary>
    public sealed class Entry
    {
        public LandPlot.Upgrade Upgrade;
        public int Cost;
        public string TitleKey;
        public string DescriptionKey;
        /// <summary>Extra condition for the entry to be shown, on top of "not owned yet".</summary>
        public System.Func<IPlotInfoProvider, bool> Prerequisite;
    }

    /// <summary>The vanilla pond upgrade whose asset is cloned and whose category is reused.</summary>
    private const LandPlot.Upgrade PondTemplateUpgrade = LandPlot.Upgrade.PLORT_COLLECTOR_POND;

    private const int BaseCost = 500;

    public static readonly List<Entry> Entries = new()
    {
        new Entry
        {
            Upgrade = Upgrades.SlimeCapacity, Cost = BaseCost * 2,
            TitleKey = "spu.slime_capacity.title", DescriptionKey = "spu.slime_capacity.desc"
        },
        new Entry
        {
            Upgrade = Upgrades.PlortCapacity, Cost = BaseCost * 2,
            TitleKey = "spu.plort_capacity.title", DescriptionKey = "spu.plort_capacity.desc"
        },
        new Entry
        {
            Upgrade = Upgrades.AncientBlessing, Cost = BaseCost * 10,
            TitleKey = "spu.ancient_blessing.title", DescriptionKey = "spu.ancient_blessing.desc",
            Prerequisite = p => p.HasUpgrade(Upgrades.SlimeCapacity) && p.HasUpgrade(Upgrades.PlortCapacity)
        }
    };

    private static readonly Dictionary<int, Entry> ByUpgrade = new();

    private static bool _registered;

    public static bool TryGetEntry(LandPlot.Upgrade upgrade, out Entry entry)
        => ByUpgrade.TryGetValue((int)upgrade, out entry);

    /// <summary>Creates the shop entries. Called once the plot upgrade assets are loaded.</summary>
    public static void Register()
    {
        if (_registered) return;

        PlotUpgradePurchaseItemModel template = FindTemplate(PondTemplateUpgrade);
        if (template == null) return;

        PlotPurchaseCategory category = FindCategoryContaining(template);
        if (category == null)
        {
            Main.Log.Error("Pond upgrade category not found; upgrades will not appear in the shop.");
            return;
        }

        Localization.Install(template._title.TableReference);
        Sprite ancientIcon = GameResources.AncientWaterIcon;

        foreach (Entry entry in Entries)
        {
            PlotUpgradePurchaseItemModel item = Object.Instantiate(template);
            item.hideFlags = HideFlags.HideAndDontSave;
            item.name = "SPU_" + entry.Upgrade;
            item._upgrade = entry.Upgrade;
            item._purchaseCost = PurchaseCost.FromCurrencyCosts(CurrencyCostEntry.CreateNewbucks(entry.Cost));
            item._title = Localization.Get(entry.TitleKey);
            item._description = Localization.Get(entry.DescriptionKey);
            item._pediaEntry = null;

            if (entry.Upgrade == Upgrades.AncientBlessing && ancientIcon != null)
                item._icon = ancientIcon;

            category.items.Add(item);
            ByUpgrade[(int)entry.Upgrade] = entry;
        }

        _registered = true;
        Main.Log.Msg($"Registered {Entries.Count} pond upgrades in the plot shop.");
    }

    private static PlotUpgradePurchaseItemModel FindTemplate(LandPlot.Upgrade upgrade)
    {
        foreach (PlotUpgradePurchaseItemModel model in
                 Resources.FindObjectsOfTypeAll<PlotUpgradePurchaseItemModel>())
        {
            if (model._upgrade == upgrade && !Upgrades.IsCustom(model._upgrade))
                return model;
        }
        return null;
    }

    private static PlotPurchaseCategory FindCategoryContaining(PlotPurchaseItemModel item)
    {
        foreach (PlotPurchaseCategory category in Resources.FindObjectsOfTypeAll<PlotPurchaseCategory>())
        {
            if (category.items == null) continue;
            foreach (PlotPurchaseItemModel candidate in category.items)
            {
                if (candidate != null && candidate.Pointer == item.Pointer)
                    return category;
            }
        }
        return null;
    }

    /// <summary>Wraps a managed predicate so the game's UI can call it.</summary>
    public static Il2CppSystem.Func<bool> Func(System.Func<bool> predicate)
        => DelegateSupport.ConvertDelegate<Il2CppSystem.Func<bool>>(predicate);
}
