using Il2Cpp;

namespace PondUpgrades;

/// <summary>
/// Custom <see cref="LandPlot.Upgrade"/> values.
///
/// The enum stops at 20, but the game never validates the range: upgrades live in a
/// <c>HashSet&lt;LandPlot.Upgrade&gt;</c> on the plot model and are persisted as plain ints
/// (see <c>LandPlotV02.WriteData</c>), so any value that does not collide with a vanilla one
/// round-trips through a save file untouched.
/// </summary>
public static class Upgrades
{
    private const int Base = 1500;

    public static readonly LandPlot.Upgrade SlimeCapacity = (LandPlot.Upgrade)(Base + 0);
    public static readonly LandPlot.Upgrade PlortCapacity = (LandPlot.Upgrade)(Base + 1);
    public static readonly LandPlot.Upgrade AncientBlessing = (LandPlot.Upgrade)(Base + 2);

    public static bool IsCustom(LandPlot.Upgrade upgrade) => (int)upgrade >= Base && (int)upgrade < Base + 100;
}
