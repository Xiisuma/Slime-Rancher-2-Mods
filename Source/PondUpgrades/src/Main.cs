using MelonLoader;
using PondUpgrades;

[assembly: MelonInfo(typeof(PondUpgrades.Main), "Pond Upgrades SR2", "1.0.0", "Aidanamite")]
[assembly: MelonGame("MonomiPark", "SlimeRancher2")]

namespace PondUpgrades;

/// <summary>
/// Entry point. Port of the Slime Rancher 1 mod "SlimePondUpgrades" by Aidanamite to
/// Slime Rancher 2 (Il2Cpp + MelonLoader, no modding framework).
/// </summary>
public class Main : MelonMod
{
    public static Main Instance { get; private set; }

    /// <summary>Logger shared by every part of the mod.</summary>
    public static MelonLogger.Instance Log => Instance.LoggerInstance;

    public override void OnInitializeMelon()
    {
        Instance = this;
        LoggerInstance.Msg("Pond Upgrades loaded.");
    }

    public override void OnSceneWasInitialized(int buildIndex, string sceneName)
    {
        // Asset references do not survive a scene reload (loading another save, returning to menu).
        GameResources.Reset();
    }
}
