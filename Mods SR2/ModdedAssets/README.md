# Modded Assets (Slime Rancher 2)

Gives the ported mods their original Slime Rancher 1 artwork back.

Every ported mod builds its content by cloning a vanilla asset, so a modded slime or plort wears the
icon of whatever it was cloned from — bubble plorts look like pink plorts in the vacpack, the market
and the silos. The SR1 mods shipped their own icons inside their DLLs. This mod carries those files
and hands them to whichever ported mod is installed.

It is one-way on purpose: **no ported mod references this one.** Install it and the icons appear;
leave it out and every mod still works with vanilla icons.

## Install

1. Install [MelonLoader](https://melonwiki.xyz/) 0.7.x for Slime Rancher 2 and run the game once.
2. Drop `ModdedAssetsSR2.dll` into the game's `Mods` folder, next to the ported mods.

## What it covers

| Mod | Assets | Format |
|---|---|---|
| BubbleSlimes | slime + plort icons | PNG |
| LuckyPlorts | plort icon | SR1 asset bundle |
| GemSlimes | 5 slime + 4 plort icons | SR1 asset bundle |

PNG files always load. The asset bundles were built with Slime Rancher 1's Unity version, and a newer
Unity may refuse to open them — when that happens the mod logs which file was rejected and leaves the
vanilla icon in place, rather than failing silently. Check `MelonLoader/Latest.log` to see which ones
went through.

The assets are extracted from the original SR1 mod DLLs, which are kept in this repo under
[`Mods SR1/`](../../Mods%20SR1). Credit stays with their authors.

## Build

Fill the shared [`Dependencies/`](../../Dependencies/README.md) folder, then:

```bash
dotnet build -c Release
```

Files in `assets/` are embedded into the DLL at build time; drop a new one in and add a line to the
`Icons` table in `src/Main.cs` to cover another mod.

## Not covered

- **Bubble Cherry model** — `assets/bubblecherry.bundle` holds the SR1 mesh, but the fruit itself was
  not ported (see the BubbleSlimes README), so nothing consumes it yet.
- **Twinkle Slime's microphone and jukebox bundles** (15 MB together) are left in the SR1 DLL: the
  features that used them need a gadget terminal SR2 has no equivalent for.
- **Pedia entries and shop listings** are not artwork — they resolve through Addressables asset
  references, which a runtime-created asset cannot have.
