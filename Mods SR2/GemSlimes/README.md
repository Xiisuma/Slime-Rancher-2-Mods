# Gem Slimes (Slime Rancher 2)

Port of the Slime Rancher 1 mod **GemSlimes** by Baz.

Five gem slimes and their plorts, in progression order:

| Gem | Cut from | Plort | Shatters on touch |
|---|---|---|---|
| Garnet | Crystal slime | 90 (saturates at 70) | yes |
| Sapphire | Rock slime | 125 (80) | no |
| Emerald | Rock slime | 300 (215) | no |
| Amethyst | Crystal slime | 450 (360) | yes |
| Diamond | Crystal slime | 600 (495) | yes |

Garnets and sapphires turn up in the wild. The other three are grown, and each meal is a whole slime,
which is what makes the chain expensive:

- Sapphire + **Lucky Slime** → Emerald
- Emerald + **Garnet Slime** → Amethyst
- Amethyst + **Gold Slime** → Diamond — and if a save somehow has no gold slime, the mod falls back to
  **Gilded Ginger** and says so in the log.

Every gem's favourite food is the mint mango, as in the original mod.

## Install

1. Install [MelonLoader](https://melonwiki.xyz/) 0.7.x for Slime Rancher 2 and run the game once.
2. Drop `GemSlimesSR2.dll` into the game's `Mods` folder.

Optional: [`ModdedAssets`](../ModdedAssets) carries the original mod's icons and hands them to the gems.

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

- Each gem is a clone of a vanilla `SlimeDefinition` — which in SR2 *is* the identifiable type —
  registered under `GemSlimes_Slime<Gem>`, the id the save system stores. Plorts follow the same
  pattern under `GemSlimes_Plort<Gem>`.
- The five gems differ only by data, so one builder covers all of them; the SR1 mod repeated the same
  130-line method per gem.
- Appearances are rebuilt rather than edited: cloning a `SlimeAppearance` still points at the vanilla
  materials, so tinting in place would repaint the slime the gem was cut from. The crystal gems also
  get their own tinted copies of the crystal spike prefabs, and `CrystalSlimeLaunch` is pointed at
  them — the spikes are held by the launcher, not read from the appearance, so a clone would
  otherwise throw crystal-slime blue.
- Growth meals are resolved in a prefix on `SlimeEat.FinishChomp`: an eat map entry naming
  `BecomesIdent` is not enough, because the game only takes its transformation branch for meals it
  treats as largo material and sends a swallowed slime to `EatAndProduce` instead.
- `ShatterOnTouch` is an injected `MonoBehaviour` that breaks a crystal gem into its plort when the
  player walks into it. SR1 implemented the game's `ControllerCollisionListener` interface; Il2CppInterop
  emits SR2's version as a class, which an injected behaviour cannot implement, so the component polls
  the distance to the player instead — same trigger, no changes to the game's collision code.
- World spawning appends low-weight members to the spawn sets the game already runs.

## Not ported

- **The garnet's plort.** In SR1 the garnet produced the *mosaic* plort, and mosaic slimes do not
  exist in SR2. It gets its own plort instead, priced at 90 so the chain still ramps.
- **The gold slime step**, replaced by the kookadoba as described above.
- **Pedia entries** (slimeology, risks, plortonomics): SR2 pedia entries are Addressable assets
  referenced by guid, which a runtime-created asset cannot have.
