using System;
using System.Collections.Generic;
using DevGive;
using Il2Cpp;
using Il2CppMonomiPark.SlimeRancher.Player;
using MelonLoader;
using SR2Kit;
using UnityEngine.InputSystem;

[assembly: MelonInfo(typeof(DevGive.Main), "Dev Give SR2", "1.0.0", "Xiisuma")]
[assembly: MelonGame("MonomiPark", "SlimeRancher2")]

namespace DevGive;

/// <summary>
/// Puts named items straight into the vacpack, on a key press.
///
/// A testing tool: the ported mods add slimes that are meant to be rare or grown, and checking one
/// of them otherwise means hours of ranching. It depends on nothing but the game, so it keeps
/// working when a console mod does not.
/// </summary>
public class Main : MelonMod
{
    private const string DefaultItems = "GemSlimes_SlimeSapphire:1, Lucky:1";

    private MelonPreferences_Entry<string> _items;
    private MelonPreferences_Entry<string> _hotkey;

    public static Main Instance { get; private set; }
    public static MelonLogger.Instance Log => Instance.LoggerInstance;

    public override void OnInitializeMelon()
    {
        Instance = this;

        MelonPreferences_Category category = MelonPreferences.CreateCategory("DevGive", "Dev Give");
        _items = category.CreateEntry("items", DefaultItems,
            description: "What the hotkey hands over: a comma-separated list of id:count. " +
                         "An id is a reference id (GemSlimes_SlimeSapphire), the tail of one (Lucky) " +
                         "or an asset name.");
        _hotkey = category.CreateEntry("hotkey", "F7",
            description: "Key that hands the items over. Names come from Unity's input system: " +
                         "F1..F12, digit1, numpad1, backquote...");
    }

    public override void OnUpdate()
    {
        if (!Pressed()) return;

        AmmoSlotManager ammo = Vacpack();
        if (ammo == null)
        {
            Log.Warning("No vacpack yet — load a save first.");
            return;
        }

        foreach (string request in _items.Value.Split(','))
            GiveOne(ammo, request.Trim());
    }

    // ---------------------------------------------------------------- Giving

    private void GiveOne(AmmoSlotManager ammo, string request)
    {
        if (string.IsNullOrEmpty(request)) return;

        string name = request;
        int count = 1;

        int separator = request.LastIndexOf(':');
        if (separator > 0)
        {
            name = request.Substring(0, separator).Trim();
            if (!int.TryParse(request.Substring(separator + 1).Trim(), out count) || count < 1) count = 1;
        }

        IdentifiableType type = Resolve(name);
        if (type == null)
        {
            Log.Warning($"Nothing named '{name}'. Closest reference ids: {string.Join(", ", Nearest(name))}");
            return;
        }

        int given = 0;
        for (int i = 0; i < count; i++)
        {
            if (!ammo.MaybeAddToAnySlot(new AmmoSlot.AmmoMetadata(type))) break;
            given++;
        }

        if (given == count) Log.Msg($"Gave {given} x {type.referenceId}.");
        else if (given > 0) Log.Warning($"Gave {given} x {type.referenceId} of {count}; the vacpack took no more.");
        else Log.Warning($"The vacpack refuses {type.referenceId}: no free slot that accepts it.");
    }

    private static AmmoSlotManager Vacpack()
    {
        SceneContext context = SceneContext.Instance;
        if (context == null) return null;

        PlayerState state = context.PlayerState;
        return state == null ? null : state.Ammo;
    }

    // ---------------------------------------------------------------- Names

    /// <summary>
    /// Finds an identifiable from what a person would type. Reference ids read
    /// <c>SlimeDefinition.Lucky</c> or <c>IdentifiableType.StonyHen</c>, so the tail on its own is
    /// accepted too — and the modded types, whose reference id is their whole name.
    /// </summary>
    private static IdentifiableType Resolve(string name)
    {
        IdentifiableType partial = null;

        foreach (IdentifiableType type in Lookup.IdentifiableTypes)
        {
            string refId = type.ReferenceId;
            if (string.IsNullOrEmpty(refId)) continue;

            if (Same(refId, name) || Same(type.name, name) || Same(Tail(refId), name)) return type;
            if (partial == null && refId.ToLowerInvariant().Contains(name.ToLowerInvariant())) partial = type;
        }
        return partial;
    }

    /// <summary>A few reference ids sharing a word with what was typed, to put in the log.</summary>
    private static List<string> Nearest(string name)
    {
        List<string> found = new();
        string wanted = name.ToLowerInvariant();
        string start = wanted.Length > 3 ? wanted.Substring(0, 3) : wanted;

        foreach (IdentifiableType type in Lookup.IdentifiableTypes)
        {
            string refId = type.ReferenceId;
            if (string.IsNullOrEmpty(refId) || !refId.ToLowerInvariant().Contains(start)) continue;

            found.Add(refId);
            if (found.Count == 8) break;
        }
        return found;
    }

    private static string Tail(string referenceId)
    {
        int dot = referenceId.LastIndexOf('.');
        return dot < 0 ? referenceId : referenceId.Substring(dot + 1);
    }

    private static bool Same(string left, string right)
        => left != null && string.Equals(left, right, StringComparison.OrdinalIgnoreCase);

    // ---------------------------------------------------------------- Input

    /// <summary>
    /// Reads the key from Unity's input system: Slime Rancher 2 ships without the legacy input
    /// manager, so <c>UnityEngine.Input</c> throws instead of answering.
    /// </summary>
    private bool Pressed()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null) return false;

        if (!Enum.TryParse(_hotkey.Value, ignoreCase: true, out Key key) || key == Key.None) return false;
        return keyboard[key].wasPressedThisFrame;
    }
}
