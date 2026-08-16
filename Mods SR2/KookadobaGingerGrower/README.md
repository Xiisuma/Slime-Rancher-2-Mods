# Kookadoba Ginger Grower (Slime Rancher 2)

Port of the Slime Rancher 1 mod **KookadobaGingerGrower** by MegaPiggy.

**Gilded Ginger becomes a garden crop.** Drop one into a garden and it grows like any other veggie,
deluxe garden included — no more hunting the wild ginger patches every time.

## Install

1. Install [MelonLoader](https://melonwiki.xyz/) 0.7.x for Slime Rancher 2 and run the game once.
2. Drop `KookadobaGingerGrowerSR2.dll` into the game's `Mods` folder.

No modding framework required.

## Build

Fill the shared [`Dependencies/`](../../Dependencies/README.md) folder, then:

```bash
dotnet build -c Release
```

Falls back to a local install with `-p:GamePath="…\Slime Rancher 2"`; `-p:DeployToGame=false` skips
the copy into `Mods`.

## How the port works

Built on the repo's own [SR2Kit](../../Shared/README.md) helpers, compiled straight into the mod
(MelonSRML, which the usual SR1-to-SR2 conversions rely on, no longer builds against the current game):

- A garden accepts a crop when its `GardenCatcher` lists a plant slot for it. The mod clones a
  vanilla veggie patch — keeping the soil, joints, audio and growth logic — and swaps the
  `ResourceGrowerDefinition` driving it for one that yields ginger. The bed's sprouts are re-skinned
  with the ginger model, the same trick the SR1 mod used.
- Gardens instantiated before the crop existed resolve what they accept in `Awake`, so a Harmony
  postfix on `GardenCatcher.Awake` injects the crop into those too.
- **Saving works**: a plot stores what is planted in it as an index into a table of grower reference
  ids (`LandPlotV02.ResourceGrowerId`), built once from the game's grower list. The mod adds its two
  definitions to that list and to both directions of the save translation — the reverse index used
  when writing, and the reference-id lookup used when reading a save back. Without this, planting
  ginger would break the save.

## Not ported

- **The kookadoba half of the mod.** Slime Rancher 1's Kookadoba does not exist in Slime Rancher 2 —
  there is no kookadoba identifiable, patch node or model anywhere in the game's assemblies, only
  `GingerPatchNode`. The mod keeps its original name for lineage, but only the ginger half has
  something to grow here.
- The SR1 mod also placed 9 bushes (normal garden) and 11 (deluxe) at hand-picked offsets. Here the
  cloned vanilla patch keeps the garden's own layout, which is what makes it look native.
