using Il2CppInterop.Runtime.InteropTypes.Arrays;
using UnityEngine;

namespace SR2Kit;

/// <summary>
/// Repaints a cloned prefab without touching the vanilla one.
///
/// A cloned GameObject still points at the *same* material assets as its original, so tinting them
/// in place repaints every vanilla actor using them — a modded sapphire slime turns the game's pink
/// slimes blue. Every renderer therefore gets its own copies, assigned through
/// <c>sharedMaterials</c>: reading <c>materials</c> would create instances Unity manages per
/// renderer instance, which a prefab that is never itself rendered does not get.
/// </summary>
public static class Recolor
{
    /// <summary>Tints every renderer under <paramref name="root"/> with its own material copies.</summary>
    /// <param name="glossy">Gems catch the light; slimes and plorts do not.</param>
    public static void Apply(GameObject root, Color top, Color middle, Color bottom, bool glossy = false)
    {
        if (root == null) return;

        foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
        {
            Il2CppReferenceArray<Material> shared = renderer.sharedMaterials;
            if (shared == null || shared.Length == 0) continue;

            Il2CppReferenceArray<Material> copies = new(shared.Length);
            for (int i = 0; i < shared.Length; i++)
            {
                if (shared[i] == null) continue;

                Material copy = Object.Instantiate(shared[i]);
                copy.hideFlags = HideFlags.HideAndDontSave;
                copy.name = shared[i].name + " (modded)";
                Tint(copy, top, middle, bottom, glossy);
                copies[i] = copy;
            }
            renderer.sharedMaterials = copies;
        }
    }

    /// <summary>Applies the palette to a material the caller already owns.</summary>
    public static void Tint(Material material, Color top, Color middle, Color bottom, bool glossy = false)
    {
        if (material == null) return;

        if (material.HasProperty("_TopColor")) material.SetColor("_TopColor", top);
        if (material.HasProperty("_MiddleColor")) material.SetColor("_MiddleColor", middle);
        if (material.HasProperty("_BottomColor")) material.SetColor("_BottomColor", bottom);
        if (material.HasProperty("_Color")) material.SetColor("_Color", middle);

        if (!glossy) return;
        if (material.HasProperty("_SpecColor")) material.SetColor("_SpecColor", top);
        if (material.HasProperty("_Shininess")) material.SetFloat("_Shininess", 1f);
        if (material.HasProperty("_Gloss")) material.SetFloat("_Gloss", 1f);
    }
}
