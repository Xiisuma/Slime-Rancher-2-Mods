using System.Collections.Generic;
using Il2Cpp;
using Il2CppMonomiPark.SlimeRancher;

namespace MultiplayerModPatcher;

/// <summary>
/// Puts the modded types into Ranching Together's own table.
///
/// SR2MP fills its <c>ActorTypes</c> dictionary once, when a save loads, from the game's save
/// reference translation. Whether it does that before or after this patcher hands out the modded
/// ids depends on the order MelonLoader loaded the two mods in, so the table is rebuilt here rather
/// than assumed: rebuilding it is idempotent, and doing it after registration is what makes a modded
/// actor nameable in a packet.
/// </summary>
internal static class ActorTypeRefresh
{
    public static void Run()
    {
        if (!SR2MPBridge.Available) return;

        IDictionary<int, IdentifiableType> actorTypes = SR2MPBridge.ActorTypes();
        if (actorTypes == null)
        {
            Main.Log.Warning("Ranching Together has no actor table yet; it will build one when a save loads.");
            return;
        }

        SaveReferenceTranslation translation =
            GameContext.Instance?.AutoSaveDirector?._saveReferenceTranslation;
        if (translation?._identifiableTypeLookup == null) return;

        int added = 0;

        Il2CppSystem.Collections.Generic.Dictionary<string, IdentifiableType>.Enumerator types =
            translation._identifiableTypeLookup.GetEnumerator();

        while (types.MoveNext())
        {
            IdentifiableType type = types.Current.value;
            if (type == null) continue;

            int id = translation.GetPersistenceId(type);
            if (actorTypes.ContainsKey(id)) continue;

            actorTypes[id] = type;
            added++;
        }

        // SR2MP uses -1 for "no type", and rebuilding must not drop it.
        actorTypes[-1] = null;

        Main.Log.Msg($"Ranching Together actor table refreshed: {added} types added, {actorTypes.Count} known.");
    }
}
