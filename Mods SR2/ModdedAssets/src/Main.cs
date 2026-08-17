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
/// up wearing the icon of whatever it was cloned from: every bubble plort looks like a pink plort
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
        ["GemSlimes_PlortSapphire"] = "plortSapphire.rgba",

        // TwinkleSlime: PNG files in the original mod, except the twinkle slime's own icon — the mod
        // never shipped one because Slime Rancher 1 already had the slime. That one comes straight
        // out of Slime Rancher 1's own assets (iconSlimeTwinkle).
        ["TwinkleSlime_PlortTwinkle"] = "plortTwinkle.rgba",
        ["TwinkleSlime_PlortLumina"] = "plortLumina.rgba",
        ["TwinkleSlime_SlimeLumina"] = "slimeLumina.rgba",
        ["TwinkleSlime_SlimeTwinkle"] = "slimeTwinkle.rgba"
    };

    public static Main Instance { get; private set; }
    public static MelonLogger.Instance Log => Instance.LoggerInstance;

    /// <summary>Reference ids already given their icon, so the second pass only does what is left.</summary>
    private static readonly HashSet<string> Done = new();

    public override void OnInitializeMelon()
    {
        Instance = this;

        // MelonLoader fires the lookup callbacks in the order the mods subscribed, so a mod loaded
        // after this one has not created its content yet when the first pass runs. Whatever is still
        // missing is picked up by OnUpdate.
        Hooks.OnLookupDirectorReady(ApplyIcons);
    }

    /// <summary>Seconds between retries while some modded types are still unregistered.</summary>
    private const float RetryInterval = 2f;

    private float _nextRetry;

    public override void OnUpdate()
    {
        if (Done.Count == Icons.Count) return;
        if (Time.time < _nextRetry) return;
        _nextRetry = Time.time + RetryInterval;

        LookupDirector director = LookupDirector.GetIfReady();
        if (director != null) ApplyIcons(director);
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
            if (Done.Contains(entry.Key)) continue;

            if (!director.TryFindIdentifiableTypeByReferenceId(entry.Key, out IdentifiableType type))
            {
                absent++;
                continue;
            }

            // A null asset means the original mod shipped no artwork for this one.
            Sprite sprite = entry.Value == null ? null : AssetLibrary.Load(entry.Value);
            if (sprite != null)
            {
                Apply(type, sprite);
                Done.Add(entry.Key);
                applied++;
                continue;
            }

            // The original art could not be opened; photograph the object instead of leaving it
            // wearing the icon of the vanilla type it was cloned from.
            sprite = IconRenderer.Render(type.prefab);
            if (sprite == null) continue;

            Apply(type, sprite);
            Done.Add(entry.Key);
            rendered++;
        }

        if (applied == 0 && rendered == 0) return;

        Log.Msg($"Applied {applied} icons, rendered {rendered} " +
                $"({absent} modded types not registered yet).");
    }

    /// <summary>
    /// Sets the icon everywhere the game reads one.
    ///
    /// A <see cref="SlimeDefinition"/> overrides <c>Icon</c> to return the one on its appearance, so
    /// setting the identifiable's own icon alone leaves the slime wearing its template's picture in
    /// the vacpack and the silos.
    /// </summary>
    private static void Apply(IdentifiableType type, Sprite sprite)
    {
        type.icon = sprite;

        SlimeDefinition slime = type.TryCast<SlimeDefinition>();
        if (slime?.AppearancesDefault == null) return;

        foreach (SlimeAppearance appearance in slime.AppearancesDefault)
        {
            if (appearance != null) appearance.IconNew = sprite;
        }
    }
}

