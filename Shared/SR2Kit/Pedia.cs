using System;
using System.Collections.Generic;
using System.IO;
using Il2Cpp;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using HarmonyLib;
using Il2CppMonomiPark.SlimeRancher.DataModel;
using Il2CppMonomiPark.SlimeRancher.Pedia;
using MelonLoader;
using MelonLoader.Utils;
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

    /// <summary>One modded entry, and the categories the game's own asset lists it in.</summary>
    private sealed class Written
    {
        public IdentifiableType Type;
        public PediaEntry Entry;
        public readonly List<PediaCategory> Categories = new();
    }

    private static readonly Dictionary<string, PediaEntry> Registered = new();
    private static readonly List<Written> Entries = new();

    private static bool _wired;

    /// <summary>
    /// Writes a Slimepedia entry for <paramref name="type"/>, modelled on the entry of
    /// <paramref name="template"/> and worded with <paramref name="description"/>.
    /// </summary>
    public static PediaEntry Register(IdentifiableType type, IdentifiableType template, string description)
    {
        if (type == null || template == null) return null;
        if (Registered.TryGetValue(type.referenceId, out PediaEntry already)) return already;

        // No entry, no clone. Slime Rancher 2 gives no page to a plort — one shared entry covers
        // them all — and a modded plort should not be the exception that gets one.
        IdentifiablePediaEntry source = Find(template);
        if (source == null)
        {
            MelonLogger.Warning($"[SR2Kit] {template.name} has no Slimepedia entry to model {type.referenceId} on.");
            return null;
        }

        IdentifiablePediaEntry entry = UnityEngine.Object.Instantiate(source);
        entry.hideFlags = HideFlags.HideAndDontSave;
        entry.name = $"Pedia{type.referenceId}";
        entry._identifiableType = type;

        // Locked, like every other entry: a rancher discovers a modded slime by meeting it, not by
        // installing the mod. Until then it sits in its category as an unknown.
        entry._isUnlockedInitially = false;

        if (type.localizedName != null) entry._title = type.localizedName;

        string table = TableOf(source);
        string key = $"pedia.{type.referenceId}";
        Translations.Add(table, key, description);
        LocalizedString text = Translations.Localized(table, key);

        entry._description = text;
        entry._details = Retext(source._details, text);

        Written written = new() { Type = type, Entry = entry };
        AddToCategories(source, written);

        Registered[type.referenceId] = entry;
        Entries.Add(written);
        Wire();

        MelonLogger.Msg($"[SR2Kit] Slimepedia entry for {type.referenceId} in {written.Categories.Count} categories.");
        return entry;
    }

    /// <summary>Arranges for the running save's pedia to be told about the entries, once.</summary>
    private static void Wire()
    {
        if (_wired) return;
        _wired = true;

        Hooks.OnSceneContextReady(context => Attach(context?.PediaDirector));
    }

    /// <summary>
    /// Hands the entries to the pedia of a running save.
    ///
    /// Writing them into the category assets is not enough: the director builds its own runtime
    /// categories from those assets, and keeps a map from identifiable to entry for the unlock that
    /// fires when a rancher first meets something. Neither knows about an entry that appeared after
    /// the game was built, so both are filled in here — which is also why the entries turn up in a
    /// save started before the mod was installed.
    /// </summary>
    private static void Attach(PediaDirector director)
    {
        if (director == null || Entries.Count == 0) return;

        int shown = 0;
        foreach (Written written in Entries)
        {
            if (written.Entry == null) continue;

            // Each category asset knows the runtime category built from it, which is the list the
            // Slimepedia actually draws.
            foreach (PediaCategory category in written.Categories)
            {
                PediaRuntimeCategory runtime = category?.GetRuntimeCategory();
                if (runtime == null || runtime.Contains(written.Entry)) continue;

                runtime.AddDynamicItem(written.Entry);
                shown++;
            }

            Link(director, written);
        }

        MelonLogger.Msg($"[SR2Kit] {Entries.Count} Slimepedia entries handed to the running save ({shown} listed).");
    }

    /// <summary>
    /// Puts an entry back to undiscovered, once and only once.
    ///
    /// An earlier version unlocked the modded entries outright, so saves carry slimes their rancher
    /// has never met. This runs on the pedia model, right after a save has filled it — doing it when
    /// the scene loads was undone by the save being read afterwards. Locking them again on every
    /// load would make discovery impossible, so each reference id is written to a file next to the
    /// mod's settings the first time it is locked, and skipped forever after: what the rancher finds
    /// later is theirs to keep.
    /// </summary>
    internal static void Relock(PediaModel model)
    {
        if (model?.unlocked == null || Entries.Count == 0) return;

        HashSet<string> already = Record();

        List<string> locked = new();
        foreach (Written written in Entries)
        {
            if (written.Entry == null || already.Contains(written.Type.referenceId)) continue;

            model.unlocked.Remove(written.Entry);
            locked.Add(written.Type.referenceId);
        }
        if (locked.Count == 0) return;

        Remember(locked);
        MelonLogger.Msg($"[SR2Kit] {locked.Count} Slimepedia entries put back to undiscovered.");
    }

    /// <summary>Reference ids already locked once, from the file shared by every mod using the kit.</summary>
    private static HashSet<string> Record()
    {
        HashSet<string> ids = new();
        try
        {
            if (File.Exists(RecordPath))
            {
                foreach (string line in File.ReadAllLines(RecordPath))
                {
                    if (line.Length > 0) ids.Add(line.Trim());
                }
            }
        }
        catch (Exception e) { MelonLogger.Warning($"[SR2Kit] Could not read {RecordPath}: {e.Message}"); }

        return ids;
    }

    private static void Remember(List<string> ids)
    {
        try { File.AppendAllLines(RecordPath, ids); }
        catch (Exception e) { MelonLogger.Warning($"[SR2Kit] Could not write {RecordPath}: {e.Message}"); }
    }

    private static string RecordPath => Path.Combine(MelonEnvironment.UserDataDirectory, "SR2Kit-pedia.txt");

    /// <summary>Points the identifiable at its entry, the map the game unlocks through.</summary>
    private static void Link(PediaDirector director, Written written)
    {
        if (director._customIdentToEntryMap == null) return;

        foreach (PediaDirector.IdentToEntryItem item in director._customIdentToEntryMap)
        {
            if (item != null && item.Ident == written.Type) return;
        }

        director._customIdentToEntryMap.Add(new PediaDirector.IdentToEntryItem
        {
            Ident = written.Type,
            Entry = written.Entry
        });
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
    private static void AddToCategories(PediaEntry source, Written written)
    {
        foreach (PediaCategory category in Resources.FindObjectsOfTypeAll<PediaCategory>())
        {
            Il2CppReferenceArray<PediaEntry> items = category._items;
            if (items == null || !Contains(items, source) || Contains(items, written.Entry)) continue;

            Il2CppReferenceArray<PediaEntry> grown = new(items.Length + 1);
            for (int i = 0; i < items.Length; i++) grown[i] = items[i];
            grown[items.Length] = written.Entry;

            category._items = grown;
            written.Categories.Add(category);
        }
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

/// <summary>Catches the moment a save has filled the pedia, the only point where a relock sticks.</summary>
[HarmonyPatch(typeof(PediaModel), nameof(PediaModel.Push))]
internal static class Patch_PediaModel_Push
{
    private static void Postfix(PediaModel __instance) => Pedia.Relock(__instance);
}
