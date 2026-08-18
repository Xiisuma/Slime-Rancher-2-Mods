using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Il2Cpp;

namespace MultiplayerModPatcher;

/// <summary>
/// Everything this mod knows about Ranching Together (SR2MP).
///
/// The multiplayer mod is reached by reflection rather than by an assembly reference: its types are
/// internal, its layout changes between releases, and a player may well run a version this patcher
/// has never seen. Every member is looked up by name once and kept nullable — if one is missing the
/// bridge reports itself unavailable and the patcher stays out of the way instead of throwing in the
/// middle of a session.
/// </summary>
internal static class SR2MPBridge
{
    public const BindingFlags Any = BindingFlags.Public | BindingFlags.NonPublic
                                    | BindingFlags.Static | BindingFlags.Instance;

    private static bool _resolved;

    private static Assembly _assembly;
    private static Type _globalVariables;
    private static MemberInfo _actorManagerMember;
    private static FieldInfo _actorTypesField;

    private static MethodInfo _applyOwnership;
    private static MethodInfo _sendToAllOrServer;
    private static Type _transferPacket;
    private static FieldInfo _packetActorId;
    private static FieldInfo _packetOwnerId;
    private static PropertyInfo _localId;
    private static PropertyInfo _actorIdProperty;

    /// <summary>Version SR2MP declares, for the log.</summary>
    public static string Version { get; private set; }

    /// <summary>True when every member this patcher needs was found.</summary>
    public static bool Available { get; private set; }

    /// <summary>Why the bridge is unavailable, for a single explanatory log line.</summary>
    public static string Unavailable { get; private set; } = "not looked up yet";

    public static void Resolve()
    {
        if (_resolved) return;
        _resolved = true;

        _assembly = AppDomain.CurrentDomain.GetAssemblies()
            .FirstOrDefault(a => a.GetName().Name == "SR2MP");

        if (_assembly == null)
        {
            Unavailable = "Ranching Together (SR2MP) is not installed";
            return;
        }

        Version = _assembly.GetName().Version?.ToString();

        _globalVariables = _assembly.GetType("SR2MP.GlobalVariables");
        if (_globalVariables == null)
        {
            Unavailable = "SR2MP.GlobalVariables is missing";
            return;
        }

        _actorManagerMember = (MemberInfo)_globalVariables.GetProperty("ActorManager", Any)
                              ?? _globalVariables.GetField("ActorManager", Any);
        if (_actorManagerMember == null)
        {
            Unavailable = "SR2MP.GlobalVariables.ActorManager is missing";
            return;
        }

        Type manager = Type("SR2MP.Shared.Managers.NetworkActorManager");
        _actorTypesField = manager?.GetField("ActorTypes", Any);
        if (_actorTypesField == null)
        {
            Unavailable = "NetworkActorManager.ActorTypes is missing";
            return;
        }

        Available = true;
        Unavailable = null;

        ResolveOwnership(manager);
    }

    /// <summary>The members the watchdog needs to hand an actor over; optional, unlike the rest.</summary>
    private static void ResolveOwnership(Type manager)
    {
        _applyOwnership = manager.GetMethod("ApplyOwnership", Any);
        _transferPacket = Type("SR2MP.Packets.Actor.ActorTransferPacket");
        _packetActorId = _transferPacket?.GetField("ActorId", Any);
        _packetOwnerId = _transferPacket?.GetField("OwnerId", Any);
        _localId = _globalVariables.GetProperty("LocalID", Any);
        _actorIdProperty = Type("SR2MP.Components.Actor.NetworkActor")?.GetProperty("ActorId", Any);

        _sendToAllOrServer = _assembly.GetType("SR2MP.Main")
            ?.GetMethods(Any)
            .FirstOrDefault(m => m.Name == "SendToAllOrServer" && m.IsGenericMethodDefinition);
    }

    public static Type Type(string fullName) => _assembly?.GetType(fullName);

    /// <summary>SR2MP's actor manager instance, or null before a save is running.</summary>
    public static object ActorManager() => _actorManagerMember switch
    {
        PropertyInfo property => property.GetValue(null),
        FieldInfo field => field.GetValue(null),
        _ => null
    };

    /// <summary>
    /// SR2MP's actor type table: persistence id to identifiable type. It is built once, when a save
    /// loads, from the game's save reference translation — so a type registered afterwards is absent
    /// from it and cannot be named in a packet.
    /// </summary>
    public static IDictionary<int, IdentifiableType> ActorTypes()
    {
        object manager = ActorManager();
        return manager == null
            ? null
            : _actorTypesField.GetValue(manager) as IDictionary<int, IdentifiableType>;
    }

    /// <summary>
    /// Takes ownership of one actor and tells the other players, exactly as SR2MP does when its own
    /// sweep claims something: apply it here first, then broadcast it.
    /// </summary>
    public static bool Claim(object networkActor)
    {
        if (_applyOwnership == null || _transferPacket == null || _packetActorId == null
            || _packetOwnerId == null || _localId == null || _actorIdProperty == null
            || _sendToAllOrServer == null)
            return false;

        if (_localId.GetValue(null) is not string localId || localId.Length == 0) return false;

        object actorId = _actorIdProperty.GetValue(networkActor);
        if (actorId == null || Id(actorId) == 0) return false;

        object packet = Activator.CreateInstance(_transferPacket);
        _packetActorId.SetValue(packet, actorId);
        _packetOwnerId.SetValue(packet, localId);

        _applyOwnership.Invoke(null, new[] { packet });
        _sendToAllOrServer.MakeGenericMethod(_transferPacket).Invoke(null, new[] { packet });
        return true;
    }

    /// <summary>The numeric value behind SR2MP's ActorId wrapper; 0 means "no actor".</summary>
    private static long Id(object actorId)
    {
        FieldInfo value = actorId.GetType().GetField("Value", Any);
        if (value != null) return Convert.ToInt64(value.GetValue(actorId));

        PropertyInfo property = actorId.GetType().GetProperty("Value", Any);
        return property == null ? 0 : Convert.ToInt64(property.GetValue(actorId));
    }
}
