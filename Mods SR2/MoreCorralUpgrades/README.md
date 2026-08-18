<h1 align="center">More Corral Upgrades</h1>

<p align="center">
  <b>Eight extra upgrades in the corral shop, sold and saved like the vanilla ones.</b>
</p>

---

## Install

1. Install [MelonLoader](https://melonwiki.xyz/) 0.7.x for Slime Rancher 2 and run the game once.
2. Drop `MoreCorralUpgrades.dll` into the game's `Mods` folder.

## What it adds

Eight upgrades appear in the corral's shop panel, priced and gated like the game's own. They persist
across saves, and a bought upgrade shows its check mark straight away.

## Notes

- Custom `LandPlot.Upgrade` values start at 1400. Upgrades live in a `HashSet` serialized as ints, so
  a value the game does not know about survives a save and load untouched.
- Shop entries are clones of vanilla `PlotUpgradePurchaseItemModel` assets added to the plot
  category.
- Availability is evaluated live through delegates. A captured boolean would leave a bought upgrade
  looking unsold until the menu was reopened.

## Build

Source lives in [`Source/MoreCorralUpgrades`](../../Source/MoreCorralUpgrades). Fill the shared
[`Dependencies/`](../../Dependencies/README.md) folder, then:

```bash
dotnet build "Source/MoreCorralUpgrades/MoreCorralUpgrades.csproj" -c Release
```

## Credits

Original Slime Rancher 1 mod: **MoreCorralUpgrades**, author unrecorded — the DLL carries no author metadata. The mod itself is kept in
[`Mods SR1/MoreCorralUpgrades`](../../Mods%20SR1/MoreCorralUpgrades) for reference, and credit for it stays with its author.

Slime Rancher 2 adaptation by **Xiu_ma**, **PikaCat** and **Claude** (Anthropic).
