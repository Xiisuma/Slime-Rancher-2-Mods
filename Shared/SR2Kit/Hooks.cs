using System;
using System.Collections.Generic;
using Il2Cpp;
using Il2CppInterop.Runtime;
using Il2CppMonomiPark.SlimeRancher.SceneManagement;
using MelonLoader;

namespace SR2Kit;

/// <summary>
/// The lifecycle callbacks a content mod needs, without a modding framework.
///
/// The game already exposes the two moments that matter: <c>LookupDirector.InstanceReady</c> fires
/// once its lookups are built (the point where new identifiable types must exist), and
/// <c>SceneContext.onSceneContextLoaded</c> fires once a save is running.
/// </summary>
public static class Hooks
{
    private static readonly List<Action<LookupDirector>> LookupCallbacks = new();
    private static readonly List<Action<SceneContext>> SceneCallbacks = new();

    private static bool _wired;
    private static bool _lookupFired;

    /// <summary>
    /// Runs <paramref name="callback"/> when the lookup director is ready — the place to create and
    /// register identifiable types, prefabs and definitions. Fires immediately if it already is.
    /// </summary>
    public static void OnLookupDirectorReady(Action<LookupDirector> callback)
    {
        Wire();
        LookupCallbacks.Add(callback);

        LookupDirector director = LookupDirector.GetIfReady();
        if (_lookupFired && director != null) Invoke(callback, director);
    }

    /// <summary>Runs <paramref name="callback"/> every time a save finishes loading.</summary>
    public static void OnSceneContextReady(Action<SceneContext> callback)
    {
        Wire();
        SceneCallbacks.Add(callback);
    }

    private static void Wire()
    {
        if (_wired) return;
        _wired = true;

        LookupDirector.add_InstanceReady(
            DelegateSupport.ConvertDelegate<Il2CppSystem.Action<LookupDirector>>(
                (Action<LookupDirector>)OnLookupDirectorInstanceReady));

        SceneContext.onSceneContextLoaded = Il2CppSystem.Delegate.Combine(
            SceneContext.onSceneContextLoaded,
            DelegateSupport.ConvertDelegate<SceneContext.SceneLoadDelegate>(
                (Action<SceneContext, Il2CppSystem.Action<SceneLoadErrorData>>)OnSceneContextLoaded))
            .Cast<SceneContext.SceneLoadDelegate>();
    }

    private static void OnLookupDirectorInstanceReady(LookupDirector director)
    {
        _lookupFired = true;
        foreach (Action<LookupDirector> callback in LookupCallbacks.ToArray())
            Invoke(callback, director);
    }

    private static void OnSceneContextLoaded(SceneContext context, Il2CppSystem.Action<SceneLoadErrorData> _)
    {
        foreach (Action<SceneContext> callback in SceneCallbacks.ToArray())
        {
            try { callback(context); }
            catch (Exception e) { MelonLogger.Error($"[SR2Kit] Scene callback failed: {e}"); }
        }
    }

    private static void Invoke(Action<LookupDirector> callback, LookupDirector director)
    {
        try { callback(director); }
        catch (Exception e) { MelonLogger.Error($"[SR2Kit] Lookup callback failed: {e}"); }
    }
}
