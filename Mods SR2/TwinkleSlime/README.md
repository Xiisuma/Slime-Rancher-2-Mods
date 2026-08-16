# Twinkle Slime (Slime Rancher 2)

Port of the Slime Rancher 1 **Twinkle Slime** mod.

Adds two ranchable slimes and their plorts:

| Content | Notes |
|---|---|
| Twinkle Slime | Pink/gold/blue palette, cannot become a largo. |
| Twinkle Plort | 45 newbucks. |
| Lumina Slime | Purple variant of the Twinkle Slime. |
| Lumina Plort | 80 newbucks. |

Each slime keeps the food of the slime it was cloned from, but every meal produces its own plort.

## Install

1. Install [MelonLoader](https://melonwiki.xyz/) 0.7.x for Slime Rancher 2 and run the game once.
2. Drop `TwinkleSlimeSR2.dll` into the game's `Mods` folder.

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

- The plorts are clones of the pink plort's `IdentifiableType`, re-coloured and registered under
  their own reference ids (`TwinkleSlime_PlortTwinkle`, `TwinkleSlime_PlortLumina`).
- A `SlimeDefinition` is itself an `IdentifiableType`, so each slime is one cloned asset: it carries
  the slime data and the identity, and is added to `SlimeDefinitions` so the game resolves it.
- Reference ids are what the save system stores, so ranched slimes and their plorts survive a reload.

## Not ported

The SR1 mod also had a microphone toy, a Chime Changer gadget with 13 buyable instruments, and a
date-based world spawner. Those need custom models and a gadget/terminal UI, and are left out.
Twinkle Slimes are obtained from the vanilla ones or spawned with a console command.
