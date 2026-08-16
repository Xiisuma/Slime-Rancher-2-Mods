using Il2Cpp;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using SR2Kit;
using UnityEngine;

namespace KookadobaGingerGrower;

/// <summary>
/// Turns Gilded Ginger into a garden crop.
///
/// A garden accepts a crop when its <see cref="GardenCatcher"/> lists a plant slot for it, and the
/// slot points at the patch prefab to grow. Both halves are built here by cloning an existing
/// veggie patch: the clone keeps the garden's soil, joints, audio and growth logic, and only the
/// <see cref="ResourceGrowerDefinition"/> driving it is swapped for one that yields ginger.
/// </summary>
public static class GingerCrop
{
    /// <summary>Reference id of the crop the new patch is cloned from.</summary>
    private const string TemplateCrop = "beet";

    /// <summary>Asset name of the Gilded Ginger identifiable type.</summary>
    private const string GingerAsset = "GingerVeggie";

    private const string GrowerId = "KookadobaGingerGrower_GingerGarden";
    private const string DeluxeGrowerId = "KookadobaGingerGrower_GingerGardenDeluxe";

    /// <summary>The Gilded Ginger identifiable type, once found.</summary>
    public static IdentifiableType Ginger { get; private set; }

    /// <summary>The plant slot added to every garden.</summary>
    public static GardenCatcher.PlantSlot Slot { get; private set; }

    /// <summary>Grower definitions of the normal and deluxe ginger patches.</summary>
    public static ResourceGrowerDefinition Grower { get; private set; }
    public static ResourceGrowerDefinition DeluxeGrower { get; private set; }

    private static bool _built;

    /// <summary>
    /// Builds the crop and registers it on the garden plot prefab. Idempotent: it is called both
    /// when the lookup director is ready and when a save loads, whichever gets the assets first.
    /// </summary>
    public static void Build()
    {
        if (_built) return;

        LookupDirector director = LookupDirector.GetIfReady();
        if (director == null) return;

        GameObject gardenPrefab = director.GetPlotPrefab(LandPlot.Id.GARDEN);
        GardenCatcher catcher = gardenPrefab == null
            ? null
            : gardenPrefab.GetComponentInChildren<GardenCatcher>(true);
        if (catcher == null || catcher.Plantable == null) return;

        GardenCatcher.PlantSlot template = FindTemplate(catcher);
        if (template == null)
        {
            Main.Log.Error("No vanilla plant slot to clone; the crop cannot be created.");
            return;
        }
        IdentifiableType ginger = FindGinger();
        if (ginger == null)
        {
            Main.Log.Error("Gilded Ginger not found; the crop cannot be created.");
            return;
        }

        Ginger = ginger;

        GameObject normal = ClonePatch(template.PlantedPrefab, "patchGardenGinger", GrowerId, ginger,
            out ResourceGrowerDefinition grower);
        GameObject deluxe = ClonePatch(template.DeluxePlantedPrefab, "patchGardenGingerDlx", DeluxeGrowerId, ginger,
            out ResourceGrowerDefinition deluxeGrower);
        if (normal == null) return;

        Grower = grower;
        DeluxeGrower = deluxeGrower;

        Slot = new GardenCatcher.PlantSlot
        {
            IdentType = ginger,
            PlantedPrefab = normal,
            DeluxePlantedPrefab = deluxe != null ? deluxe : normal
        };

        AddSlot(catcher, Slot);
        _built = true;

        Main.Log.Msg($"Gilded Ginger ({ginger.ReferenceId}) is now plantable in gardens.");
    }

    /// <summary>Makes an existing garden accept ginger. Called from the catcher's Awake patch.</summary>
    public static void Inject(GardenCatcher catcher)
    {
        if (Slot == null || Ginger == null || catcher == null) return;
        if (catcher._plantableDict == null || catcher._deluxeDict == null) return;

        catcher._plantableDict[Ginger] = Slot.PlantedPrefab;
        catcher._deluxeDict[Ginger] = Slot.DeluxePlantedPrefab;
    }

    /// <summary>
    /// Finds the Gilded Ginger type. It is not in the lookup director's reference-id map  like Oca
    /// Oca and Silver Parsnip, it is only reachable as a loaded asset  so the search goes through
    /// every identifiable type currently in memory.
    /// </summary>
    private static IdentifiableType FindGinger()
    {
        foreach (IdentifiableType type in Resources.FindObjectsOfTypeAll<IdentifiableType>())
        {
            if (type == null) continue;
            if (type.name == GingerAsset) return type;
        }
        return Lookup.FindIdentifiable("ginger");
    }

    /// <summary>
    /// Picks the vanilla slot to clone. A veggie patch is preferred over a fruit tree, because
    /// ginger is a veggie and the tree prefabs carry trunk and canopy geometry ginger has no use for.
    /// </summary>
    private static GardenCatcher.PlantSlot FindTemplate(GardenCatcher catcher)
    {
        GardenCatcher.PlantSlot fallback = null;
        foreach (GardenCatcher.PlantSlot slot in catcher.Plantable)
        {
            if (slot == null || slot.IdentType == null || slot.PlantedPrefab == null) continue;

            string refId = slot.IdentType.ReferenceId;
            if (refId != null && refId.ToLowerInvariant().Contains(TemplateCrop)) return slot;
            fallback ??= slot;
        }
        return fallback;
    }

    /// <summary>
    /// Clones a planted patch and re-points its spawners at a ginger-producing grower definition.
    /// </summary>
    private static GameObject ClonePatch(GameObject source, string name, string persistenceId,
        IdentifiableType ginger, out ResourceGrowerDefinition grower)
    {
        grower = null;
        if (source == null) return null;

        GameObject clone = PrefabHost.Clone(source, name);

        // PrefabHost deactivates its clones, but the host object is already inactive, which is what
        // keeps this one dormant. Re-enabling the clone itself makes the copies the game
        // instantiates from it start active, exactly like a copy of the vanilla prefab would.
        clone.SetActive(true);

        Il2CppArrayBase<SpawnResource> spawners = clone.GetComponentsInChildren<SpawnResource>(true);
        if (spawners == null || spawners.Length == 0)
        {
            Main.Log.Error($"{source.name} has no SpawnResource; ginger cannot grow on its clone.");
            return null;
        }

        ResourceGrowerDefinition source_grower = spawners[0]._resourceGrowerDefinition;
        if (source_grower == null)
        {
            Main.Log.Error($"{source.name} has no grower definition; ginger cannot grow on its clone.");
            return null;
        }

        grower = Object.Instantiate(source_grower);
        grower.hideFlags = HideFlags.HideAndDontSave;
        grower.name = persistenceId;

        // The persistence id is what the save file stores for the crop planted in a plot, so it has
        // to be stable and mod-prefixed; the prefab is what the plot re-instantiates on load.
        grower._persistenceId = persistenceId;
        grower._primaryResourceType = ginger;
        grower._prefab = clone;
        grower._resources = SingleResource(source_grower, ginger);

        foreach (SpawnResource spawner in spawners)
            spawner._resourceGrowerDefinition = grower;

        Reskin(clone, ginger);
        return clone;
    }

    /// <summary>
    /// Dresses the cloned bed with the ginger model, the way the original mod re-skinned its
    /// patches: the sprouts poking out of the soil and the spawn joints are the only parts of a
    /// patch that show the crop, everything else is dirt.
    /// </summary>
    private static void Reskin(GameObject patch, IdentifiableType ginger)
    {
        if (ginger.prefab == null) return;

        // The root of a resource holds the physics mesh and no renderer; the visible model is the
        // "model_" child, which is the one worth copying onto the bed.
        MeshFilter model = null;
        MeshRenderer modelRenderer = null;
        foreach (MeshFilter filter in ginger.prefab.GetComponentsInChildren<MeshFilter>(true))
        {
            if (filter.sharedMesh == null || !filter.name.StartsWith("model")) continue;

            MeshRenderer renderer = filter.GetComponent<MeshRenderer>();
            if (renderer == null || renderer.sharedMaterial == null) continue;

            model = filter;
            modelRenderer = renderer;
            break;
        }
        if (model == null) return;

        foreach (MeshFilter filter in patch.GetComponentsInChildren<MeshFilter>(true))
        {
            string name = filter.name;
            if (!name.StartsWith("Sprout") && !name.StartsWith("SpawnJoint")) continue;

            MeshRenderer renderer = filter.GetComponent<MeshRenderer>();
            if (renderer == null) continue;

            filter.sharedMesh = model.sharedMesh;
            renderer.sharedMaterial = modelRenderer.sharedMaterial;
        }
    }

    /// <summary>Replaces a grower's weighted crop table with ginger alone, keeping its yield size.</summary>
    private static Il2CppReferenceArray<ResourceSpawnerDefinition.WeightedResourceEntry> SingleResource(
        ResourceGrowerDefinition template, IdentifiableType ginger)
    {
        int minimum = 1;
        if (template._resources != null && template._resources.Length > 0 && template._resources[0] != null)
            minimum = template._resources[0].MinimumAmount;

        Il2CppReferenceArray<ResourceSpawnerDefinition.WeightedResourceEntry> resources = new(1);
        resources[0] = new ResourceSpawnerDefinition.WeightedResourceEntry
        {
            ResourceIdentifiableType = ginger,
            Weight = 1f,
            MinimumAmount = minimum
        };
        return resources;
    }

    /// <summary>Appends a slot to the garden prefab, so every garden built from it accepts ginger.</summary>
    private static void AddSlot(GardenCatcher catcher, GardenCatcher.PlantSlot slot)
    {
        Il2CppReferenceArray<GardenCatcher.PlantSlot> slots = catcher.Plantable;
        foreach (GardenCatcher.PlantSlot existing in slots)
        {
            if (existing != null && existing.IdentType == slot.IdentType) return;
        }

        Il2CppReferenceArray<GardenCatcher.PlantSlot> grown = new(slots.Length + 1);
        for (int i = 0; i < slots.Length; i++) grown[i] = slots[i];
        grown[slots.Length] = slot;
        catcher.Plantable = grown;
    }
}

