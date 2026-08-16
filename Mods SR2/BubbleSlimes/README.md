# Bubble Slimes (Slime Rancher 2)

Port of the Slime Rancher 1 mod **Bubble Slimes**.

| Content | Notes |
|---|---|
| Bubble Slime | Pale water-blue slime, cannot become a largo, **pops when the player bumps into it** and leaves a splash of water. |
| Bubble Plort | 210 newbucks, saturates at 189. |

Bubble slimes turn up in the wild as rare members of the spawn sets that already run in the world.

## Install

1. Install [MelonLoader](https://melonwiki.xyz/) 0.7.x for Slime Rancher 2 and run the game once.
2. Drop `BubbleSlimesSR2.dll` into the game's `Mods` folder.

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
(MelonSRML, which the SR1-to-SR2 conversions usually rely on, no longer builds against the current
game):

- The slime is a clone of the pink slime's `SlimeDefinition` — which in SR2 *is* the identifiable
  type — re-coloured and registered under the reference id `BubbleSlimes_SlimeBubble`, the id the
  save system stores.
- The plort is a clone of the pink plort, re-coloured, priced through the `PlortEconomyDirector`.
- `BubblePop` is an injected `MonoBehaviour` on the slime prefab: on collision with the player it
  applies water where the slime stood and destroys the actor, matching the SR1 behaviour (which
  spawned a water puddle).
- World spawning appends a low-weight member to the spawn sets that already contain pink slimes.

## Not ported

- **Bubble Cherry** — the SR1 mod shipped a custom fruit built from an asset bundle (`model_cherry`
  mesh plus ambient-occlusion, mask and normal textures). Those assets are SR1-specific and are not
  in this repo, and a fruit without them would be an invisible reskin of a vanilla one. The slime
  therefore keeps the pink slime's diet instead of favouring cherries.
- **Puddle slime → Bubble Slime transformation**, which in SR1 happened when a puddle slime ate a
  Bubble Cherry. It depends on the cherry above.
- **Pedia entry** with the SR1 slimeology/risks/plortonomics text: SR2 pedia entries are Addressable
  assets referenced by guid, which a runtime-created asset does not have.
- The SR1 eat-map entries that made bubble slimes react to rock and crystal slimes: those ids do not
  map onto SR2's slime roster.
