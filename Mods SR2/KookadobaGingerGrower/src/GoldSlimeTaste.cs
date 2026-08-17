using Il2Cpp;
using SR2Kit;

namespace KookadobaGingerGrower;

/// <summary>
/// Gives the gold slime a taste for Gilded Ginger.
///
/// It belongs with the crop: once a rancher can grow ginger by the bed, the slime that eats anything
/// has a reason to want that one. Only the favourite is set — its food groups are left alone, so it
/// still eats everything it used to.
/// </summary>
public static class GoldSlimeTaste
{
    private static bool _done;

    /// <summary>Idempotent, like the rest of the mod's setup: called at both startup moments.</summary>
    public static void Apply()
    {
        if (_done || GingerCrop.Ginger == null) return;

        SlimeDefinitions definitions = GameContext.Instance?.SlimeDefinitions
                                       ?? LookupDirector.GetIfReady()?._slimeDefinitions;
        if (definitions == null) return;

        SlimeDefinition gold = Lookup.FindSlimeDefinition(definitions, "slime", "gold");
        if (gold == null)
        {
            Main.Log.Warning("No gold slime in this game; nothing to give a taste for ginger.");
            _done = true;
            return;
        }

        if (Diets.AddFavorite(gold, GingerCrop.Ginger))
            Main.Log.Msg($"{gold.ReferenceId} now favours {GingerCrop.Ginger.name}, and still eats the rest.");

        _done = true;
    }
}
