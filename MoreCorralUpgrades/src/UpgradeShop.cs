using System.Collections.Generic;
using Il2Cpp;
using Il2CppInterop.Runtime;
using Il2CppMonomiPark.SlimeRancher.Economy;
using Il2CppMonomiPark.SlimeRancher.UI.Plot;
using UnityEngine;

namespace MoreCorralUpgrades;

/// <summary>
/// Injects the mod's upgrades into the corral purchase menu.
///
/// Every vanilla entry is a <see cref="PlotUpgradePurchaseItemModel"/> ScriptableObject listed in a
/// <see cref="PlotPurchaseCategory"/>. Cloning one of them and swapping its upgrade value, cost and
/// strings is enough for the menu, the purchase flow and <c>LandPlot.AddUpgrade</c> to treat the new
/// entry exactly like a vanilla one.
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
        /// <summary>Vanilla upgrade whose icon is reused for this entry.</summary>
        public LandPlot.Upgrade IconSource;
        /// <summary>Extra condition for the entry to be purchasable, on top of "not owned yet".</summary>
        public System.Func<IPlotInfoProvider, bool> Prerequisite;
    }

    public static readonly List<Entry> Entries = new()
    {
        new Entry
        {
            Upgrade = Upgrades.AirNetBooster, Cost = 700,
            TitleKey = "mcu.air_net_booster.title", DescriptionKey = "mcu.air_net_booster.desc",
            IconSource = LandPlot.Upgrade.AIR_NET,
            Prerequisite = p => p.HasUpgrade(LandPlot.Upgrade.AIR_NET)
        },
        new Entry
        {
            Upgrade = Upgrades.PlortProtector, Cost = 1000,
            TitleKey = "mcu.plort_protector.title", DescriptionKey = "mcu.plort_protector.desc",
            IconSource = LandPlot.Upgrade.PLORT_COLLECTOR
        },
        new Entry
        {
            Upgrade = Upgrades.ProtectorBattery, Cost = 1250,
            TitleKey = "mcu.protector_battery.title", DescriptionKey = "mcu.protector_battery.desc",
            IconSource = LandPlot.Upgrade.SOLAR_SHIELD,
            Prerequisite = p => p.HasUpgrade(Upgrades.PlortProtector)
        },
        new Entry
        {
            Upgrade = Upgrades.MiniGarden, Cost = 400,
            TitleKey = "mcu.mini_garden.title", DescriptionKey = "mcu.mini_garden.desc",
            IconSource = LandPlot.Upgrade.SOIL
        },
        new Entry
        {
            Upgrade = Upgrades.CapacityBooster, Cost = 600,
            TitleKey = "mcu.capacity_booster.title", DescriptionKey = "mcu.capacity_booster.desc",
            IconSource = LandPlot.Upgrade.STORAGE_CAPACITY_INCREASE
        },
        new Entry
        {
            Upgrade = Upgrades.Miniturizer, Cost = 1500,
            TitleKey = "mcu.miniturizer.title", DescriptionKey = "mcu.miniturizer.desc",
            IconSource = LandPlot.Upgrade.VITAMIZER
        },
        new Entry
        {
            Upgrade = Upgrades.SlimeSprinkler, Cost = 750,
            TitleKey = "mcu.slime_sprinkler.title", DescriptionKey = "mcu.slime_sprinkler.desc",
            IconSource = LandPlot.Upgrade.SPRINKLER
        },
        new Entry
        {
            Upgrade = Upgrades.ClearCrops, Cost = 20,
            TitleKey = "mcu.clear_crops.title", DescriptionKey = "mcu.clear_crops.desc",
            IconSource = LandPlot.Upgrade.SOIL,
            Prerequisite = p => p.HasUpgrade(Upgrades.MiniGarden) && p.HasAttached()
        }
    };

    private static readonly Dictionary<int, Entry> ByUpgrade = new();

    private static bool _registered;

    public static bool TryGetEntry(LandPlot.Upgrade upgrade, out Entry entry)
        => ByUpgrade.TryGetValue((int)upgrade, out entry);

    /// <summary>Creates the shop entries. Called once the corral upgrade assets are loaded.</summary>
    public static void Register()
    {
        if (_registered) return;

        PlotUpgradePurchaseItemModel template = FindTemplate(LandPlot.Upgrade.AIR_NET);
        if (template == null) return;

        PlotPurchaseCategory category = FindCategoryContaining(template);
        if (category == null)
        {
            Main.Log.Error("Corral upgrade category not found; upgrades will not appear in the shop.");
            return;
        }

        Localization.Install(template._title.TableReference);

        foreach (Entry entry in Entries)
        {
            PlotUpgradePurchaseItemModel item = Object.Instantiate(template);
            item.hideFlags = HideFlags.HideAndDontSave;
            item.name = "MCU_" + entry.Upgrade;
            item._upgrade = entry.Upgrade;
            item._purchaseCost = PurchaseCost.FromCurrencyCosts(CurrencyCostEntry.CreateNewbucks(entry.Cost));
            item._title = Localization.Get(entry.TitleKey);
            item._description = Localization.Get(entry.DescriptionKey);
            item._pediaEntry = null;

            PlotUpgradePurchaseItemModel iconSource = FindTemplate(entry.IconSource);
            if (iconSource != null)
            {
                item._icon = iconSource._icon;
                item._unavailableIcon = iconSource._unavailableIcon;
                item._fullArtReference = iconSource._fullArtReference;
            }

            category.items.Add(item);
            ByUpgrade[(int)entry.Upgrade] = entry;
        }

        _registered = true;
        Main.Log.Msg($"Registered {Entries.Count} corral upgrades in the plot shop.");
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
