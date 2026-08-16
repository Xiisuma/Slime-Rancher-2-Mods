using System.Collections.Generic;
using MelonLoader;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;

namespace SR2Kit;

/// <summary>
/// Adds strings to the game's localization tables at runtime, the replacement for MelonSRML's
/// <c>TranslationPatcher</c>.
/// </summary>
public static class Translations
{
    private readonly record struct PendingEntry(string Table, string Key, string Value);

    private static readonly List<PendingEntry> Pending = new();

    /// <summary>
    /// Registers <paramref name="value"/> under <paramref name="key"/> in <paramref name="table"/>
    /// (for example "Actor" or "UI"). If the table is not loaded yet the entry is applied later, on
    /// the next call once localization is up. Returns the key, so it can be used inline.
    /// </summary>
    public static string Add(string table, string key, string value)
    {
        Pending.Add(new PendingEntry(table, key, value));
        Flush();
        return key;
    }

    /// <summary>Builds a <see cref="LocalizedString"/> pointing at a table entry.</summary>
    public static LocalizedString Localized(string table, string key)
    {
        LocalizedString localized = new();
        localized.SetReference(table, key);
        return localized;
    }

    /// <summary>Applies every entry whose table is loaded. Safe to call repeatedly.</summary>
    public static void Flush()
    {
        if (Pending.Count == 0) return;
        if (LocalizationSettings.Instance == null) return;

        List<PendingEntry> remaining = new();
        foreach (PendingEntry entry in Pending)
        {
            StringTable stringTable = null;
            try { stringTable = LocalizationSettings.StringDatabase.GetTable(entry.Table); }
            catch { /* localization not initialized yet */ }

            if (stringTable == null)
            {
                remaining.Add(entry);
                continue;
            }
            stringTable.AddEntry(entry.Key, entry.Value);
        }

        if (remaining.Count == Pending.Count && remaining.Count > 0)
            MelonLogger.Warning($"[SR2Kit] {remaining.Count} translations still pending; tables not loaded yet.");

        Pending.Clear();
        Pending.AddRange(remaining);
    }
}
