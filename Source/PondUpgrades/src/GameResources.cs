using Il2Cpp;
using UnityEngine;

namespace PondUpgrades;

/// <summary>Lazy lookups of game assets the mod needs.</summary>
public static class GameResources
{
    private const string AncientWaterMaterialName = "Depth Magic Water Ball";

    private static Sprite _ancientWaterIcon;
    private static Material _ancientWaterMaterial;

    /// <summary>Icon of the magic water, used by the Ancient Blessing shop entry.</summary>
    public static Sprite AncientWaterIcon
    {
        get
        {
            if (_ancientWaterIcon != null) return _ancientWaterIcon;
            foreach (IdentifiableType type in Resources.FindObjectsOfTypeAll<IdentifiableType>())
            {
                string id = type.ReferenceId;
                if (string.IsNullOrEmpty(id) || !id.ToLowerInvariant().Contains("magic_water")) continue;
                if (type.icon != null) return _ancientWaterIcon = type.icon;
            }
            return null;
        }
    }

    /// <summary>Material applied to the pond surface once it is blessed.</summary>
    public static Material AncientWaterMaterial
    {
        get
        {
            if (_ancientWaterMaterial != null) return _ancientWaterMaterial;
            foreach (Material material in Resources.FindObjectsOfTypeAll<Material>())
            {
                if (material.name == AncientWaterMaterialName) return _ancientWaterMaterial = material;
            }
            return null;
        }
    }

    /// <summary>Drops cached references when a save is unloaded.</summary>
    public static void Reset()
    {
        _ancientWaterIcon = null;
        _ancientWaterMaterial = null;
    }
}
