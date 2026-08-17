using Il2Cpp;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using UnityEngine;

namespace SR2Kit;

/// <summary>
/// Dresses a modded slime.
///
/// Recolouring the prefab's renderers is not enough: a slime is painted at runtime by its
/// <see cref="SlimeAppearance"/>, which the appearance applicator pushes onto the body. A clone that
/// keeps its template's appearance therefore looks exactly like the slime it was cut from, however
/// its prefab materials were tinted.
/// </summary>
public static class SlimeBuilder
{
    /// <summary>
    /// Builds a tinted copy of the template's default appearance and assigns it to
    /// <paramref name="slime"/>.
    ///
    /// The structure array and every material in it are rebuilt rather than edited: a cloned
    /// appearance still points at the vanilla materials, so tinting in place would repaint the
    /// slime the clone came from.
    /// </summary>
    public static SlimeAppearance BuildAppearance(SlimeDefinition slime, SlimeDefinition template,
        Color top, Color middle, Color bottom, string name)
    {
        SlimeAppearance source = template.AppearancesDefault != null && template.AppearancesDefault.Length > 0
            ? template.AppearancesDefault[0]
            : null;
        if (source == null) return null;

        SlimeAppearance appearance = Object.Instantiate(source);
        appearance.hideFlags = HideFlags.HideAndDontSave;
        appearance.name = name;

        Il2CppReferenceArray<SlimeAppearanceStructure> structures = source.Structures;
        if (structures != null)
        {
            Il2CppReferenceArray<SlimeAppearanceStructure> tinted = new(structures.Length);
            for (int i = 0; i < structures.Length; i++)
            {
                tinted[i] = new SlimeAppearanceStructure(structures[i])
                {
                    DefaultMaterials = TintedCopy(structures[i].DefaultMaterials, top, middle, bottom)
                };
            }
            appearance.Structures = tinted;
        }

        // The writable side of these lives under the "New"/"GetSet" names; the plain properties are
        // read-only accessors over the same serialized fields.
        appearance.ColorPaletteNew = new SlimeAppearance.Palette
        {
            Top = top,
            Middle = middle,
            Bottom = bottom,
            Ammo = middle
        };
        appearance.SplatColorGetSet = middle;

        Il2CppReferenceArray<SlimeAppearance> appearances = new(1);
        appearances[0] = appearance;
        slime.AppearancesDefault = appearances;
        slime.AppearancesDynamic = new Il2CppSystem.Collections.Generic.List<SlimeAppearance>();

        return appearance;
    }

    /// <summary>
    /// Points a cloned prefab's slime components at the modded definition. Without this the copy
    /// keeps eating, producing and looking like its template — a twinkle slime cut from a pink one
    /// drops pink plorts.
    /// </summary>
    public static void RetargetPrefab(GameObject prefab, SlimeDefinition slime, SlimeAppearance appearance)
    {
        if (prefab == null || slime == null) return;

        foreach (SlimeAppearanceApplicator applicator in prefab.GetComponentsInChildren<SlimeAppearanceApplicator>(true))
        {
            applicator.SlimeDefinition = slime;
            if (appearance != null) applicator.Appearance = appearance;
        }

        foreach (SlimeEat eat in prefab.GetComponentsInChildren<SlimeEat>(true))
            eat.SlimeDefinition = slime;

        foreach (PlayWithToys toys in prefab.GetComponentsInChildren<PlayWithToys>(true))
            toys.SlimeDefinition = slime;

        foreach (ReactToToyNearby reaction in prefab.GetComponentsInChildren<ReactToToyNearby>(true))
            reaction.SlimeDefinition = slime;
    }

    private static Il2CppReferenceArray<Material> TintedCopy(Il2CppReferenceArray<Material> source,
        Color top, Color middle, Color bottom)
    {
        if (source == null) return null;

        Il2CppReferenceArray<Material> tinted = new(source.Length);
        for (int i = 0; i < source.Length; i++)
        {
            if (source[i] == null) continue;

            Material copy = Object.Instantiate(source[i]);
            copy.hideFlags = HideFlags.HideAndDontSave;
            copy.name = source[i].name + " (modded)";
            Recolor.Tint(copy, top, middle, bottom);
            tinted[i] = copy;
        }
        return tinted;
    }
}
