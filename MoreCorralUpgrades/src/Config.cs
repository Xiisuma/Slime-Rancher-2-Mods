using MelonLoader;

namespace MoreCorralUpgrades;

/// <summary>
/// User settings, stored in <c>UserData/MelonPreferences.cfg</c>.
/// Mirrors the SRML config of the original mod.
/// </summary>
public static class Config
{
    private static MelonPreferences_Category _category;

    private static MelonPreferences_Entry<float> _miniturizerSize;
    private static MelonPreferences_Entry<float> _miniturizerAnimationTime;

    /// <summary>Scale applied to actors caught by the Miniturizer (1 = unchanged).</summary>
    public static float MiniturizerSize => _miniturizerSize.Value;

    /// <summary>Seconds the shrink/grow animation takes.</summary>
    public static float MiniturizerAnimationTime => _miniturizerAnimationTime.Value;

    public static void Initialize()
    {
        _category = MelonPreferences.CreateCategory("MoreCorralUpgrades", "More Corral Upgrades");
        _miniturizerSize = _category.CreateEntry("MiniturizerSize", 0.5f,
            description: "Size multiplier applied to slimes, food and toys inside a Miniturizer corral.");
        _miniturizerAnimationTime = _category.CreateEntry("MiniturizerAnimationTime", 1f,
            description: "Duration in seconds of the shrink/grow animation.");
    }
}
