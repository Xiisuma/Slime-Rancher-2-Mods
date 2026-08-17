using System.Collections.Generic;
using Il2Cpp;
using MelonLoader;
using ModdedAssets;
using SR2Kit;
using UnityEngine;

[assembly: MelonInfo(typeof(ModdedAssets.Main), "Modded Assets SR2", "1.0.0", "Xiisuma")]
[assembly: MelonGame("MonomiPark", "SlimeRancher2")]

namespace ModdedAssets;

/// <summary>
/// Gives the ported mods their original artwork back.
///
/// Every ported mod creates its content by cloning a vanilla asset, so a modded slime or plort ends
/// up wearing the icon of whatever it was cloned from  every bubble plort looks like a pink plort
/// in the vacpack, the market and the silos. The Slime Rancher 1 mods shipped their own icons inside
/// their DLLs; this mod carries those files and hands them to whichever ported mod is installed.
///
/// It is deliberately one-way: no ported mod references this one. Install it and the icons appear;
/// leave it out and every mod still works with vanilla icons.
/// </summary>
public class Main : MelonMod
{
    /// <summary>Reference id of a modded type, and the asset file that should illustrate it.</summary>
    private static readonly Dictionary<string, string> Icons = new()
    {
        // BubbleSlimes: PNG files in the original mod.
        ["BubbleSlimes_SlimeBubble"] = "slimeBubble.rgba",
        ["BubbleSlimes_PlortBubble"] = "plortBubble.rgba",

        // LuckyPlorts and GemSlimes: unpacked from the Slime Rancher 1 asset bundles they shipped in.
        ["LuckyPlorts_PlortLucky"] = "plortLucky.rgba",
        ["GemSlimes_SlimeAmethyst"] = "slimeAmethyst.rgba",
        ["GemSlimes_SlimeDiamond"] = "slimeDiamond.rgba",
        ["GemSlimes_SlimeEmerald"] = "slimeEmerald.rgba",
        ["GemSlimes_SlimeGarnet"] = "slimeGarnet.rgba",
        ["GemSlimes_SlimeSapphire"] = "slimeSapphire.rgba",
        ["GemSlimes_PlortAmethyst"] = "plortAmethyst.rgba",
        ["GemSlimes_PlortDiamond"] = "plortDiamond.rgba",
        ["GemSlimes_PlortEmerald"] = "plortEmerald.rgba",
        ["GemSlimes_PlortSapphire"] = "plortSapphire.rgba"
    };

    public static Main Instance { get; private set; }
    public static MelonLogger.Instance Log => Instance.LoggerInstance;

    public override void OnInitializeMelon()
    {
        Instance = this;
        Hooks.OnLookupDirectorReady(ApplyIcons);
    }

    /// <summary>
    /// Runs after every mod has registered its content, since MelonLoader fires the lookup callbacks
    /// in the order the mods subscribed and this one only reads what the others created.
    /// </summary>
    private void ApplyIcons(LookupDirector director)
    {
        int applied = 0;
        int rendered = 0;
        int absent = 0;

        foreach (KeyValuePair<string, string> entry in Icons)
        {
            if (!director.TryFindIdentifiableTypeByReferenceId(entry.Key, out IdentifiableType type))
            {
                absent++;
                continue;
            }

            Sprite sprite = AssetLibrary.Load(entry.Value);
            if (sprite != null)
            {
                type.icon = sprite;
                applied++;
                continue;
            }

            // The original art could not be opened; photograph the object instead of leaving it
            // wearing the icon of the vanilla type it was cloned from.
            sprite = IconRenderer.Render(type.prefab);
            if (sprite == null) continue;

            type.icon = sprite;
            rendered++;
        }

        Log.Msg($"Applied {applied} icons, rendered {rendered} " +
                $"({absent} modded types not installed).");
    }
}

