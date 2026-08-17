# Dev Give (SR2)

Hands named items straight to the vacpack on a key press. A testing tool for the ported mods: gem
slimes are rare or grown, and checking one of them otherwise costs hours of ranching.

It talks to nothing but the game, so it keeps working when a console mod does not.

## Use

1. Drop `DevGiveSR2.dll` in `Mods`.
2. Launch the game once — it writes its settings into `UserData/MelonPreferences.cfg`.
3. Edit the `[DevGive]` section, load a save, press the key.

```ini
[DevGive]
items = "Gold:1, StonyHen:1"
hotkey = "F7"
```

`items` is a comma-separated list of `id:count`. The count may be left out.

An id is matched, in order, against:

- a full reference id — `GemSlimes_SlimeSapphire`, `SlimeDefinition.Lucky`
- the tail of one — `Lucky`, `Gold`, `StonyHen`
- the asset name
- any reference id containing it

Names are case-insensitive. When nothing matches, the log lists the closest reference ids.

`hotkey` takes a key name from Unity's input system: `F1`..`F12`, `digit1`, `numpad1`, `backquote`.
Slime Rancher 2 ships without the legacy input manager, which is why the names are these and not
Unity's old `KeyCode` ones.

## Handy ids

| Item | Id |
|---|---|
| Lucky slime | `Lucky` |
| Gold slime | `Gold` |
| Stony hen | `StonyHen` |
| Gilded ginger | `GingerVeggie` |
| Gem slimes | `GemSlimes_SlimeGarnet`, `…Sapphire`, `…Emerald`, `…Amethyst`, `…Diamond` |
| Gem plorts | `GemSlimes_PlortGarnet`, `…Sapphire`, `…Emerald`, `…Amethyst`, `…Diamond` |
| Bubble slime | `BubbleSlimes_SlimeBubble` |
| Twinkle slimes | `TwinkleSlime_SlimeTwinkle`, `TwinkleSlime_SlimeLumina` |

## Limits

The vacpack decides what it accepts: an item with no slot willing to take it is refused, and the log
says so. Nothing is spawned in the world.
