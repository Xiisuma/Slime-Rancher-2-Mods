# Pond Upgrades (Slime Rancher 2)

Port of the Slime Rancher 1 mod **SlimePondUpgrades** by Aidanamite to Slime Rancher 2.
Adds three upgrades to the pond purchase menu.

| Upgrade | Cost | Effect |
|---|---|---|
| Slime Capacity | 1000 | Doubles the number of slimes the pond can contain. |
| Plort Capacity | 1000 | Doubles the number of plorts the pond can contain. |
| Ancient Blessing | 5000 | Slime and plort capacity ×6 total, and the water turns magic. Requires both upgrades above. |

## Install

1. Install [MelonLoader](https://melonwiki.xyz/) 0.7.x for Slime Rancher 2 and run the game once so it
   generates `MelonLoader/Il2CppAssemblies`.
2. Drop `SlimePondUpgradesSR2.dll` into the game's `Mods` folder.

No modding framework required — the mod talks to MelonLoader and Harmony directly.

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

Pass `-p:DeployToGame=false` to skip copying the DLL into the game's `Mods` folder.

## How the port works

The first SR2 conversion of this mod was written against **MelonSRML**, which has not been updated
since July 2024 and no longer compiles against Slime Rancher 2 1.2.3 with MelonLoader 0.7.3 (`SavedGame`,
`Ammo.Slot` and several MelonLoader APIs are gone). This version drops that dependency:

- **Custom upgrade values** — `LandPlot.Upgrade` stops at 20, but upgrades are stored in a hash set
  and persisted as plain ints, so values ≥ 1500 round-trip through saves untouched.
- **Shop entries** — the pond's own `PlotUpgradePurchaseItemModel` asset is cloned, then its upgrade
  value, cost, icon and strings are swapped. Purchase, pricing and persistence follow the vanilla path.
- **Strings** — added to the string table the vanilla upgrade entries already use (English/French).
- **Availability** — Harmony postfixes on `PlotUpgradePurchaseItemModel.UpdateAvailability` /
  `UpdateHidden` apply the mod's own rules (buy once; Ancient Blessing hidden until the two capacity
  upgrades are owned).
- **Capacity** — postfixes on `SlimeEatWater.CalcMaximumSlimeDensity` / `CalcMaximumPlortDensity`
  multiply the result based on the upgrades of the ponds the slime stands in.
- **Blessed water** — a `PondUpgradeHandler` attached to each pond at runtime swaps the surface
  material for `Depth Magic Water Ball`.
