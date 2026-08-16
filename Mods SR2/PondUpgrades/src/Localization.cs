using System.Collections.Generic;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;

namespace PondUpgrades;

/// <summary>
/// Adds the mod's strings to the string table the vanilla plot upgrades already use, so the
/// purchase UI can display them like any other entry.
/// </summary>
public static class Localization
{
    private static readonly Dictionary<string, string> English = new()
    {
        ["spu.slime_capacity.title"] = "Slime Capacity",
        ["spu.slime_capacity.desc"] = "Doubles the number of slimes the pond can contain.",
        ["spu.plort_capacity.title"] = "Plort Capacity",
        ["spu.plort_capacity.desc"] = "Doubles the number of plorts the pond can contain.",
        ["spu.ancient_blessing.title"] = "Ancient Blessing",
        ["spu.ancient_blessing.desc"] = "Blesses the water with an ancient power, tripling the slime and plort capacity again."
    };

    private static readonly Dictionary<string, string> French = new()
    {
        ["spu.slime_capacity.title"] = "Capacité en slimes",
        ["spu.slime_capacity.desc"] = "Double le nombre de slimes que l'étang peut contenir.",
        ["spu.plort_capacity.title"] = "Capacité en plorts",
        ["spu.plort_capacity.desc"] = "Double le nombre de plorts que l'étang peut contenir.",
        ["spu.ancient_blessing.title"] = "Bénédiction ancienne",
        ["spu.ancient_blessing.desc"] = "Bénit l'eau d'un pouvoir ancien, triplant encore la capacité en slimes et en plorts."
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
