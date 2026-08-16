using Il2Cpp;
using Il2CppMonomiPark.SlimeRancher.UI;
using UnityEngine;

namespace MoreCorralUpgrades;

/// <summary>
/// Builds the Internal Garden: a copy of the garden plot's planting bed, parented to the corral and
/// re-wired so the corral itself owns the crop.
/// </summary>
public static class InternalGarden
{
    private const string ObjectName = "MCU_InternalGarden";
    private const float Scale = 0.5f;

    /// <summary>Adds the garden to a corral, or re-activates it if it is already there.</summary>
    public static void Build(LandPlot corral)
    {
        Transform existing = corral.transform.Find(ObjectName);
        if (existing != null)
        {
            existing.gameObject.SetActive(true);
            return;
        }

        LandPlot gardenPrefab = FindGardenPrefab();
        if (gardenPrefab == null)
        {
            Main.Log.Error("Garden plot prefab not found; the Internal Garden cannot be built.");
            return;
        }

        GameObject garden = Object.Instantiate(gardenPrefab.gameObject, corral.transform, false);
        garden.name = ObjectName;
        garden.transform.localPosition = new Vector3(0f, 0f, 0f);
        garden.transform.localScale = Vector3.one * Scale;

        // The copy must not behave like a second plot: it only keeps the soil and the catcher.
        Strip<LandPlot>(garden);
        Strip<LandPlotLocation>(garden);
        Strip<TrackContainedIdentifiables>(garden);
        Strip<PlotUpgrader>(garden);
        Strip<SiloStorage>(garden);

        foreach (GardenCatcher catcher in garden.GetComponentsInChildren<GardenCatcher>(true))
            catcher.Activator = corral;

        foreach (GardenCountdownUI countdown in garden.GetComponentsInChildren<GardenCountdownUI>(true))
            countdown.plot = corral;

        garden.SetActive(true);
    }

    /// <summary>Hides the garden without destroying the crop it may hold.</summary>
    public static void Hide(LandPlot corral)
    {
        Transform existing = corral.transform.Find(ObjectName);
        if (existing != null) existing.gameObject.SetActive(false);
    }

    private static LandPlot FindGardenPrefab()
    {
        LandPlot fallback = null;
        foreach (LandPlot plot in Resources.FindObjectsOfTypeAll<LandPlot>())
        {
            if (plot.TypeId != LandPlot.Id.GARDEN) continue;
            // Prefabs live outside any scene; prefer one of those over a plot already placed on the ranch.
            if (!plot.gameObject.scene.IsValid()) return plot;
            fallback ??= plot;
        }
        return fallback;
    }

    private static void Strip<T>(GameObject root) where T : Component
    {
        foreach (T component in root.GetComponentsInChildren<T>(true))
            Object.DestroyImmediate(component);
    }
}
