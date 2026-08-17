# Modded Assets (Slime Rancher 2)

Gives the ported mods icons of their own.

Every ported mod builds its content by cloning a vanilla asset, so a modded slime or plort wears the
icon of whatever it was cloned from — bubble plorts look like pink plorts in the vacpack, the market
and the silos. This mod carries a picture for each modded type and hands it to whichever ported mod
is installed.

It is one-way on purpose: **no ported mod references this one.** Install it and the icons appear;
leave it out and every mod still works with vanilla icons.

## Install

1. Install [MelonLoader](https://melonwiki.xyz/) 0.7.x for Slime Rancher 2 and run the game once.
2. Drop `ModdedAssetsSR2.dll` into the game's `Mods` folder, next to the ported mods.

## What it covers

| Mod | Assets |
|---|---|
| BubbleSlimes | slime + plort icons |
| LuckyPlorts | plort icon |
| GemSlimes | 5 slime + 5 plort icons |
| TwinkleSlime | 2 slime + 2 plort icons |

## How the artwork gets in

The icons are the set in `assets/`, drawn for this repo. They replaced the Slime Rancher 1 originals,
which came two ways: BubbleSlimes as plain PNG files, GemSlimes and LuckyPlorts inside Unity asset
bundles built for Slime Rancher 1 — which Slime Rancher 2's Unity refuses to open at runtime, so they
were unpacked **offline** with UnityPy. The `.bundle` files are still in `assets/` as the originals.

Everything is embedded as raw RGBA32 (`.rgba`: width, height, then the rows bottom-up), because
`ImageConversion.LoadImage` takes an `Il2CppSystem.ReadOnlySpan` that Il2CppInterop cannot marshal —
calling it throws at runtime. The `.png` files are kept alongside as the readable originals; only the
`.rgba` ones are compiled into the DLL, downscaled to 256 pixels. At full size the raw pixels made
the DLL 32 MB, and the UI never shows an icon larger than a slot.

If an asset is ever missing or unreadable, the mod photographs the object instead: an isolated camera
renders the prefab's meshes to a sprite, so a modded type never silently wears the icon of the vanilla
one it was cloned from. `MelonLoader/Latest.log` reports the split, for example
`Applied 12 icons, rendered 0`.

The assets are extracted from the original SR1 mod DLLs, which are kept in this repo under
[`Mods SR1/`](../../Mods%20SR1). Credit stays with their authors.

## Build

Fill the shared [`Dependencies/`](../../Dependencies/README.md) folder, then:

```bash
dotnet build -c Release
```

`assets/*.rgba` and `assets/*.bundle` are embedded into the DLL at build time. To cover another mod,
add its icon as `.rgba` and a line to the `Icons` table in `src/Main.cs`. To convert a PNG:

```python
import struct
from PIL import Image

img = Image.open("icon.png").convert("RGBA")
img.thumbnail((256, 256), Image.LANCZOS)
flipped = img.transpose(Image.FLIP_TOP_BOTTOM)   # Unity uploads rows bottom-up
with open("icon.rgba", "wb") as f:
    f.write(struct.pack("<ii", *flipped.size))
    f.write(flipped.tobytes())
```

## Not covered

- **Bubble Cherry model** — `assets/bubblecherry.bundle` holds the SR1 mesh, but the fruit itself was
  not ported (see the BubbleSlimes README), so nothing consumes it yet.
- **Twinkle Slime's microphone and jukebox bundles** (15 MB together) are left in the SR1 DLL: the
  features that used them need a gadget terminal SR2 has no equivalent for.
- **Pedia entries and shop listings** are not artwork — they resolve through Addressables asset
  references, which a runtime-created asset cannot have.
