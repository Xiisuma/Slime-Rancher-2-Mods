<h1 align="center">Pond Upgrades</h1>

<p align="center">
  <b>Three upgrades for the pond, in the plot shop where they belong.</b>
</p>

---

## Install

1. Install [MelonLoader](https://melonwiki.xyz/) 0.7.x for Slime Rancher 2 and run the game once.
2. Drop `SlimePondUpgradesSR2.dll` into the game's `Mods` folder.

## What it adds

Three pond upgrades, sold from the plot shop and stored in the save like vanilla ones.

## Notes

- Custom `LandPlot.Upgrade` values start at 1500, clear of
  [`MoreCorralUpgrades`](../MoreCorralUpgrades) at 1400 — two mods handing out the same number would
  make one upgrade unlock the other.
- Same live availability delegates as the corral mod, for the same reason.

## Build

Source lives in [`Source/PondUpgrades`](../../Source/PondUpgrades). Point the build at a Slime Rancher 2 install
with MelonLoader on it — `SR2_PATH`, or `-p:GamePath="…\Slime Rancher 2"` — then:

```bash
dotnet build "Source/PondUpgrades/SlimePondUpgradesSR2.csproj" -c Release
```

## Credits

Original Slime Rancher 1 mod: **[Pond Upgrades](https://www.nexusmods.com/slimerancher/mods/281)**, by **Aidanamite**.
Credit for it stays with its author.

Slime Rancher 2 adaptation by **Xiu_ma**, **PikaCat** and **Claude** (Anthropic).
