using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;

namespace MultiplayerModPatcher;

/// <summary>
/// Says what was in a weather update that failed to apply.
///
/// Ranching Together rebuilds the whole forecast of every zone from one packet. When anything in it
/// is null the exception unwinds the entire method, so a single unresolvable entry costs the update
/// for every zone — the log only says
/// <c>Packet handler for WeatherPacket threw: NullReferenceException</c>, which names nothing.
///
/// The guard runs after the failure and writes down what the packet was carrying: the zones it named
/// and, for each, whether the states it wanted are actually resolvable here. That is the difference
/// between "the host sent a state this client cannot resolve" and "the client's registry was not
/// ready", which need opposite fixes.
/// </summary>
internal static class WeatherGuard
{
    private static FieldInfo _zones;
    private static FieldInfo _forecasts;
    private static FieldInfo _state;

    public static bool Install(HarmonyLib.Harmony harmony)
    {
        if (!SR2MPBridge.Available) return false;

        MethodInfo apply = SR2MPBridge.Type("SR2MP.Client.Managers.NetworkWeatherManager")
            ?.GetMethods(SR2MPBridge.Any)
            .FirstOrDefault(method => method.Name == "Apply" && method.GetParameters().Length == 2);
        if (apply == null) return false;

        Type packet = SR2MPBridge.Type("SR2MP.Packets.World.WeatherPacket");
        _zones = packet?.GetField("Zones", SR2MPBridge.Any);

        Type zoneData = SR2MPBridge.Type("SR2MP.Packets.World.WeatherZoneData");
        _forecasts = zoneData?.GetField("WeatherForecasts", SR2MPBridge.Any);

        Type forecast = SR2MPBridge.Type("SR2MP.Packets.World.WeatherForecast");
        _state = forecast?.GetField("State", SR2MPBridge.Any);

        if (_zones == null || _forecasts == null || _state == null) return false;

        harmony.Patch(apply, finalizer: new HarmonyMethod(
            typeof(WeatherGuard).GetMethod(nameof(AfterApply),
                BindingFlags.NonPublic | BindingFlags.Static)));

        Main.Log.Msg("Weather updates are watched for the entry that breaks them.");
        return true;
    }

    /// <summary>
    /// Harmony finalizer: runs whether or not the update threw, and returning the exception leaves
    /// Ranching Together's own handling exactly as it was.
    /// </summary>
    private static Exception AfterApply(object packet, Exception __exception)
    {
        if (__exception == null || packet == null) return __exception;

        try
        {
            Main.Log.Warning($"A weather update was lost ({__exception.GetType().Name}). It carried:");

            foreach (DictionaryEntry zone in Zones(packet))
                Main.Log.Warning($"  zone {zone.Key}: {Describe(zone.Value)}");
        }
        catch (Exception e)
        {
            Main.Log.Warning($"The weather update could not even be described: {e.Message}");
        }

        return __exception;
    }

    private static IEnumerable Zones(object packet) =>
        _zones.GetValue(packet) as IEnumerable ?? Array.Empty<DictionaryEntry>();

    /// <summary>Lists the states a zone's forecast asks for, marking the ones that arrived empty.</summary>
    private static string Describe(object zoneData)
    {
        if (_forecasts.GetValue(zoneData) is not IEnumerable forecasts) return "no forecast";

        List<string> states = new();
        foreach (object forecast in forecasts)
        {
            object state = _state.GetValue(forecast);
            states.Add(state == null ? "<null state>" : Name(state));
        }

        return states.Count == 0 ? "empty forecast" : string.Join(", ", states);
    }

    private static string Name(object state)
    {
        try
        {
            return (state as UnityEngine.Object)?.name ?? state.ToString();
        }
        catch
        {
            return "<unreadable>";
        }
    }
}
