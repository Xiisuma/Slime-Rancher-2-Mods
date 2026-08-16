using Il2Cpp;
using UnityEngine;

namespace ModdedAssets;

/// <summary>
/// Builds an icon by photographing a prefab.
///
/// The Slime Rancher 1 mods shipped their icons as Unity asset bundles built for that game's engine
/// version, and Slime Rancher 2 refuses to open them. Rather than leave those types wearing the icon
/// of whatever they were cloned from, the mod renders the actual object — the same trick the SR1
/// modding library used to generate its own icons.
/// </summary>
public static class IconRenderer
{
    private const int Size = 256;

    /// <summary>Layer nothing else uses, so the shot only catches the subject.</summary>
    private const int IsolationLayer = 31;

    /// <summary>Far below the world, where nothing can wander into frame.</summary>
    private static readonly Vector3 Studio = new(0f, -8000f, 0f);

    /// <summary>Three-quarter view, the angle the game's own icons use.</summary>
    private static readonly Quaternion Angle = Quaternion.Euler(15f, 30f, 0f);

    /// <summary>Renders <paramref name="prefab"/> to a sprite, or null if it has nothing to show.</summary>
    public static Sprite Render(GameObject prefab)
    {
        if (prefab == null) return null;

        GameObject subject = Object.Instantiate(prefab, Studio, Quaternion.identity);
        subject.hideFlags = HideFlags.HideAndDontSave;

        // Deactivate before anything else: an active clone of a slime prefab runs its Awake, which
        // registers an actor with the game's directors and leaves trampoline errors in the log.
        subject.SetActive(false);
        StripBehaviours(subject);
        SetLayer(subject, IsolationLayer);
        subject.SetActive(true);

        Bounds bounds = Frame(subject);
        if (bounds.extents == Vector3.zero)
        {
            Object.DestroyImmediate(subject);
            return null;
        }

        RenderTexture target = new(Size, Size, 16, RenderTextureFormat.ARGB32)
        {
            hideFlags = HideFlags.HideAndDontSave
        };

        GameObject cameraObject = new("SR2Kit_IconCamera");
        cameraObject.hideFlags = HideFlags.HideAndDontSave;
        Camera camera = cameraObject.AddComponent<Camera>();
        camera.orthographic = true;
        camera.orthographicSize = bounds.extents.magnitude * 1.1f;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0f, 0f, 0f, 0f);
        camera.cullingMask = 1 << IsolationLayer;
        camera.targetTexture = target;
        camera.nearClipPlane = 0.01f;
        camera.farClipPlane = 100f;
        cameraObject.transform.rotation = Angle;
        cameraObject.transform.position = bounds.center - Angle * Vector3.forward * 10f;

        camera.Render();

        RenderTexture previous = RenderTexture.active;
        RenderTexture.active = target;

        Texture2D texture = new(Size, Size, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Bilinear,
            hideFlags = HideFlags.HideAndDontSave
        };
        texture.ReadPixels(new Rect(0f, 0f, Size, Size), 0, 0);
        texture.Apply(false, false);

        RenderTexture.active = previous;

        Object.DestroyImmediate(cameraObject);
        Object.DestroyImmediate(subject);
        target.Release();
        Object.DestroyImmediate(target);

        Sprite sprite = Sprite.Create(texture, new Rect(0f, 0f, Size, Size), new Vector2(0.5f, 0.5f));
        sprite.hideFlags = HideFlags.HideAndDontSave;
        return sprite;
    }

    /// <summary>
    /// Strips the gameplay components off the stand-in. A slime prefab wakes up hungry, physical and
    /// registered with the game's directors; the copy only has to hold still and be visible.
    /// </summary>
    private static void StripBehaviours(GameObject subject)
    {
        foreach (Rigidbody body in subject.GetComponentsInChildren<Rigidbody>(true))
            body.isKinematic = true;

        foreach (Collider collider in subject.GetComponentsInChildren<Collider>(true))
            collider.enabled = false;

        foreach (SRBehaviour behaviour in subject.GetComponentsInChildren<SRBehaviour>(true))
            behaviour.enabled = false;
    }

    private static void SetLayer(GameObject subject, int layer)
    {
        subject.layer = layer;
        foreach (Transform child in subject.GetComponentsInChildren<Transform>(true))
            child.gameObject.layer = layer;
    }

    /// <summary>Bounds of everything the camera can see, used to frame the shot.</summary>
    private static Bounds Frame(GameObject subject)
    {
        bool found = false;
        Bounds bounds = new(subject.transform.position, Vector3.zero);

        foreach (Renderer renderer in subject.GetComponentsInChildren<Renderer>(true))
        {
            // Particle and trail renderers describe effects, not the object's silhouette.
            if (!renderer.enabled || renderer.TryCast<MeshRenderer>() == null
                                  && renderer.TryCast<SkinnedMeshRenderer>() == null) continue;

            if (!found)
            {
                bounds = renderer.bounds;
                found = true;
                continue;
            }
            bounds.Encapsulate(renderer.bounds);
        }
        return found ? bounds : new Bounds(subject.transform.position, Vector3.zero);
    }
}
