<h1 align="center">Slime Rancher 2 Mods</h1>

<p align="center">
  <img src="Mods%20SR2/GemSlimes/img/slimeGarnet.png" width="76" alt="Garnet Slime">
  <img src="Mods%20SR2/TwinkleSlime/img/slimeLumina.png" width="76" alt="Lumina Slime">
  <img src="Mods%20SR2/BubbleSlimes/img/slimeBubble.png" width="76" alt="Bubble Slime">
  <img src="Mods%20SR2/KookadobaGingerGrower/img/ginger.png" width="76" alt="Gilded Ginger">
</p>

<p align="center">
  <b>Slime Rancher 1 mods, ported to Slime Rancher 2.</b><br>
  MelonLoader 0.7.x · Slime Rancher 2 v1.2.3 · no modding framework required
</p>

---

## Install a mod

1. Install [MelonLoader](https://melonwiki.xyz/) 0.7.x for Slime Rancher 2 and run the game once.
2. Copy the `.dll` from a folder below into the game's `Mods` folder.

That is the whole install. Nothing else to copy, no framework underneath.

## The mods

| Mod | What it adds |
|---|---|
| [Gem Slimes](Mods%20SR2/GemSlimes) | Five gem slimes, their plorts, and a chain that grows one into the next |
| [Twinkle Slime](Mods%20SR2/TwinkleSlime) | The twinkle slime and its rare lumina variant, with plorts |
| [Bubble Slimes](Mods%20SR2/BubbleSlimes) | A slime that pops when you walk into it |
| [Lucky Plorts](Mods%20SR2/LuckyPlorts) | Lucky slimes turn stony hens into a plort worth 60 |
| [Kookadoba Ginger Grower](Mods%20SR2/KookadobaGingerGrower) | Gilded Ginger becomes a crop: plantable, carryable, growing in the wild |
| [More Corral Upgrades](Mods%20SR2/MoreCorralUpgrades) | Eight extra corral upgrades in the shop |
| [Pond Upgrades](Mods%20SR2/PondUpgrades) | Three pond upgrades in the shop |
| [Modded Assets](Mods%20SR2/ModdedAssets) | Icons for everything above — optional, one-way |
| [Dev Give](Mods%20SR2/DevGive) | Testing tool: hands named items to the vacpack on a key press |

## Repository

```
Mods SR2/<Mod>/      the .dll to install, and what it does
Source/<Mod>/        the code that builds it
Shared/SR2Kit/       helper sources compiled into every mod
Dependencies/        game and loader assemblies (git-ignored)
Mods SR1/            the original Slime Rancher 1 mods, for reference
Reference/           third-party Slime Rancher 2 mods used while testing
```

## Building

Fill [`Dependencies/`](Dependencies/README.md), then build any project:

```bash
dotnet build "Source/GemSlimes/GemSlimesSR2.csproj" -c Release
```

`-p:GamePath="…\Slime Rancher 2"` points at a local install and copies the result straight into its
`Mods` folder; `-p:DeployToGame=false` skips that copy.

There is no shared framework DLL. [`Shared/SR2Kit`](Shared/README.md) is compiled **into** each mod,
so every one stays a single drop-in file: MelonSRML, which the usual SR1-to-SR2 conversions rely on,
no longer builds against the current game.

## Credit

Slime Rancher 2 adaptations by **Xiu_ma**, **PikaCat** and **Claude** (Anthropic).

Each page names the Slime Rancher 1 mod it comes from, links to its Nexus Mods page and names its
original author; credit for the originals stays with them.

| Slime Rancher 1 mod | Author | Adapted as |
|---|---|---|
| [Bubble Slimes](https://www.nexusmods.com/slimerancher/mods/100) | Bazzzzzzzzzzzzzzzzzzzz | [BubbleSlimes](Mods%20SR2/BubbleSlimes) |
| [Gem Slimes](https://www.nexusmods.com/slimerancher/mods/104) | Bazzzzzzzzzzzzzzzzzzzz | [GemSlimes](Mods%20SR2/GemSlimes) |
| [Growable Ginger and Kookadoba](https://www.nexusmods.com/slimerancher/mods/91) | MegaPiggy | [KookadobaGingerGrower](Mods%20SR2/KookadobaGingerGrower) |
| [Lucky Slime Plorts](https://www.nexusmods.com/slimerancher/mods/65) | DogeisCut | [LuckyPlorts](Mods%20SR2/LuckyPlorts) |
| [More Corral Upgrades](https://www.nexusmods.com/slimerancher/mods/292) | Aidanamite | [MoreCorralUpgrades](Mods%20SR2/MoreCorralUpgrades) |
| [Pond Upgrades](https://www.nexusmods.com/slimerancher/mods/281) | Aidanamite | [PondUpgrades](Mods%20SR2/PondUpgrades) |
| [Twinkle Slimes](https://www.nexusmods.com/slimerancher/mods/88) | MegaPiggy | [TwinkleSlime](Mods%20SR2/TwinkleSlime) |
| [Assets Lib](https://www.nexusmods.com/slimerancher/mods/341) | Aidanamite | stood where [ModdedAssets](Mods%20SR2/ModdedAssets) stands, not derived from it |
