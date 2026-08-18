<h1 align="center">Lucky Plorts</h1>

<p align="center">
  <img src="img/plortLucky.png" width="110" alt="Lucky Plort">
</p>

<p align="center">
  <b>Lucky slimes leave something behind when they eat a stony hen.</b>
</p>

---

## Install

1. Install [MelonLoader](https://melonwiki.xyz/) 0.7.x for Slime Rancher 2 and run the game once.
2. Drop `LuckyPlortsSR2.dll` into the game's `Mods` folder.

Optional: [`ModdedAssets`](../ModdedAssets) gives the plort its icon.

## What it adds

Feed a **stony hen** to a lucky slime and it produces a **Lucky Plort**: pale stone, off-white on
top, worth **60 newbucks** at the market. The hen becomes the lucky slime's favourite food, so two
plorts come out of a single meal.

## Notes

- The plort is a clone of the pink plort's identifiable type, re-coloured through the kit so it gets
  its own material copies — painting the inherited ones would turn every vanilla pink plort pale.
- The market pays for it and destroys it like a vanilla plort; without the kit's cleanup a modded
  plort stays lying in the machine, sellable again on the next touch.

## Build

Source lives in [`Source/LuckyPlorts`](../../Source/LuckyPlorts). Fill the shared
[`Dependencies/`](../../Dependencies/README.md) folder, then:

```bash
dotnet build "Source/LuckyPlorts/LuckyPlortsSR2.csproj" -c Release
```


## Credits

Original Slime Rancher 1 mod: **[Lucky Slime Plorts](https://www.nexusmods.com/slimerancher/mods/65)**, by **DogeisCut**.
Credit for it stays with its author.

Slime Rancher 2 adaptation by **Xiu_ma**, **PikaCat** and **Claude** (Anthropic).
