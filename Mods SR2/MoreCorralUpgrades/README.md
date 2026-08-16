# MoreCorralUpgrades (Slime Rancher 2)

Port of the Slime Rancher 1 mod **MoreCorralUpgrades** to Slime Rancher 2.
Adds eight extra upgrades to the corral purchase menu.

| Upgrade | Cost | Effect |
|---|---|---|
| Air Net Upgrade | 700 | The air net takes twice as many hits before breaking. Requires the Air Net. |
| Plort Protector | 1000 | Slimes in the corral cannot eat plorts while the battery holds a charge. Refill it with water. |
| Battery Upgrade | 1250 | Triples the Plort Protector battery capacity. Requires the Plort Protector. |
| Internal Garden | 400 | Adds a planting bed inside the corral. |
| Increase Storage Capacity | 600 | Triples the capacity of every storage on the corral. |
| Miniturizer | 1500 | Shrinks slimes, plorts, toys and food inside the corral. |
| Slime Sprinkler | 750 | Sprinkles the slimes in the corral with water every 5 seconds. |
| Clear Crops | 20 | Removes the crop from the Internal Garden. Requires the Internal Garden. |

## Install

1. Install [MelonLoader](https://melonwiki.xyz/) 0.7.x for Slime Rancher 2 and run the game once so it
   generates `MelonLoader/Il2CppAssemblies`.
2. Drop `MoreCorralUpgrades.dll` into the game's `Mods` folder.

## Settings

`UserData/MelonPreferences.cfg`, category `MoreCorralUpgrades`:

- `MiniturizerSize` (default `0.5`) — size multiplier applied inside a Miniturizer corral.
- `MiniturizerAnimationTime` (default `1`) — seconds the shrink/grow animation takes.

## Build

References are taken from the shared `Dependencies/` folder at the repo root — fill it first, see
[Dependencies/README.md](../../Dependencies/README.md).

```bash
dotnet build -c Release
```

If `Dependencies/` is empty, the build falls back to a local install:

```bash
dotnet build -c Release -p:GamePath="C:\Program Files (x86)\Steam\steamapps\common\Slime Rancher 2"
```

`GamePath` (or the `SR2_PATH` environment variable) must point at the folder containing
`SlimeRancher2.exe`. When that install is found, the build copies the DLL into its `Mods` folder;
pass `-p:DeployToGame=false` to skip that.

## How the port works

Slime Rancher 1 used SRML on a Mono build; Slime Rancher 2 is Il2Cpp and uses MelonLoader, so the
mod was rewritten rather than recompiled:

- **Custom upgrade values** — `LandPlot.Upgrade` stops at 20, but upgrades are stored in a hash set
  and persisted as plain ints, so values ≥ 1400 round-trip through saves untouched.
- **Shop entries** — each vanilla entry is a `PlotUpgradePurchaseItemModel` asset listed in a
  `PlotPurchaseCategory`. The mod clones one and swaps its upgrade value, cost, icon and strings;
  purchase, pricing and persistence then work exactly like a vanilla upgrade.
- **Strings** — added to the string table the vanilla upgrade entries already use (English/French).
- **Behaviour** — instead of a `PlotUpgrader` baked into the prefab, one `CorralUpgradeHandler`
  component is attached to each corral at runtime and reads the plot's upgrade set.
- **Actor tracking** — the corral already owns a `TrackContainedIdentifiables` that knows everything
  inside its walls, so the Miniturizer and the Plort Protector use it instead of extra trigger
  volumes.

## Known limitations

- The Plort Protector battery reuses the plot model's `attachedDeathTime`, the same field the
  Internal Garden crop uses. Using both upgrades on the same corral makes them share that timer.
- The Internal Garden is built by cloning the garden plot prefab; its placement inside the corral is
  a first pass and may need tuning.
