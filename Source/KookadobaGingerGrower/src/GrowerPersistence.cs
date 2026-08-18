using Il2Cpp;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Il2CppMonomiPark.SlimeRancher;
using Il2CppMonomiPark.SlimeRancher.Persist;
using UnityEngine;

namespace KookadobaGingerGrower;

/// <summary>
/// Makes the modded grower definitions saveable.
///
/// A plot stores what is planted in it as an index into a table of grower reference ids
/// (<c>LandPlotV02.ResourceGrowerId</c>). The table is built once from the game's grower list, so a
/// definition the game has never heard of would make the save throw when a ginger patch is planted.
/// Both directions of that translation are patched here: the reverse index used when writing, and
/// the reference-id lookup used when reading a save back.
/// </summary>
public static class GrowerPersistence
{
    private static bool _registered;

    public static void Register()
    {
        if (_registered) return;
        if (GingerCrop.Grower == null) return;

        AddToGrowerList(GingerCrop.Grower);
        AddToGrowerList(GingerCrop.DeluxeGrower);

        SaveReferenceTranslation translation = GameContext.Instance?.AutoSaveDirector?._saveReferenceTranslation;
        PersistenceIDTranslation<ResourceGrowerDefinition> growers = translation?._resourceGrowerTranslation;
        if (growers == null)
        {
            Main.Log.Warning("Save reference translation not available; ginger patches would not survive a save.");
            return;
        }

        AddToTranslation(growers, GingerCrop.Grower);
        AddToTranslation(growers, GingerCrop.DeluxeGrower);
        _registered = true;

        Main.Log.Msg($"Ginger growers registered for saving (persistence id " +
                     $"{translation.GetPersistenceId(GingerCrop.Grower)}).");
    }

    /// <summary>
    /// Adds the definition to the game's canonical grower list, so anything rebuilding the
    /// translation from that list picks the modded growers up on its own.
    /// </summary>
    private static void AddToGrowerList(ResourceGrowerDefinition grower)
    {
        if (grower == null) return;

        foreach (ResourceGrowerList list in Resources.FindObjectsOfTypeAll<ResourceGrowerList>())
        {
            if (list.items == null || list.items.Contains(grower)) continue;
            list.items.Add(grower);
        }
    }

    private static void AddToTranslation(PersistenceIDTranslation<ResourceGrowerDefinition> growers,
        ResourceGrowerDefinition grower)
    {
        if (grower == null) return;

        string referenceId = grower.ReferenceId;
        growers.RawLookupDictionary[referenceId] = grower;

        PersistenceIdLookupTable<ResourceGrowerDefinition> table = growers.InstanceLookupTable;
        if (table == null || table._reverseIndex == null || table._reverseIndex.ContainsKey(referenceId)) return;

        Il2CppStringArray index = table._primaryIndex;
        Il2CppStringArray grown = new(index.Length + 1);
        for (int i = 0; i < index.Length; i++) grown[i] = index[i];
        grown[index.Length] = referenceId;

        table._primaryIndex = grown;
        table._reverseIndex[referenceId] = index.Length;
    }
}
