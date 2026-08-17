using KookadobaGingerGrower;
using MelonLoader;
using SR2Kit;

[assembly: MelonInfo(typeof(KookadobaGingerGrower.Main), "Kookadoba Ginger Grower SR2", "1.0.0", "MegaPiggy")]
[assembly: MelonGame("MonomiPark", "SlimeRancher2")]

namespace KookadobaGingerGrower;

/// <summary>
/// Port of the Slime Rancher 1 mod "KookadobaGingerGrower" by MegaPiggy to Slime Rancher 2
/// (Il2Cpp + MelonLoader, no modding framework — see the shared SR2Kit).
///
/// Gilded Ginger becomes a garden crop: drop one in a garden and it grows like any other veggie,
/// deluxe garden included. Kookadoba does not exist in Slime Rancher 2, so only the ginger half of
/// the original mod has an equivalent here (see the README).
/// </summary>
public class Main : MelonMod
{
    public static Main Instance { get; private set; }
    public static MelonLogger.Instance Log => Instance.LoggerInstance;

    public override void OnInitializeMelon()
    {
        Instance = this;

        // The crop is built from the garden plot prefab, which the lookup director owns. Both steps
        // run at both moments and are idempotent: the lookup pass is the one that matters (it lands
        // before a save is pulled into the plot models), the scene pass only catches the case where
        // the save translation was not built yet.
        Hooks.OnLookupDirectorReady(_ => Setup());
        Hooks.OnSceneContextReady(_ =>
        {
            Setup();
            GingerPatches.Reset();
            GingerPatches.List();
        });

        LoggerInstance.Msg("Kookadoba Ginger Grower loaded.");
    }

    private static void Setup()
    {
        GingerCrop.Build();
        GrowerPersistence.Register();
        GoldSlimeTaste.Apply();
    }
}
