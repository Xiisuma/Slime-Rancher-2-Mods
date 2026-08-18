using System;
using System.Collections.Generic;
using Il2Cpp;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Il2CppMonomiPark.SlimeRancher;
using Il2CppMonomiPark.SlimeRancher.Persist;

namespace MultiplayerModPatcher;

/// <summary>
/// Gives every modded identifiable type a persistence id.
///
/// The game addresses an identifiable type in a save file by its index in a table built once, from
/// the list of types the game itself ships (<see cref="SaveReferenceTranslation"/>). A mod that
/// registers a new type with the lookup director is reachable by reference id but absent from that
/// table, and Ranching Together names actors over the network with exactly those indexes — so a
/// modded slime cannot be described in a packet at all.
///
/// The fix is the one the game would have applied itself: append the missing types to the table.
/// They are appended in reference-id order, never inserted, so vanilla ids keep the values a save
/// already holds and two players running the same mods compute the same ids on both machines —
/// which matters because Ranching Together translates ids from the host to the client but sends its
/// own local ids back the other way.
/// </summary>
internal static class PersistenceRegistration
{
    /// <summary>Reference ids given an id here, in the order they were appended.</summary>
    private static readonly List<string> Registered = new();

    /// <summary>Reference ids the game already knew, so the work is only done once per save.</summary>
    private static bool _done;

    public static IReadOnlyList<string> ModdedTypes => Registered;

    public static void Run()
    {
        if (_done) return;

        SaveReferenceTranslation translation =
            GameContext.Instance?.AutoSaveDirector?._saveReferenceTranslation;
        if (translation == null)
        {
            Main.Log.Warning("No save reference translation yet; modded types keep their missing ids.");
            return;
        }

        LookupDirector director = LookupDirector.GetIfReady();
        if (director?._identifiableTypeByRefId == null)
        {
            Main.Log.Warning("The lookup director is not ready; modded types keep their missing ids.");
            return;
        }

        PersistenceIdLookupTable<IdentifiableType> table = translation._identifiableTypeToPersistenceId;
        if (table?._primaryIndex == null || table._reverseIndex == null)
        {
            Main.Log.Warning("The identifiable persistence table is not laid out as expected.");
            return;
        }

        List<string> missing = FindMissing(director, table);
        if (missing.Count == 0)
        {
            _done = true;
            Main.Log.Msg("No modded identifiable type is missing from the persistence table.");
            return;
        }

        // Ordinal sort, so the ids depend on which mods are installed and on nothing else — not on
        // the order MelonLoader happened to load them in, which differs from machine to machine.
        missing.Sort(StringComparer.Ordinal);

        foreach (string referenceId in missing)
        {
            if (!director.TryFindIdentifiableTypeByReferenceId(referenceId, out IdentifiableType type))
                continue;

            Append(translation, table, type, referenceId);
            Registered.Add(referenceId);
        }

        _done = true;
        Report(translation);
    }

    /// <summary>Reference ids the lookup director knows but the persistence table does not.</summary>
    private static List<string> FindMissing(LookupDirector director,
        PersistenceIdLookupTable<IdentifiableType> table)
    {
        List<string> missing = new();

        Il2CppSystem.Collections.Generic.Dictionary<string, IdentifiableType>.Enumerator types =
            director._identifiableTypeByRefId.GetEnumerator();

        while (types.MoveNext())
        {
            string referenceId = types.Current.key;
            if (string.IsNullOrEmpty(referenceId)) continue;
            if (table._reverseIndex.ContainsKey(referenceId)) continue;

            missing.Add(referenceId);
        }

        return missing;
    }

    /// <summary>
    /// Adds one type to both halves of the translation: the reference-id lookup a save is read back
    /// with, and the index table it is written with.
    /// </summary>
    private static void Append(SaveReferenceTranslation translation,
        PersistenceIdLookupTable<IdentifiableType> table, IdentifiableType type, string referenceId)
    {
        if (translation._identifiableTypeLookup != null)
            translation._identifiableTypeLookup[referenceId] = type;

        Il2CppStringArray index = table._primaryIndex;
        Il2CppStringArray grown = new(index.Length + 1);
        for (int i = 0; i < index.Length; i++) grown[i] = index[i];
        grown[index.Length] = referenceId;

        table._primaryIndex = grown;
        table._reverseIndex[referenceId] = index.Length;
    }

    /// <summary>
    /// Logs what was registered, and a fingerprint of the modded content.
    ///
    /// Two players see the same ids only if they run the same set of modded types; the fingerprint
    /// is printed on both machines so a mismatch is one line to compare rather than a session spent
    /// wondering why a slime arrives as the wrong species.
    /// </summary>
    private static void Report(SaveReferenceTranslation translation)
    {
        foreach (string referenceId in Registered)
        {
            if (!LookupDirector.GetIfReady()
                    .TryFindIdentifiableTypeByReferenceId(referenceId, out IdentifiableType type))
                continue;

            Main.Log.Msg($"  {referenceId} = {translation.GetPersistenceId(type)}");
        }

        Main.Log.Msg($"Registered {Registered.Count} modded identifiable types " +
                     $"(content fingerprint {Fingerprint()}).");
    }

    /// <summary>Stable hash of the modded reference ids, in the order they were given ids.</summary>
    public static string Fingerprint()
    {
        unchecked
        {
            uint hash = 2166136261;
            foreach (string referenceId in Registered)
            {
                foreach (char c in referenceId)
                {
                    hash ^= c;
                    hash *= 16777619;
                }
                hash ^= '\n';
                hash *= 16777619;
            }
            return hash.ToString("X8");
        }
    }
}
