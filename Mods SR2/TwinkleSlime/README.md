<h1 align="center">Twinkle Slime</h1>

<p align="center">
  <img src="img/slimeTwinkle.png" width="100" alt="Twinkle Slime">
  <img src="img/slimeLumina.png" width="100" alt="Lumina Slime">
  <img src="img/plortTwinkle.png" width="76" alt="Twinkle Plort">
  <img src="img/plortLumina.png" width="76" alt="Lumina Plort">
</p>

<p align="center">
  <b>A slime that took to the night sky, and its rarer cousin lit from the inside.</b>
</p>

---

## Install

1. Install [MelonLoader](https://melonwiki.xyz/) 0.7.x for Slime Rancher 2 and run the game once.
2. Drop `TwinkleSlimeSR2.dll` into the game's `Mods` folder.

Optional: [`ModdedAssets`](../ModdedAssets) carries the four icons.

## What it adds

| | Rarity | Plort |
|---|---|---|
| **Twinkle Slime** | 2% of a spawn set | Twinkle Plort — 45 newbucks |
| **Lumina Slime** | 0.2% — the secret variant | Lumina Plort — 80 newbucks |

Both keep the pink slime's appetite and take the beach ball as their favourite toy.

## Notes

- Shares of a spawn set rather than absolute weights: the vanilla sets do not agree on a scale, so
  the same number is a rarity in one and the commonest slime in another.
- Appearances are rebuilt rather than tinted in place, which is what keeps vanilla slimes vanilla.

## Not ported

The microphone, jukebox and instruments of the original mod need a gadget terminal Slime Rancher 2
has no equivalent for. Their bundles are left in the SR1 mod.

## Build

Source lives in [`Source/TwinkleSlime`](../../Source/TwinkleSlime). Fill the shared
[`Dependencies/`](../../Dependencies/README.md) folder, then:

```bash
dotnet build "Source/TwinkleSlime/TwinkleSlimeSR2.csproj" -c Release
```

## Credits

Original Slime Rancher 1 mod: **[Twinkle Slimes](https://www.nexusmods.com/slimerancher/mods/88)**, by **MegaPiggy**.
Credit for it stays with its author.

Slime Rancher 2 adaptation by **Xiu_ma**, **PikaCat** and **Claude** (Anthropic).
