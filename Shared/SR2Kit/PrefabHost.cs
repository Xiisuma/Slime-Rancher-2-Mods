using UnityEngine;

namespace SR2Kit;

/// <summary>
/// Parent for prefabs a mod creates at runtime.
///
/// A cloned prefab must stay inactive, survive scene loads and never be found by gameplay code that
/// scans the active scene — an inactive, hidden, <c>DontDestroyOnLoad</c> object gives all three.
/// </summary>
public static class PrefabHost
{
    private static Transform _root;

    public static Transform Root
    {
        get
        {
            if (_root != null) return _root;

            GameObject host = new("SR2Kit_Prefabs");
            host.SetActive(false);
            host.hideFlags = HideFlags.HideAndDontSave;
            Object.DontDestroyOnLoad(host);
            return _root = host.transform;
        }
    }

    /// <summary>Clones <paramref name="original"/> into the prefab host, inactive and renamed.</summary>
    public static GameObject Clone(GameObject original, string name)
    {
        GameObject clone = Object.Instantiate(original, Root, false);
        clone.name = name;
        clone.SetActive(false);
        return clone;
    }
}
