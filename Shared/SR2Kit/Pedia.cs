using System.Collections.Generic;
using Il2Cpp;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Il2CppMonomiPark.SlimeRancher.Pedia;
using MelonLoader;
using UnityEngine;
using UnityEngine.Localization;

namespace SR2Kit;

/// <summary>
/// Gives a modded identifiable its own Slimepedia entry.
///
/// An entry is a <see cref="PediaEntry"/> asset holding a title, a description and a list of pages,
/// and a category asset lists the entries it shows. Both are plain scriptable objects, so a modded
/// entry is a clone of the one the game already writes for the vanilla type the mod was cut from:
/// it inherits the layout, the highlight set and the categories it belongs to, and only its subject
/// and its text are replaced.
/// </summary>
public static class Pedia
{
    /// <summary>Table used for the entry text when the template's own table cannot be read.</summary>
    private const string FallbackTable = "Actor";

    /// <summary>Category holding everything a rancher can carry, and the home of modded plorts.</summary>
    private const string ResourceCategory = "Resources";

    private static readonly Dictionary<string, PediaEntry> Registered = new();

    /// <summary>
    /// Writes a Slimepedia entry for <paramref name="type"/>, modelled on the entry of
    /// <paramref name="template"/> and worded with <paramref name="description"/>.
    /// </summary>
    public static PediaEntry Register(IdentifiableType type, IdentifiableType template, string description)
    {
        if (type == null || template == null) return null;
        if (Registered.TryGetValue(type.referenceId, out PediaEntry already)) return already;

        // Plorts have no page of their own in Slime Rancher 2 — one shared entry covers them all —
        // so a modded plort has nothing to be modelled on. It gets a resource page instead, which is
        // where a rancher looks for a thing they can hold.
        IdentifiablePediaEntry source = Find(template) ?? FirstIn(ResourceCategory);
        if (source == null)
        {
            MelonLogger.Warning($"[SR2Kit] Nothing to model the Slimepedia entry of {type.referenceId} on.");
            return null;
        }

        IdentifiablePediaEntry entry = Object.Instantiate(source);
        entry.hideFlags = HideFlags.HideAndDontSave;
        entry.name = $"Pedia{type.referenceId}";
        entry._identifiableType = type;

        // Unlocked from the start: the game unlocks an entry when the player first meets the thing,
        // which for a modded type would mean the Slimepedia stays empty until then.
        entry._isUnlockedInitially = true;

        if (type.localizedName != null) entry._title = type.localizedName;

        string table = TableOf(source);
        string key = $"pedia.{type.referenceId}";
        Translations.Add(table, key, description);
        LocalizedString text = Translations.Localized(table, key);

        entry._description = text;
        entry._details = Retext(source._details, text);

        int categories = AddToCategories(source, entry);
        Registered[type.referenceId] = entry;

        MelonLogger.Msg($"[SR2Kit] Slimepedia entry for {type.referenceId} in {categories} categories.");
        return entry;
    }

    /// <summary>The entry the game writes for an identifiable, or null if it has none.</summary>
    private static IdentifiablePediaEntry Find(IdentifiableType type)
    {
        foreach (IdentifiablePediaEntry entry in Resources.FindObjectsOfTypeAll<IdentifiablePediaEntry>())
        {
            if (entry != null && entry._identifiableType == type) return entry;
        }
        return null;
    }

    /// <summary>First entry of a category that documents an identifiable, to be used as a model.</summary>
    private static IdentifiablePediaEntry FirstIn(string categoryName)
    {
        foreach (PediaCategory category in Resources.FindObjectsOfTypeAll<PediaCategory>())
        {
            if (category.name != categoryName || category._items == null) continue;

            foreach (PediaEntry entry in category._items)
            {
                IdentifiablePediaEntry identifiable = entry?.TryCast<IdentifiablePediaEntry>();
                if (identifiable != null && identifiable._identifiableType != null) return identifiable;
            }
        }
        return null;
    }

    /// <summary>
    /// Rebuilds the entry's pages around one text.
    ///
    /// The page objects are shared with the template — they are plain objects inside the asset, not
    /// copies — so writing into them would rewrite the vanilla entry. Only the first page carries
    /// the description; the rest are dropped, since they describe the slime this one was cut from.
    /// </summary>
    private static Il2CppReferenceArray<PediaEntryDetail> Retext(
        Il2CppReferenceArray<PediaEntryDetail> pages, LocalizedString text)
    {
        if (pages == null || pages.Length == 0) return pages;

        Il2CppReferenceArray<PediaEntryDetail> rewritten = new(1);
        rewritten[0] = new PediaEntryDetail
        {
            Section = pages[0].Section,
            Text = text,
            TextGamepad = text
        };
        return rewritten;
    }

    /// <summary>Lists the new entry wherever the template is listed.</summary>
    private static int AddToCategories(PediaEntry source, PediaEntry entry)
    {
        int added = 0;

        foreach (PediaCategory category in Resources.FindObjectsOfTypeAll<PediaCategory>())
        {
            Il2CppReferenceArray<PediaEntry> items = category._items;
            if (items == null || !Contains(items, source) || Contains(items, entry)) continue;

            Il2CppReferenceArray<PediaEntry> grown = new(items.Length + 1);
            for (int i = 0; i < items.Length; i++) grown[i] = items[i];
            grown[items.Length] = entry;

            category._items = grown;
            added++;
        }
        return added;
    }

    private static bool Contains(Il2CppReferenceArray<PediaEntry> items, PediaEntry entry)
    {
        foreach (PediaEntry existing in items)
        {
            if (existing == entry) return true;
        }
        return false;
    }

    /// <summary>Localization table the template's own text lives in, so ours reads from the same one.</summary>
    private static string TableOf(PediaEntry source)
    {
        try
        {
            string name = source._description?.TableReference.TableCollectionName;
            if (!string.IsNullOrEmpty(name)) return name;
        }
        catch { /* an entry whose reference was never set */ }

        return FallbackTable;
    }
}
