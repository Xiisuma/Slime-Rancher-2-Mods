using System;
using System.Linq;
using System.Reflection;
using Il2Cpp;
using Il2CppMonomiPark.SlimeRancher;
using Il2CppMonomiPark.SlimeRancher.Persist;
using Il2CppMonomiPark.SlimeRancher.UI.ButtonBehavior;
using Il2CppMonomiPark.SlimeRancher.UI.MainMenu;
using Il2CppMonomiPark.SlimeRancher.UI.MainMenu.Model;
using MelonLoader;
using UnityEngine;

namespace DevGive;

/// <summary>
/// Drives the game from the settings file instead of the menus: continue the last save, then host or
/// join a multiplayer session.
///
/// This exists to make multiplayer testable without a second pair of hands. Two copies of the game
/// can be started with different settings files, one hosting and one joining, and both end up in the
/// same world without a single click. Everything is off by default, so a player who only wants the
/// give hotkey never notices it.
/// </summary>
internal sealed class AutoSession
{
    private readonly MelonPreferences_Entry<bool> _continue;
    private readonly MelonPreferences_Entry<int> _host;
    private readonly MelonPreferences_Entry<string> _connect;
    private readonly MelonPreferences_Entry<bool> _noSave;
    private readonly MelonPreferences_Entry<string> _teleport;
    private readonly MelonPreferences_Entry<string> _teleportKey;
    private readonly MelonPreferences_Entry<string> _quitKey;

    /// <summary>Seconds between attempts while the main menu is still building itself.</summary>
    private const float RetryInterval = 1f;

    /// <summary>Seconds after that to give up, so a broken setting is not retried forever.</summary>
    private const float GiveUp = 120f;

    /// <summary>Seconds to let a save settle before opening or joining a session on it.</summary>
    private const float SessionDelay = 5f;

    private float _timer;
    private float _nextTry;
    private bool _menuDone;
    private float _loadedAt = -1f;
    private bool _sessionStarted;
    private bool _savingStopped;

    public AutoSession(MelonPreferences_Category category)
    {
        _continue = category.CreateEntry("autoContinue", false,
            description: "Loads the save the Continue button would, a few seconds after the game starts.");
        _host = category.CreateEntry("autoHost", 0,
            description: "Once in a save, hosts a Ranching Together session on this port. 0 = off.");
        _connect = category.CreateEntry("autoConnect", "",
            description: "Joins a Ranching Together session at ip:port instead of hosting. Empty = off.");
        _teleport = category.CreateEntry("teleportOffset", "",
            description: "Offset x,y,z the teleport key moves the player by. Empty = no teleport key. " +
                         "Meant for putting two players far apart on purpose.");
        _teleportKey = category.CreateEntry("teleportHotkey", "F8",
            description: "Key that applies teleportOffset.");
        _quitKey = category.CreateEntry("quitHotkey", "",
            description: "Key that closes the game the way the menu would. Empty = off. Killing the " +
                         "process instead can leave a half-written autosave behind.");
        _noSave = category.CreateEntry("disableAutosave", false,
            description: "Stops this copy of the game writing autosaves. Meant for the joining side " +
                         "of a local multiplayer test, which would otherwise write over the host's save.");
    }

    public bool Enabled => _continue.Value || _host.Value != 0 || _connect.Value.Length > 0
                           || _noSave.Value || _teleport.Value.Length > 0 || _quitKey.Value.Length > 0;

    public void Update()
    {
        if (!Enabled) return;

        // Slime Rancher 2 stops updating when its window loses focus, which freezes a load halfway
        // and makes a second instance useless for testing. Anything driven from the settings file
        // needs the game to keep running while it is behind another window.
        if (!Application.runInBackground)
        {
            Application.runInBackground = true;
            Main.Log.Msg("The game will keep running while its window is in the background.");
        }

        _timer += Time.unscaledDeltaTime;

        if (_teleport.Value.Length > 0 && Main.Pressed(_teleportKey.Value)) Teleport();

        if (_quitKey.Value.Length > 0 && Main.Pressed(_quitKey.Value))
        {
            Main.Log.Msg("Quitting on request.");
            Application.Quit();
        }

        // The menus are built over several seconds and a save cannot be continued before the profile
        // is read, so the request is retried rather than fired once at a guessed moment.
        if (!_menuDone && _timer < GiveUp && _timer >= _nextTry)
        {
            _nextTry = _timer + RetryInterval;
            _menuDone = TryMenuActions();
            if (!_menuDone && _timer + RetryInterval >= GiveUp)
                Main.Log.Warning("autoContinue/autoConnect gave up: the menu never became ready.");
        }

        if (_noSave.Value && !_savingStopped)
        {
            AutoSaveDirector director = GameContext.Instance?.AutoSaveDirector;
            if (director != null)
            {
                director.SetAutosaveDisabled(true);
                _savingStopped = true;
                Main.Log.Msg("disableAutosave: this copy will not write autosaves.");
            }
        }

        // Hosting and joining both need a save to be running: Ranching Together refuses either from
        // the menu, and the world has to have settled before it starts describing it over the wire.
        if (_sessionStarted || (_host.Value == 0 && _connect.Value.Length == 0)) return;

        if (_loadedAt < 0f)
        {
            if (SRSingleton<SceneContext>.Instance?.player == null) return;
            _loadedAt = _timer;
            return;
        }

        if (_timer - _loadedAt < SessionDelay) return;

        _sessionStarted = true;
        if (_host.Value != 0) Host(_host.Value);
        else Connect(_connect.Value);
    }

    /// <summary>
    /// Moves the player by the configured offset.
    ///
    /// The game's own teleport is used rather than a write to the transform, so the regions around
    /// the player stream in and out exactly as they do when walking there — which is the whole point
    /// when what is being tested is what happens to the world a player leaves behind.
    /// </summary>
    private void Teleport()
    {
        string[] parts = _teleport.Value.Split(',');
        if (parts.Length != 3
            || !float.TryParse(parts[0], out float x)
            || !float.TryParse(parts[1], out float y)
            || !float.TryParse(parts[2], out float z))
        {
            Main.Log.Warning($"teleportOffset: '{_teleport.Value}' is not x,y,z.");
            return;
        }

        TeleportablePlayer player = SRSingleton<SceneContext>.Instance?.player
            ?.GetComponent<TeleportablePlayer>();
        if (player == null)
        {
            Main.Log.Warning("teleportOffset: no player to move.");
            return;
        }

        Vector3 destination = player.transform.position + new Vector3(x, y, z);

        // Both optional arguments have to be built by hand: the interop layer dereferences them
        // rather than treating a missing one as "no value", so leaving them out throws.
        Il2CppSystem.Nullable<Vector3> rotation = new(player.transform.eulerAngles);

        if (player.SceneGroup == null)
        {
            Main.Log.Warning("teleportOffset: the player is not in a scene group yet.");
            return;
        }

        player.TeleportTo(destination, player.SceneGroup, rotation, overlayEnabled: false);
        Main.Log.Msg($"Teleported to {destination}.");
    }

    /// <summary>Runs the menu-level requests once the game can serve them. False = try again later.</summary>
    private bool TryMenuActions()
    {
        if (!_continue.Value) return true;
        if (SRSingleton<SceneContext>.Instance?.player != null) return true;

        AutoSaveDirector director = GameContext.Instance?.AutoSaveDirector;
        if (director == null || !director.HasContinue()) return false;

        return Continue();
    }

    /// <summary>
    /// Presses the main menu's Continue button, without the menu.
    ///
    /// Calling <c>AutoSaveDirector.Load</c> directly is not enough: it prepares the save but never
    /// moves the game out of the menu, so the title screen simply stays up. The landing menu holds
    /// the model behind each of its buttons, and asking the Continue one to run its behavior is
    /// exactly what a click on it does.
    /// </summary>
    private static bool Continue()
    {
        // FindObjectsOfTypeAll also returns the menu prefabs sitting in memory, and their button
        // models lead nowhere: only the one actually on screen is wired to the scene loader.
        MainMenuLandingRootUI menu = Resources.FindObjectsOfTypeAll<MainMenuLandingRootUI>()
            .FirstOrDefault(ui => ui != null && ui.isActiveAndEnabled && ui.gameObject.activeInHierarchy
                                  && ui._models != null && ui._models.Count > 0);

        if (menu == null) return false;

        foreach (ButtonBehaviorModel model in menu._models)
        {
            ContinueGameBehaviorModel resume = model.TryCast<ContinueGameBehaviorModel>();
            if (resume == null) continue;

            Main.Log.Msg($"autoContinue: continuing {resume.GameDataSummary?.SaveName}.");
            resume.InvokeBehavior();
            return true;
        }

        return false;
    }

    /// <summary>Starts hosting, the way Ranching Together's own "host" console command does.</summary>
    private static void Host(int port)
    {
        object server = Networking("Server");
        if (server == null) return;

        MethodInfo start = server.GetType().GetMethod("Start",
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance,
            binder: null, new[] { typeof(int), typeof(bool) }, modifiers: null);

        if (start == null)
        {
            Main.Log.Warning("autoHost: Ranching Together's server has no Start(int, bool).");
            return;
        }

        start.Invoke(server, new object[] { port, true });
        Main.Log.Msg($"autoHost: hosting on port {port}.");
    }

    /// <summary>Joins a session, the way the "connect" console command does.</summary>
    private static void Connect(string address)
    {
        string[] parts = address.Split(':');
        if (parts.Length != 2 || !int.TryParse(parts[1], out int port))
        {
            Main.Log.Warning($"autoConnect: '{address}' is not ip:port.");
            return;
        }

        object client = Networking("Client");
        if (client == null) return;

        MethodInfo connect = client.GetType().GetMethod("Connect",
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance,
            binder: null, new[] { typeof(string), typeof(int) }, modifiers: null);

        if (connect == null)
        {
            Main.Log.Warning("autoConnect: Ranching Together's client has no Connect(string, int).");
            return;
        }

        connect.Invoke(client, new object[] { parts[0], port });
        Main.Log.Msg($"autoConnect: joining {address}.");
    }

    /// <summary>Ranching Together's server or client object, or null with a reason in the log.</summary>
    private static object Networking(string member)
    {
        Assembly assembly = AppDomain.CurrentDomain.GetAssemblies()
            .FirstOrDefault(a => a.GetName().Name == "SR2MP");
        if (assembly == null)
        {
            Main.Log.Warning($"auto{member}: Ranching Together is not installed.");
            return null;
        }

        Type main = assembly.GetType("SR2MP.Main");
        const BindingFlags Any = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;

        object value = main?.GetProperty(member, Any)?.GetValue(null)
                       ?? main?.GetField(member, Any)?.GetValue(null);

        if (value == null) Main.Log.Warning($"auto{member}: SR2MP.Main.{member} is missing.");
        return value;
    }

}
