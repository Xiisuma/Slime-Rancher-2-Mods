using Il2Cpp;
using SR2Kit;
using UnityEngine;

namespace KookadobaGingerGrower;

/// <summary>
/// Lets a rancher carry the ginger they just grew.
///
/// Gilded Ginger belongs to no identifiable group at all — the carrot belongs to sixteen. Its prefab
/// has the <c>Vacuumable</c> component like any crop, but the vacpack, the silos, the drones and the
/// rest decide by group membership, so the ginger could be grown and never picked up. It is given
/// the groups of an ordinary crop here, which is what makes it behave like one.
/// </summary>
public static class GingerGroups
{
    /// <summary>The crop whose groups the ginger borrows.</summary>
    private const string Model = "carrot";

    private static bool _done;

    public static void Apply()
    {
        if (_done || GingerCrop.Ginger == null) return;

        LookupDirector director = LookupDirector.GetIfReady();
        if (director == null) return;

        IdentifiableType model = Lookup.FindIdentifiable(Model);
        if (model == null)
        {
            Main.Log.Warning($"No {Model} to copy groups from; the ginger stays uncarryable.");
            _done = true;
            return;
        }
        _done = true;

        int added = 0;
        foreach (IdentifiableTypeGroup group in Resources.FindObjectsOfTypeAll<IdentifiableTypeGroup>())
        {
            if (group == null || !group.IsMember(model) || group.IsMember(GingerCrop.Ginger)) continue;

            director.AddIdentifiableTypeToGroup(GingerCrop.Ginger, group);
            added++;
        }

        // Groups hold other groups, so joining a handful of them is what earns the rest. The list is
        // logged because it is the answer to "why can I not pick this up".
        string memberships = string.Empty;
        foreach (IdentifiableTypeGroup group in Resources.FindObjectsOfTypeAll<IdentifiableTypeGroup>())
        {
            if (group != null && group.IsMember(GingerCrop.Ginger)) memberships += group.name + " ";
        }

        Main.Log.Msg($"Gilded Ginger joined {added} groups, and is now a member of: {memberships}");
    }
}
