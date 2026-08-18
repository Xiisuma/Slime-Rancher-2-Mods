using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using UnityEngine;

namespace ModdedAssets;

/// <summary>
/// Loads the artwork embedded in this mod.
///
/// Two formats live side by side, because that is how the Slime Rancher 1 mods shipped them: plain
/// PNG files, and Unity asset bundles built by the original authors. PNGs always work. Bundles were
/// built with Slime Rancher 1's Unity version and may be rejected by Slime Rancher 2's — when that
/// happens the mod says so and moves on instead of failing silently.
/// </summary>
public static class AssetLibrary
{
    /// <summary>Width and height, four bytes each, ahead of the pixels.</summary>
    private const int HeaderSize = 8;

    private static readonly Assembly Self = Assembly.GetExecutingAssembly();
    /// <summary>Bundle buffers kept alive for as long as the bundles read from them.</summary>
    private static readonly List<Il2CppStructArray<byte>> Pinned = new();

    private static readonly Dictionary<string, Sprite> Cache = new();
    private static readonly HashSet<string> Rejected = new();

    /// <summary>Loads a sprite by asset file name, for example "slimeBubble.png".</summary>
    public static Sprite Load(string fileName)
    {
        if (Cache.TryGetValue(fileName, out Sprite cached)) return cached;
        if (Rejected.Contains(fileName)) return null;

        byte[] bytes = Read(fileName);
        if (bytes == null)
        {
            Main.Log.Warning($"Asset {fileName} is missing from the mod.");
            Rejected.Add(fileName);
            return null;
        }

        Sprite sprite = fileName.EndsWith(".rgba", StringComparison.OrdinalIgnoreCase)
            ? FromRawPixels(bytes)
            : FromBundle(fileName, bytes);

        if (sprite == null)
        {
            Rejected.Add(fileName);
            return null;
        }

        sprite.hideFlags = HideFlags.HideAndDontSave;
        Cache[fileName] = sprite;
        return sprite;
    }

    private static byte[] Read(string fileName)
    {
        // Embedded resources are named "<default namespace>.<folder>.<file>".
        using Stream stream = Self.GetManifestResourceStream($"ModdedAssets.assets.{fileName}");
        if (stream == null) return null;

        byte[] bytes = new byte[stream.Length];
        int read = 0;
        while (read < bytes.Length)
        {
            int chunk = stream.Read(bytes, read, bytes.Length - read);
            if (chunk <= 0) break;
            read += chunk;
        }
        return bytes;
    }

    /// <summary>
    /// Builds a texture from raw RGBA32 pixels: an eight-byte header (width, height) followed by the
    /// rows, bottom-up, the order Unity expects.
    ///
    /// The PNGs are converted to this format when they are added to the mod rather than decoded here,
    /// because <c>ImageConversion.LoadImage</c> takes an <c>Il2CppSystem.ReadOnlySpan</c> that
    /// Il2CppInterop cannot marshal — calling it throws a missing-method exception at runtime.
    /// </summary>
    private static Sprite FromRawPixels(byte[] bytes)
    {
        if (bytes.Length < HeaderSize) return null;

        int width = BitConverter.ToInt32(bytes, 0);
        int height = BitConverter.ToInt32(bytes, 4);
        int expected = width * height * 4;
        if (width <= 0 || height <= 0 || bytes.Length - HeaderSize != expected) return null;

        byte[] pixels = new byte[expected];
        Buffer.BlockCopy(bytes, HeaderSize, pixels, 0, expected);

        Texture2D texture = new(width, height, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Bilinear,
            hideFlags = HideFlags.HideAndDontSave
        };
        texture.LoadRawTextureData(new Il2CppStructArray<byte>(pixels));
        texture.Apply(false, false);

        return Sprite.Create(texture, new Rect(0f, 0f, width, height), new Vector2(0.5f, 0.5f));
    }

    /// <summary>Pulls the first sprite (or texture) out of a Slime Rancher 1 asset bundle.</summary>
    private static Sprite FromBundle(string fileName, byte[] bytes)
    {
        AssetBundle bundle;
        try
        {
            // The array has to outlive the call: a temporary is collected in the Il2Cpp domain while
            // the bundle is still reading from it, which surfaces as a garbage-collection error.
            Il2CppStructArray<byte> pinned = new(bytes);
            Pinned.Add(pinned);
            bundle = AssetBundle.LoadFromMemory(pinned);
        }
        catch (Exception e)
        {
            Main.Log.Warning($"{fileName} is a Slime Rancher 1 asset bundle this Unity version refuses to open ({e.Message}).");
            return null;
        }

        if (bundle == null)
        {
            Main.Log.Warning($"{fileName} is a Slime Rancher 1 asset bundle this Unity version refuses to open.");
            return null;
        }

        foreach (UnityEngine.Object asset in bundle.LoadAllAssets())
        {
            Sprite sprite = asset.TryCast<Sprite>();
            if (sprite != null) return sprite;

            Texture2D texture = asset.TryCast<Texture2D>();
            if (texture == null) continue;
            return Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f));
        }

        Main.Log.Warning($"{fileName} opened but holds no sprite or texture.");
        return null;
    }
}
