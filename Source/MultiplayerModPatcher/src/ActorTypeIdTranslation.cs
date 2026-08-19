using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using Il2Cpp;
using Il2CppMonomiPark.SlimeRancher;
using UnityEngine;

namespace MultiplayerModPatcher;

/// <summary>
/// Names a thing to the host the way the host names it.
///
/// Ranching Together identifies an actor's kind by the index the save system gives its identifiable
/// type, and those indexes are per-machine: they come from a table built at load time, so a player
/// with one extra content mod has different numbers for everything that follows it.
///
/// The mod already knows this in one direction. On connection the host sends its whole table as
/// reference ids, and the client rewrites its own lookup to match:
///
/// <code>
/// GlobalVariables.ActorManager.ActorTypes[num] = identifiableType;   // SR2MP 0.3.8, ActorTypeRegistryHandler
/// </code>
///
/// so anything the host sends is read correctly. But what the client sends back is numbered by its
/// own table and never translated:
///
/// <code>
/// public static int GetPersistentID(IdentifiableType type) =&gt;
///     SRSingleton&lt;GameContext&gt;.Instance.AutoSaveDirector._saveReferenceTranslation.GetPersistenceId(type);
/// </code>
///
/// While both players run exactly the same mods the two numberings coincide and nothing shows. As
/// soon as they differ, everything the client sends arrives as the wrong kind of thing on the host —
/// a slime turns into something else on the other screen.
///
/// The translation the host sent is kept here, and a client's outgoing ids go through it.
/// </summary>
internal static class ActorTypeIdTranslation
{
    /// <summary>Reference id to the id the host uses for it.</summary>
    private static readonly Dictionary<string, int> HostIds = new();

    /// <summary>Types the host has no id for, so each one is only reported once.</summary>
    private static readonly HashSet<string> Unknown = new();

    private static FieldInfo _registry;
    private static PropertyInfo _client;
    private static PropertyInfo _server;
    private static PropertyInfo _connected;
    private static PropertyInfo _running;

    private static int _translated;
    private static float _nextReport;

    /// <summary>Seconds between two summary lines in the log.</summary>
    private const float ReportInterval = 30f;

    public static bool Install(HarmonyLib.Harmony harmony)
    {
        if (!SR2MPBridge.Available) return false;

        MethodInfo handler = SR2MPBridge.Type("SR2MP.Handlers.Actor.ActorTypeRegistryHandler")
            ?.GetMethods(SR2MPBridge.Any)
            .FirstOrDefault(method => method.Name == "Handle" && method.GetParameters().Length == 2
                                      && !method.IsAbstract);

        MethodInfo persistentId = SR2MPBridge.Type("SR2MP.Shared.Managers.NetworkActorManager")
            ?.GetMethods(SR2MPBridge.Any)
            .FirstOrDefault(method => method.Name == "GetPersistentID" && method.IsStatic
                                      && method.GetParameters().Length == 1);

        _registry = SR2MPBridge.Type("SR2MP.Packets.Actor.ActorTypeRegistryPacket")
            ?.GetField("Registry", SR2MPBridge.Any);

        Type main = SR2MPBridge.Type("SR2MP.Main");
        _client = main?.GetProperty("Client", SR2MPBridge.Any);
        _server = main?.GetProperty("Server", SR2MPBridge.Any);
        _connected = _client?.PropertyType.GetProperty("IsConnected", SR2MPBridge.Any);
        _running = _server?.PropertyType.GetProperty("IsRunning", SR2MPBridge.Any);

        if (handler == null || persistentId == null || _registry == null
            || _connected == null || _running == null)
        {
            Main.Log.Warning("Actor type ids left as they are: Ranching Together's registry code moved.");
            return false;
        }

        harmony.Patch(handler, postfix: new HarmonyMethod(
            typeof(ActorTypeIdTranslation).GetMethod(nameof(AfterRegistry),
                BindingFlags.NonPublic | BindingFlags.Static)));

        harmony.Patch(persistentId, postfix: new HarmonyMethod(
            typeof(ActorTypeIdTranslation).GetMethod(nameof(AfterPersistentId),
                BindingFlags.NonPublic | BindingFlags.Static)));

        Main.Log.Msg("Actor types are sent to the host under the host's own ids.");
        return true;
    }

    /// <summary>Keeps the table the host sent, which is the only translation a client has.</summary>
    private static void AfterRegistry(object packet)
    {
        try
        {
            if (_registry.GetValue(packet) is not IEnumerable<KeyValuePair<int, string>> registry) return;

            HostIds.Clear();
            Unknown.Clear();

            foreach (KeyValuePair<int, string> entry in registry)
            {
                if (!string.IsNullOrEmpty(entry.Value)) HostIds[entry.Value] = entry.Key;
            }

            Main.Log.Msg($"The host names {HostIds.Count} kinds of thing; " +
                         $"{Different()} of them are numbered differently here.");
        }
        catch (Exception e)
        {
            Main.Log.Warning($"The host's actor type table could not be kept: {e.Message}");
            HostIds.Clear();
        }
    }

    /// <summary>Turns a local id into the one the host knows the same type by.</summary>
    private static void AfterPersistentId(IdentifiableType type, ref int __result)
    {
        try
        {
            if (HostIds.Count == 0 || type == null || !AsClient()) return;

            string referenceId = type.ReferenceId;
            if (string.IsNullOrEmpty(referenceId)) return;

            if (HostIds.TryGetValue(referenceId, out int hostId))
            {
                if (hostId == __result) return;

                __result = hostId;
                _translated++;
                return;
            }

            // Nothing sent under this id can be understood: the host has no such type at all.
            if (Unknown.Add(referenceId))
                Main.Log.Warning($"The host does not know {referenceId}; anything sent about it will " +
                                 "arrive as something else. Both players need the same content mods.");
        }
        catch
        {
            // An id that cannot be translated is left as Ranching Together computed it.
        }
    }

    /// <summary>True while this game is a client of someone else's session.</summary>
    public static bool ConnectedAsClient() => AsClient();

    private static bool AsClient()
    {
        object server = _server?.GetValue(null);
        if (server != null && _running.GetValue(server) is true) return false;

        object client = _client?.GetValue(null);
        return client != null && _connected.GetValue(client) is true;
    }

    /// <summary>How many of the host's ids differ from this game's own, for the log.</summary>
    private static int Different()
    {
        SaveReferenceTranslation translation =
            GameContext.Instance?.AutoSaveDirector?._saveReferenceTranslation;
        if (translation?._identifiableTypeLookup == null) return 0;

        int different = 0;

        Il2CppSystem.Collections.Generic.Dictionary<string, IdentifiableType>.Enumerator types =
            translation._identifiableTypeLookup.GetEnumerator();

        while (types.MoveNext())
        {
            IdentifiableType type = types.Current.value;
            if (type == null || string.IsNullOrEmpty(type.ReferenceId)) continue;

            if (HostIds.TryGetValue(type.ReferenceId, out int hostId)
                && hostId != translation.GetPersistenceId(type))
                different++;
        }

        return different;
    }

    /// <summary>Logs what has been translated, once a session rather than once a packet.</summary>
    public static void Report()
    {
        if (_translated == 0 || Time.time < _nextReport) return;

        _nextReport = Time.time + ReportInterval;

        Main.Log.Msg($"{_translated} actor type id(s) translated to the host's numbering.");
        _translated = 0;
    }
}
