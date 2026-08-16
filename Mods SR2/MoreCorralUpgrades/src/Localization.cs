using System.Collections.Generic;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;

namespace MoreCorralUpgrades;

/// <summary>
/// Adds the mod's strings to the string table the vanilla plot upgrades already use, so the
/// purchase UI can display them like any other entry.
/// </summary>
public static class Localization
{
    private static readonly Dictionary<string, string> English = new()
    {
        ["mcu.air_net_booster.title"] = "Air Net Upgrade",
        ["mcu.air_net_booster.desc"] = "Increases the strength of the air net so that it can take more hits.",
        ["mcu.plort_protector.title"] = "Plort Protector",
        ["mcu.plort_protector.desc"] = "Prevents slimes inside the corral from eating plorts while its battery is charged. Recharge it with water.",
        ["mcu.protector_battery.title"] = "Battery Upgrade",
        ["mcu.protector_battery.desc"] = "Triples the capacity of the Plort Protector's battery.",
        ["mcu.mini_garden.title"] = "Internal Garden",
        ["mcu.mini_garden.desc"] = "Adds a garden inside the corral.",
        ["mcu.capacity_booster.title"] = "Increase Storage Capacity",
        ["mcu.capacity_booster.desc"] = "Triples the capacity of every storage on the corral.",
        ["mcu.miniturizer.title"] = "Miniturizer",
        ["mcu.miniturizer.desc"] = "Reduces the size of the slimes, toys and food that enter the corral.",
        ["mcu.slime_sprinkler.title"] = "Slime Sprinkler",
        ["mcu.slime_sprinkler.desc"] = "Regularly sprinkles the slimes inside the corral with water.",
        ["mcu.clear_crops.title"] = "Clear Crops",
        ["mcu.clear_crops.desc"] = "Removes the crop from the Internal Garden."
    };

    private static readonly Dictionary<string, string> French = new()
    {
        ["mcu.air_net_booster.title"] = "Filet renforcé",
        ["mcu.air_net_booster.desc"] = "Renforce le filet aérien pour qu'il encaisse davantage de coups.",
        ["mcu.plort_protector.title"] = "Protecteur de plorts",
        ["mcu.plort_protector.desc"] = "Empêche les slimes de l'enclos de manger les plorts tant que la batterie est chargée. Rechargez-la avec de l'eau.",
        ["mcu.protector_battery.title"] = "Batterie améliorée",
        ["mcu.protector_battery.desc"] = "Triple la capacité de la batterie du protecteur de plorts.",
        ["mcu.mini_garden.title"] = "Jardin interne",
        ["mcu.mini_garden.desc"] = "Ajoute un jardin à l'intérieur de l'enclos.",
        ["mcu.capacity_booster.title"] = "Capacité de stockage accrue",
        ["mcu.capacity_booster.desc"] = "Triple la capacité de tous les stockages de l'enclos.",
        ["mcu.miniturizer.title"] = "Miniaturiseur",
        ["mcu.miniturizer.desc"] = "Réduit la taille des slimes, jouets et aliments présents dans l'enclos.",
        ["mcu.slime_sprinkler.title"] = "Arroseur à slimes",
        ["mcu.slime_sprinkler.desc"] = "Arrose régulièrement les slimes de l'enclos.",
        ["mcu.clear_crops.title"] = "Vider la culture",
        ["mcu.clear_crops.desc"] = "Retire la plante du jardin interne."
    };

    private static TableReference _table;
    private static bool _installed;

    /// <summary>
    /// Registers every mod string in <paramref name="table"/> (the table used by the vanilla
    /// upgrade entries). Safe to call more than once.
    /// </summary>
    public static void Install(TableReference table)
    {
        if (_installed) return;
        _table = table;

        StringTable stringTable = LocalizationSettings.StringDatabase.GetTable(table);
        if (stringTable == null)
        {
            Main.Log.Warning("String table not loaded yet; mod strings will fall back to their key.");
            return;
        }

        Dictionary<string, string> strings = IsFrench(stringTable) ? French : English;
        foreach (KeyValuePair<string, string> entry in strings)
            stringTable.AddEntry(entry.Key, entry.Value);

        _installed = true;
    }

    /// <summary>Builds a <see cref="LocalizedString"/> pointing at one of the mod's keys.</summary>
    public static LocalizedString Get(string key)
    {
        LocalizedString localized = new();
        localized.SetReference(_table, key);
        return localized;
    }

    private static bool IsFrench(StringTable table)
    {
        string code = table.LocaleIdentifier.Code;
        return code != null && code.StartsWith("fr");
    }
}
