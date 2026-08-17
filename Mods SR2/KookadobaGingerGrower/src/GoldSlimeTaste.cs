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
            Main.Log.Msg($"{gold.ReferenceId} now favours {GingerCrop.Ginger.name}.");

        // The vanilla gold slime has no food at all — no food groups, no eat map — so it never ate
        // anything and the plort group its diet carries never came up. Giving it a first meal wakes
        // its appetite, plorts included, which is not what a gold slime does. It cannot become a
        // largo either, so the group goes.
        if (!gold.CanLargofy && gold.Diet.EdiblePlortIdentifiableTypeGroup != null)
        {
            gold.Diet.EdiblePlortIdentifiableTypeGroup = null;
            Main.Log.Msg($"{gold.ReferenceId} no longer eats plorts.");
        }

        _done = true;
    }
}
