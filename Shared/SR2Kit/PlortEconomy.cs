using System.Collections.Generic;
using HarmonyLib;
using Il2Cpp;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Il2CppMonomiPark.SlimeRancher.Economy;
using Il2CppMonomiPark.SlimeRancher.UI;
using MelonLoader;
using UnityEngine;

namespace SR2Kit;

/// <summary>Adds a modded plort to the market so it can be sold and priced like a vanilla one.</summary>
public static class PlortEconomy
{
    /// <summary>Every modded plort registered so far, in registration order.</summary>
    private static readonly List<IdentifiableType> Priced = new();

    /// <summary>The modded plorts, for the market board to make room for.</summary>
    internal static IReadOnlyList<IdentifiableType> Plorts => Priced;

    /// <summary>
    /// Registers <paramref name="plort"/> at <paramref name="value"/> newbucks. Saturation defaults
    /// to five times the value, matching the ratio the vanilla plorts use.
    /// </summary>
    public static void Register(PlortEconomyDirector director, IdentifiableType plort, float value,
        float fullSaturation = 0f)
    {
        if (director == null || plort == null) return;
        if (fullSaturation <= 0f) fullSaturation = value * 5f;

        PlortEconomySettings settings = director._settings;
        if (settings == null || settings.PlortsTable.Plorts == null) return;

        Il2CppReferenceArray<PlortValueConfiguration> plorts = settings.PlortsTable.Plorts;
        foreach (PlortValueConfiguration configuration in plorts)
        {
            if (configuration != null && configuration.Type == plort) return;
        }

        Il2CppReferenceArray<PlortValueConfiguration> grown = new(plorts.Length + 1);
        for (int i = 0; i < plorts.Length; i++) grown[i] = plorts[i];
        grown[plorts.Length] = new PlortValueConfiguration
        {
            Type = plort,
            InitialValue = value,
            FullSaturation = fullSaturation
        };

        PlortValueConfigurationTable table = settings.PlortsTable;
        table.Plorts = grown;
        settings.PlortsTable = table;

        if (!director._currValueMap.ContainsKey(plort))
            director._currValueMap.Add(plort, new PlortEconomyDirector.CurrValueEntry(value, value, value, fullSaturation));

        if (!Priced.Contains(plort)) Priced.Add(plort);
        AddToMarketUI(plort);
        MarketCleanup.Track(plort);
        MelonLogger.Msg($"[SR2Kit] Registered {plort.referenceId} in the market at {value} newbucks.");
    }

    /// <summary>Lists the plort in the market terminal's UI.</summary>
    internal static void AddToMarketUI(IdentifiableType plort)
    {
        foreach (MarketUIConfiguration configuration in Resources.FindObjectsOfTypeAll<MarketUIConfiguration>())
        {
            Il2CppReferenceArray<PlortEntry> entries = configuration._plorts;
            if (entries == null) continue;

            bool present = false;
            foreach (PlortEntry entry in entries)
            {
                if (entry == null || entry.IdentType != plort) continue;
                present = true;
                break;
            }
            if (present) continue;

            Il2CppReferenceArray<PlortEntry> grown = new(entries.Length + 1);
            for (int i = 0; i < entries.Length; i++) grown[i] = entries[i];
            grown[entries.Length] = new PlortEntry { IdentType = plort };
            configuration._plorts = grown;
        }
    }
}

/// <summary>
/// Makes the market board show the modded plorts.
///
/// The board is built once, from a list of plorts and a set of panels that each declare how many
/// rows they hold: a plort past the last row is priced and sold, but never displayed. Both halves
/// are therefore settled before the terminal wakes up — the list is completed, and the panels are
/// given as many extra rows as the list has gained, spread evenly so no single board grows a tail.
/// </summary>
[HarmonyPatch(typeof(MarketUI), nameof(MarketUI.Awake))]
internal static class Patch_MarketUI_Awake
{
    private static void Prefix(MarketUI __instance)
    {
        foreach (IdentifiableType plort in PlortEconomy.Plorts) PlortEconomy.AddToMarketUI(plort);

        MarketUIConfiguration configuration = __instance._config;
        Il2CppReferenceArray<MarketUI.PricesPanelEntry> panels = __instance.pricesPanels;
        if (configuration?._plorts == null || panels == null || panels.Length == 0) return;

        int rows = 0;
        foreach (MarketUI.PricesPanelEntry panel in panels)
        {
            if (panel != null) rows += panel.entryCount;
        }

        int missing = configuration._plorts.Length - rows;
        for (int i = 0; i < missing; i++)
        {
            MarketUI.PricesPanelEntry panel = panels[i % panels.Length];
            if (panel != null) panel.entryCount++;
        }

        if (missing > 0)
            MelonLogger.Msg($"[SR2Kit] Market board grown by {missing} rows for the modded plorts.");
    }
}
