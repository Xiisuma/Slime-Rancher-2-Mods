using Il2Cpp;

namespace MoreCorralUpgrades;

/// <summary>
/// Custom <see cref="LandPlot.Upgrade"/> values.
///
/// The enum only defines values 0-20, but the game never validates the range: upgrades live in a
/// <c>HashSet&lt;LandPlot.Upgrade&gt;</c> on the plot model and are persisted as plain ints
/// (see <c>LandPlotV02.WriteData</c>), so any value that does not collide with a vanilla one
/// round-trips through a save file untouched.
/// </summary>
public static class Upgrades
{
    private const int Base = 1400;

    public static readonly LandPlot.Upgrade AirNetBooster = (LandPlot.Upgrade)(Base + 0);
    public static readonly LandPlot.Upgrade PlortProtector = (LandPlot.Upgrade)(Base + 1);
    public static readonly LandPlot.Upgrade ProtectorBattery = (LandPlot.Upgrade)(Base + 2);
    public static readonly LandPlot.Upgrade MiniGarden = (LandPlot.Upgrade)(Base + 3);
    public static readonly LandPlot.Upgrade CapacityBooster = (LandPlot.Upgrade)(Base + 4);
    public static readonly LandPlot.Upgrade Miniturizer = (LandPlot.Upgrade)(Base + 5);
    public static readonly LandPlot.Upgrade ClearCrops = (LandPlot.Upgrade)(Base + 6);
    public static readonly LandPlot.Upgrade SlimeSprinkler = (LandPlot.Upgrade)(Base + 7);

    public static readonly LandPlot.Upgrade[] All =
    {
        AirNetBooster, PlortProtector, ProtectorBattery, MiniGarden,
        CapacityBooster, Miniturizer, ClearCrops, SlimeSprinkler
    };

    public static bool IsCustom(LandPlot.Upgrade upgrade) => (int)upgrade >= Base && (int)upgrade < Base + 100;
}
