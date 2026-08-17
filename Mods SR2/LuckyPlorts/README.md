# Lucky Plorts (Slime Rancher 2)

Port of the Slime Rancher 1 mod **Lucky Plorts** by DogeisCut.

Lucky Slimes now eat Stony Hens as a favourite food and produce a pale **Lucky Plort**, worth
60 newbucks at the market.

## Install

1. Install [MelonLoader](https://melonwiki.xyz/) 0.7.x for Slime Rancher 2 and run the game once.
2. Drop `LuckyPlortsSR2.dll` into the game's `Mods` folder.

No modding framework required.

## Build

Fill the shared [`Dependencies/`](../../Dependencies/README.md) folder, then:

```bash
dotnet build -c Release
```

Falls back to a local install with `-p:GamePath="…\Slime Rancher 2"`; `-p:DeployToGame=false` skips
the copy into `Mods`.

## How the port works

The previous SR2 conversion depended on **MelonSRML**, which no longer builds against the current
game and MelonLoader. This version uses the repo's own [SR2Kit](../../Shared/README.md) helpers,
compiled straight into the mod:

- The Lucky Plort is a clone of the pink plort's `IdentifiableType`, re-coloured off-white and registered
  under the reference id `LuckyPlorts_PlortLucky` — which is what the save system stores, so the
  plorts survive a reload.
- The market price is registered on the `PlortEconomyDirector` and in the terminal's plort list.
- The Lucky Slime's diet gets one extra entry: Stony Hen in, Lucky Plort out.
