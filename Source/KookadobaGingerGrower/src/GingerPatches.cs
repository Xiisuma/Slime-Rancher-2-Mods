using Il2Cpp;
using UnityEngine;

namespace KookadobaGingerGrower;

/// <summary>
/// Reports where the wild ginger grows.
///
/// The crop has to be started from a real Gilded Ginger, and the game gives no map marker for the
/// patches, so the mod lists the ones that streamed in — enough to walk to the nearest one.
/// </summary>
public static class GingerPatches
{
    /// <summary>Beyond this, the list stops being useful and becomes log spam.</summary>
    private const int Limit = 20;

    private static bool _listed;

    /// <summary>Logs the wild ginger patches present in the running save. Once per save.</summary>
    public static void List()
    {
        if (_listed) return;

        Il2CppInterop.Runtime.InteropTypes.Arrays.Il2CppArrayBase<GingerPatchNode> nodes =
            Object.FindObjectsOfType<GingerPatchNode>();
        if (nodes == null || nodes.Length == 0) return;

        _listed = true;
        int shown = 0;

        foreach (GingerPatchNode node in nodes)
        {
            if (node == null) continue;
            if (shown++ >= Limit) break;

            Vector3 position = node.transform.position;
            Main.Log.Msg($"Wild ginger in {node.gameObject.scene.name} " +
                         $"at ({position.x:F0}, {position.y:F0}, {position.z:F0}).");
        }

        Main.Log.Msg($"{nodes.Length} wild ginger patches loaded.");
    }

    /// <summary>Allows the list to be printed again for the next save.</summary>
    public static void Reset() => _listed = false;
}
