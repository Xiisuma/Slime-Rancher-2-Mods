using Il2Cpp;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using MelonLoader;
using UnityEngine;

namespace SR2Kit;

/// <summary>
/// Creating and registering new identifiable types, the replacement for MelonSRML's
/// <c>[IdentifiableTypeHolder]</c>.
///
/// New types are clones of a vanilla one: that inherits its categories, its group rules and every
/// serialized field the game expects, so only the identity (name, reference id, prefab, colours)
/// has to be replaced. Registration goes through the game's own
/// <see cref="LookupDirector.AddIdentifiableTypeToGroup"/> and reference-id lookup, which is also
/// what the save system resolves ids against — so actors of a modded type survive a save/load.
/// </summary>
public static class IdentifiableRegistry
{
    /// <summary>
    /// Clones <paramref name="template"/> into a new identifiable type and registers it.
    /// </summary>
    /// <param name="referenceId">Stable id stored in save files. Prefix it with your mod.</param>
    /// <param name="translationTable">Localization table holding the display name, usually "Actor".</param>
    /// <param name="translationKey">Key of the display name inside that table.</param>
    public static IdentifiableType Create(LookupDirector director, IdentifiableType template,
        string referenceId, string translationTable = null, string translationKey = null)
    {
        if (director == null || template == null) return null;

        if (director.TryFindIdentifiableTypeByReferenceId(referenceId, out IdentifiableType existing))
            return existing;

        IdentifiableType type = Object.Instantiate(template);
        type.hideFlags = HideFlags.HideAndDontSave;
        type.name = referenceId;
        type.referenceId = referenceId;

        // Force the persistence hash to be recomputed from the new reference id.
        type.initializedHashId = false;
        type.stableHashedId = 0;

        if (translationTable != null && translationKey != null)
            type.localizedName = Translations.Localized(translationTable, translationKey);

        Register(director, type, template);
        return type;
    }

    /// <summary>
    /// Makes an already-built type visible to the game: reachable by reference id (which is how the
    /// save system resolves it) and member of every group the template belongs to.
    /// </summary>
    public static void Register(LookupDirector director, IdentifiableType type, IdentifiableType template)
    {
        director._identifiableTypeByRefId[type.referenceId] = type;

        int groups = 0;
        foreach (IdentifiableTypeGroup group in Resources.FindObjectsOfTypeAll<IdentifiableTypeGroup>())
        {
            if (!group.IsMember(template)) continue;
            director.AddIdentifiableTypeToGroup(type, group);
            groups++;
        }

        MelonLogger.Msg($"[SR2Kit] Registered {type.referenceId} ({groups} groups).");
    }

    /// <summary>Assigns a prefab to a type and points every actor inside it at that type.</summary>
    public static void SetPrefab(IdentifiableType type, GameObject prefab)
    {
        foreach (IdentifiableActor actor in prefab.GetComponentsInChildren<IdentifiableActor>(true))
            actor.identType = type;

        type.prefab = prefab;
    }

    /// <summary>
    /// Adds a slime definition to the game's registry and rebuilds its indexes.
    /// A <see cref="SlimeDefinition"/> is itself an <see cref="IdentifiableType"/>, so it must also
    /// be registered with <see cref="Register"/> to be reachable by reference id.
    /// </summary>
    public static void AddSlimeDefinition(SlimeDefinitions definitions, SlimeDefinition definition)
    {
        if (definitions == null || definition == null) return;

        if (definitions.GetSlimeByIdentifiableId(definition) != null) return;

        Il2CppReferenceArray<SlimeDefinition> slimes = definitions.Slimes;
        Il2CppReferenceArray<SlimeDefinition> grown = new(slimes.Length + 1);
        for (int i = 0; i < slimes.Length; i++) grown[i] = slimes[i];
        grown[slimes.Length] = definition;

        definitions.Slimes = grown;
        definitions.RefreshIndexes();
        definitions.RefreshDefinitions();

        MelonLogger.Msg($"[SR2Kit] Registered slime definition {definition.name}.");
    }
}
