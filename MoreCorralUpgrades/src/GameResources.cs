using Il2Cpp;
using UnityEngine;

namespace MoreCorralUpgrades;

/// <summary>Lazy lookups of game assets the mod needs.</summary>
public static class GameResources
{
    private static LiquidDefinition _water;

    /// <summary>The water liquid, used by the Slime Sprinkler and the Plort Protector battery.</summary>
    public static LiquidDefinition Water
    {
        get
        {
            if (_water != null) return _water;
            foreach (LiquidDefinition liquid in Resources.FindObjectsOfTypeAll<LiquidDefinition>())
            {
                if (liquid.IsWater) return _water = liquid;
            }
            return null;
        }
    }

    /// <summary>Drops cached references when a save is unloaded.</summary>
    public static void Reset() => _water = null;
}
