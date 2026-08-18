<h1 align="center">Bubble Slimes</h1>

<p align="center">
  <img src="img/slimeBubble.png" width="110" alt="Bubble Slime">
  <img src="img/plortBubble.png" width="90" alt="Bubble Plort">
</p>

<p align="center">
  <b>A slime with the constitution of a soap bubble.</b>
</p>

---

## Install

1. Install [MelonLoader](https://melonwiki.xyz/) 0.7.x for Slime Rancher 2 and run the game once.
2. Drop `BubbleSlimesSR2.dll` into the game's `Mods` folder.

Optional: [`ModdedAssets`](../ModdedAssets) gives the slime and its plort their icons.

## What it adds

| | |
|---|---|
| **Bubble Slime** | Pale water blue, one in fifty of the pink slime's spawns. Pops when a rancher bumps into it, leaving its plort and a splash of water. |
| **Bubble Plort** | 210 newbucks, saturates at 189. |

It cannot be made into a largo — nothing holds.

## Notes

- The pop is an injected `MonoBehaviour` on the slime prefab; the splash reuses the game's own water
  liquid definition.
- Every meal produces a bubble plort: the cloned eat map keeps the pink slime's food and only its
  produce is retargeted.

## Build

Source lives in [`Source/BubbleSlimes`](../../Source/BubbleSlimes). Point the build at a Slime Rancher 2 install
with MelonLoader on it — `SR2_PATH`, or `-p:GamePath="…\Slime Rancher 2"` — then:

```bash
dotnet build "Source/BubbleSlimes/BubbleSlimesSR2.csproj" -c Release
```

## Credits

Original Slime Rancher 1 mod: **[Bubble Slimes](https://www.nexusmods.com/slimerancher/mods/100)**, by **Bazzzzzzzzzzzzzzzzzzzz**.
Credit for it stays with its author.

Slime Rancher 2 adaptation by **Xiu_ma**, **PikaCat** and **Claude** (Anthropic).
