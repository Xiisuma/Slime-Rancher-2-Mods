using MelonLoader;
using MoreCorralUpgrades;

[assembly: MelonInfo(typeof(MoreCorralUpgrades.Main), "MoreCorralUpgrades", "1.0.0", "Aidanamite", null)]
[assembly: MelonGame("MonomiPark", "SlimeRancher2")]

namespace MoreCorralUpgrades;

/// <summary>
/// Entry point. Port of the Slime Rancher 1 mod "MoreCorralUpgrades" to Slime Rancher 2
/// (Il2Cpp + MelonLoader instead of Mono + SRML).
/// </summary>
public class Main : MelonMod
{
    public static Main Instance { get; private set; }

    /// <summary>Logger shared by every part of the mod.</summary>
    public static MelonLogger.Instance Log => Instance.LoggerInstance;

    public override void OnInitializeMelon()
    {
        Instance = this;
        Config.Initialize();
        LoggerInstance.Msg("MoreCorralUpgrades loaded.");
    }

    public override void OnSceneWasInitialized(int buildIndex, string sceneName)
    {
        // Asset references do not survive a scene reload (loading another save, returning to menu).
        GameResources.Reset();
    }
}
