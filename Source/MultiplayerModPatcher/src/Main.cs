using System;
using Il2Cpp;
using MelonLoader;
using UnityEngine.InputSystem;
using SR2Kit;

[assembly: MelonInfo(typeof(MultiplayerModPatcher.Main), "Multiplayer Mod Patcher", "1.0.0", "Xiu_ma, PikaCat, Claude")]
[assembly: MelonGame("MonomiPark", "SlimeRancher2")]

namespace MultiplayerModPatcher;

/// <summary>
/// Repairs what Ranching Together (SR2MP) cannot synchronise on its own.
///
/// Two problems, one mod:
///
/// * Modded content is invisible to the network. SR2MP names an actor by the index the save system
///   gives its identifiable type, and a modded type has no such index — see
///   <see cref="PersistenceRegistration"/>.
/// * Actors freeze in mid-air once players walk away from each other, because a remote actor's
///   rigidbody is frozen and nobody claims it back when its owner stops simulating it — see
///   <see cref="OwnershipWatchdog"/>.
///
/// It reads SR2MP by reflection and never references it, so it installs and runs on its own: with no
/// multiplayer mod present the persistence half still applies, which is what modded actors need to
/// survive a save either way.
/// </summary>
public class Main : MelonMod
{
    public static Main Instance { get; private set; }
    public static MelonLogger.Instance Log => Instance.LoggerInstance;

    public override void OnInitializeMelon()
    {
        Instance = this;

        MelonPreferences_Category category =
            MelonPreferences.CreateCategory("MultiplayerModPatcher", "Multiplayer Mod Patcher");
        MelonPreferences_Entry<bool> claim = category.CreateEntry("claimAbandonedActors", true,
            description: "Hands an actor back to a player who can see it once its owner has gone " +
                         "silent. Off logs the same actors without touching them.");
        _snapshotKey = category.CreateEntry("snapshotHotkey", "F10",
            description: "Key that writes the network state of the actors around you to the log. " +
                         "Press it while looking at something stuck in mid-air.");

        SR2MPBridge.Resolve();
        if (SR2MPBridge.Available)
        {
            Log.Msg($"Ranching Together found (v{SR2MPBridge.Version}).");
            OwnershipWatchdog.Claiming = claim.Value;
            OwnershipWatchdog.Install(HarmonyInstance);
            WeatherGuard.Install(HarmonyInstance);
            MarketPriceSync.Install(HarmonyInstance);
            GardenSync.Install(HarmonyInstance);
            WeatherPatternGuard.Install(HarmonyInstance);
            ActorTypeIdTranslation.Install(HarmonyInstance);
        }
        else
        {
            Log.Msg($"Multiplayer support idle: {SR2MPBridge.Unavailable}.");
        }

        // Every mod has registered its content by the time a save is running, and the save reference
        // translation only exists from that point on.
        Hooks.OnSceneContextReady(OnSceneReady);
    }

    private static MelonPreferences_Entry<string> _snapshotKey;

    public override void OnUpdate()
    {
        OwnershipWatchdog.Report();
        ActorTypeIdTranslation.Report();

        if (_snapshotKey.Value.Length > 0 && Pressed(_snapshotKey.Value))
        {
            OwnershipWatchdog.Snapshot();
            WeatherPatternGuard.Snapshot();
            MarketPriceSync.Snapshot();
        }
    }

    /// <summary>
    /// Reads a key from Unity's input system: Slime Rancher 2 ships without the legacy input
    /// manager, so <c>UnityEngine.Input</c> throws instead of answering.
    /// </summary>
    private static bool Pressed(string keyName)
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null) return false;

        if (!Enum.TryParse(keyName, ignoreCase: true, out Key key) || key == Key.None) return false;
        return keyboard[key].wasPressedThisFrame;
    }

    private static void OnSceneReady(SceneContext _)
    {
        PersistenceRegistration.Run();
        ActorTypeRefresh.Run();
        WeatherPatternGuard.Describe();
    }
}
