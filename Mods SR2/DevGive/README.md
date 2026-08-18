<h1 align="center">Dev Give</h1>

<p align="center">
  <b>Hands named items straight to the vacpack, on a key press.</b><br>
  A testing tool for the ported mods.
</p>

---

## Install

1. Install [MelonLoader](https://melonwiki.xyz/) 0.7.x for Slime Rancher 2 and run the game once.
2. Drop `DevGiveSR2.dll` into the game's `Mods` folder.
3. Launch once more — it writes its settings into `UserData/MelonPreferences.cfg`.

## Use

Edit the `[DevGive]` section, load a save, press the key.

```ini
[DevGive]
items = "Gold:1, StonyHen:1"
hotkey = "F7"
```

`items` is a comma-separated list of `id:count`; the count may be left out. An id is matched, in
order, against a full reference id, the tail of one, the asset name, then any reference id containing
it — all case-insensitive. At the main menu the log says what each name resolved to, so a typo shows
up before a key press in a save:

```
[Dev_Give_SR2] F7 gives 1 x SlimeDefinition.Gold.
```

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

## Build

Source lives in [`Source/DevGive`](../../Source/DevGive). Fill the shared
[`Dependencies/`](../../Dependencies/README.md) folder, then:

```bash
dotnet build "Source/DevGive/DevGiveSR2.csproj" -c Release
```

## Credits

Not a port: this testing tool was written by **Xiu_ma**, **PikaCat** and **Claude** (Anthropic) to serve
the Slime Rancher 1 mods adapted alongside it.
