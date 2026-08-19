using System;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using Il2Cpp;
using Il2CppMonomiPark.SlimeRancher.DataModel;
using Il2CppMonomiPark.SlimeRancher.Weather;
using Il2CppMonomiPark.SlimeRancher.World;
using UnityEngine;

namespace MultiplayerModPatcher;

/// <summary>
/// Keeps a zone's forecast made of that zone's own weather.
///
/// Ranching Together maps a weather state to the pattern that plays it through a lookup built once,
/// from the zone configurations loaded at that moment. A zone nobody has visited yet has no
/// configuration loaded, so its states are not in the lookup — and rather than say so, the lookup
/// falls back to any pattern with the same state name, which in practice is always the one belonging
/// to Rainbow Fields:
///
/// <code>
/// Using fallback pattern for Luminous Strand / Rain Light State: Rain Pattern Fields   // SR2MP 0.3.8
/// </code>
///
/// One evening of two-player ranching produced 2 177 of those lines, and the forecasts they built
/// name a pattern that does not belong to the zone holding them. The game reads those forecasts when
/// it works out what to draw on the map, and throws — 96 times in the same session, from
/// <c>WeatherRegistry.CalculateZoneMapData</c> — besides the three updates lost outright inside
/// <c>NetworkWeatherManager.Apply</c>.
///
/// Two things are done about it. The lookup is answered from the zone configurations loaded right
/// now instead of the ones loaded once, which resolves the pattern properly as soon as a zone is
/// live. And whatever still ends up in a forecast is checked after the update is applied: an entry
/// naming a pattern from another zone is dropped rather than left for the map to read.
/// </summary>
internal static class WeatherPatternGuard
{
    /// <summary>Seconds between two summary lines in the log.</summary>
    private const float ReportInterval = 30f;

    private static FieldInfo _lookupInitialized;

    private static int _resolved;
    private static int _dropped;
    private static float _nextReport;

    public static bool Install(HarmonyLib.Harmony harmony)
    {
        if (!SR2MPBridge.Available) return false;

        Type helper = SR2MPBridge.Type("SR2MP.Server.Managers.WeatherUpdateHelper");
        MethodInfo pattern = helper?.GetMethod("GetPatternForZoneAndState", SR2MPBridge.Any);

        MethodInfo apply = SR2MPBridge.Type("SR2MP.Client.Managers.NetworkWeatherManager")
            ?.GetMethods(SR2MPBridge.Any)
            .FirstOrDefault(method => method.Name == "Apply" && method.GetParameters().Length == 2);

        if (pattern == null || apply == null)
        {
            Main.Log.Warning("Weather patterns left as they are: Ranching Together's weather code moved.");
            return false;
        }

        // Their lookup is a one-time snapshot; clearing the flag makes it rebuild from the zones
        // loaded now, which is what makes their own answer right rather than merely overridden.
        _lookupInitialized = helper.GetField("lookupInitialized", SR2MPBridge.Any);

        harmony.Patch(pattern, postfix: new HarmonyMethod(
            typeof(WeatherPatternGuard).GetMethod(nameof(AfterPattern),
                BindingFlags.NonPublic | BindingFlags.Static)));

        harmony.Patch(apply, postfix: new HarmonyMethod(
            typeof(WeatherPatternGuard).GetMethod(nameof(AfterApply),
                BindingFlags.NonPublic | BindingFlags.Static)));

        Main.Log.Msg("Weather forecasts are kept to each zone's own patterns.");
        return true;
    }

    /// <summary>
    /// Answers with the zone's own pattern whenever it has one, in place of the fallback taken from
    /// whichever zone happened to be loaded when the lookup was built.
    /// </summary>
    private static void AfterPattern(ZoneDefinition zone, string stateName,
        ref WeatherPatternDefinition __result)
    {
        try
        {
            if (zone == null || string.IsNullOrEmpty(stateName)) return;
            if (__result != null && Belongs(zone, __result)) return;

            // The state lists a pattern plays are filled as zones come to life, so a pattern that
            // cannot be found by state name is looked up by what the fallback itself describes:
            // the same weather, from the zone that actually holds it.
            WeatherPatternDefinition own = Own(zone, stateName) ?? SameWeather(zone, __result);
            if (own == null) return;

            __result = own;
            _resolved++;

            // The zone is loaded now, so their snapshot is out of date: let them rebuild it.
            _lookupInitialized?.SetValue(null, false);
        }
        catch
        {
            // A pattern that cannot be checked is left exactly as Ranching Together resolved it.
        }
    }

    /// <summary>Drops what the update left behind that no longer describes the zone holding it.</summary>
    private static void AfterApply()
    {
        try
        {
            WeatherRegistry registry = SRSingleton<SceneContext>.Instance?.WeatherRegistry;
            if (registry?._zones == null) return;

            Il2CppSystem.Collections.Generic.Dictionary<ZoneDefinition, WeatherRegistry.ZoneWeatherData>
                .Enumerator zones = registry._zones.GetEnumerator();

            while (zones.MoveNext())
            {
                ZoneDefinition zone = zones.Current.Key;
                WeatherRegistry.ZoneWeatherData data = zones.Current.Value;
                if (zone == null || data?.Forecast == null) continue;

                for (int i = data.Forecast.Count - 1; i >= 0; i--)
                {
                    WeatherModel.ForecastEntry entry = data.Forecast[i];
                    if (entry != null && entry.Pattern != null && Belongs(zone, entry.Pattern)) continue;

                    data.Forecast.RemoveAt(i);
                    _dropped++;
                }
            }
        }
        catch (Exception e)
        {
            Main.Log.Warning($"A forecast could not be checked: {e.Message}");
        }

        Report();
    }

    /// <summary>The pattern this zone plays a state with, read from the configurations loaded now.</summary>
    private static WeatherPatternDefinition Own(ZoneDefinition zone, string stateName)
    {
        WeatherRegistry registry = SRSingleton<SceneContext>.Instance?.WeatherRegistry;
        if (registry?.ZoneConfigList == null) return null;

        Il2CppSystem.Collections.Generic.List<ZoneWeatherConfig>.Enumerator configs =
            registry.ZoneConfigList.GetEnumerator();

        while (configs.MoveNext())
        {
            ZoneWeatherConfig config = configs.Current;
            if (config == null || !Same(config.Zone, zone) || config.Patterns == null) continue;

            Il2CppSystem.Collections.Generic.List<WeatherPatternDefinition>.Enumerator patterns =
                config.Patterns.GetEnumerator();

            while (patterns.MoveNext())
            {
                WeatherPatternDefinition pattern = patterns.Current;
                if (pattern != null && Plays(pattern, stateName)) return pattern;
            }
        }

        return null;
    }

    /// <summary>True when the zone's own configuration lists this pattern.</summary>
    private static bool Belongs(ZoneDefinition zone, WeatherPatternDefinition pattern)
    {
        WeatherRegistry registry = SRSingleton<SceneContext>.Instance?.WeatherRegistry;
        if (registry?.ZoneConfigList == null) return true;

        bool configured = false;

        Il2CppSystem.Collections.Generic.List<ZoneWeatherConfig>.Enumerator configs =
            registry.ZoneConfigList.GetEnumerator();

        while (configs.MoveNext())
        {
            ZoneWeatherConfig config = configs.Current;
            if (config == null || !Same(config.Zone, zone) || config.Patterns == null) continue;

            configured = true;
            if (config.Patterns.Contains(pattern)) return true;
        }

        // A zone whose configuration is not loaded cannot answer, and a forecast is not worth
        // dropping on a question nobody can answer yet.
        return !configured;
    }

    /// <summary>
    /// The zone's own pattern for the same weather as another zone's, matched on the metadata the
    /// game itself uses to describe a pattern — its map icon and name. It is what makes "the rain
    /// of Rainbow Fields" resolvable to "the rain of Luminous Strand" before either zone has told
    /// anyone which states it plays.
    /// </summary>
    private static WeatherPatternDefinition SameWeather(ZoneDefinition zone, WeatherPatternDefinition foreign)
    {
        if (foreign?.Metadata == null) return null;

        WeatherRegistry registry = SRSingleton<SceneContext>.Instance?.WeatherRegistry;
        if (registry?.ZoneConfigList == null) return null;

        Il2CppSystem.Collections.Generic.List<ZoneWeatherConfig>.Enumerator configs =
            registry.ZoneConfigList.GetEnumerator();

        while (configs.MoveNext())
        {
            ZoneWeatherConfig config = configs.Current;
            if (config == null || !Same(config.Zone, zone) || config.Patterns == null) continue;

            Il2CppSystem.Collections.Generic.List<WeatherPatternDefinition>.Enumerator patterns =
                config.Patterns.GetEnumerator();

            while (patterns.MoveNext())
            {
                WeatherPatternDefinition pattern = patterns.Current;
                if (pattern != null && pattern.Metadata == foreign.Metadata) return pattern;
            }
        }

        return null;
    }

    /// <summary>
    /// Two zone definitions are the same zone when they are named the same: the registry and the
    /// configuration list do not always hand out the same instance, and a reference comparison then
    /// silently answers "different zone" for every zone but one.
    /// </summary>
    private static bool Same(ZoneDefinition left, ZoneDefinition right) =>
        left != null && right != null && (left == right || left.name == right.name);

    private static bool Plays(WeatherPatternDefinition pattern, string stateName)
    {
        if (pattern._stateList == null) return false;

        Il2CppSystem.Collections.Generic.HashSet<WeatherStateDefinition>.Enumerator states =
            pattern._stateList.GetEnumerator();

        while (states.MoveNext())
        {
            WeatherStateDefinition state = states.Current;
            if (state != null && state.name == stateName) return true;
        }

        return false;
    }

    /// <summary>
    /// One line naming the zones whose weather configuration is loaded. A zone missing from that
    /// list is a zone whose forecasts Ranching Together can only guess at, which is the whole
    /// reason this guard exists.
    /// </summary>
    public static void Describe()
    {
        WeatherRegistry registry = SRSingleton<SceneContext>.Instance?.WeatherRegistry;
        if (registry?.ZoneConfigList == null) return;

        System.Collections.Generic.List<string> zones = new();

        Il2CppSystem.Collections.Generic.List<ZoneWeatherConfig>.Enumerator configs =
            registry.ZoneConfigList.GetEnumerator();

        while (configs.MoveNext())
        {
            ZoneWeatherConfig config = configs.Current;
            if (config?.Zone != null) zones.Add(config.Zone.name);
        }

        Main.Log.Msg($"Weather configurations loaded for {zones.Count} zone(s): " +
                     $"{string.Join(", ", zones)}.");
    }

    /// <summary>
    /// Writes which zones have their weather configuration loaded, and how many patterns each one
    /// knows. A zone missing from that list is a zone whose forecasts can only be guessed at.
    /// </summary>
    public static void Snapshot()
    {
        WeatherRegistry registry = SRSingleton<SceneContext>.Instance?.WeatherRegistry;
        if (registry?.ZoneConfigList == null)
        {
            Main.Log.Msg("No weather registry to describe.");
            return;
        }

        Main.Log.Msg($"Weather configurations loaded ({registry.ZoneConfigList.Count}):");

        Il2CppSystem.Collections.Generic.List<ZoneWeatherConfig>.Enumerator configs =
            registry.ZoneConfigList.GetEnumerator();

        while (configs.MoveNext())
        {
            ZoneWeatherConfig config = configs.Current;
            if (config == null) continue;

            int states = 0;
            System.Collections.Generic.List<string> named = new();
            if (config.Patterns != null)
            {
                Il2CppSystem.Collections.Generic.List<WeatherPatternDefinition>.Enumerator patterns =
                    config.Patterns.GetEnumerator();

                while (patterns.MoveNext())
                {
                    WeatherPatternDefinition pattern = patterns.Current;
                    if (pattern == null) continue;

                    int played = pattern._stateList?.Count ?? 0;
                    states += played;
                    named.Add($"{pattern.name} ({played} state(s))");
                }
            }

            Main.Log.Msg($"  zone {(config.Zone == null ? "<none>" : config.Zone.name)}: " +
                         $"{config.Patterns?.Count ?? 0} pattern(s), {states} state(s) [{string.Join(" | ", named)}]");
        }
    }

    private static void Report()
    {
        if (_resolved == 0 && _dropped == 0) return;
        if (Time.time < _nextReport) return;

        _nextReport = Time.time + ReportInterval;
        Main.Log.Msg($"Weather: {_resolved} pattern(s) resolved to the right zone, " +
                     $"{_dropped} forecast entry(ies) dropped as belonging elsewhere.");

        _resolved = 0;
        _dropped = 0;
    }
}
