# SR2Kit — shared modding helpers

The bits of a modding framework the SR2 mods in this repo actually need, kept as **source** rather
than a DLL: each mod compiles the files into its own assembly, so a mod stays a single drop-in DLL
with no extra install step and no shared-library version to keep in sync.

Written because [MelonSRML](https://github.com/SlimeRancherModding/MelonSRML) — the usual SR2 modding
API — has not been updated since July 2024, ships no release, and no longer compiles against Slime
Rancher 2 1.2.3 with MelonLoader 0.7.3.

| File | Replaces | What it does |
|---|---|---|
| `Hooks.cs` | `SRMLMelonMod.PreRegister` / `OnSceneContext` | Callbacks on `LookupDirector.InstanceReady` and `SceneContext.onSceneContextLoaded`. |
| `IdentifiableRegistry.cs` | `[IdentifiableTypeHolder]` | Clones a vanilla `IdentifiableType`, gives it a new reference id, registers it in the lookup director and in every group the template belongs to. Also registers slime definitions. |
| `PrefabHost.cs` | `EntryPoint.prefabParent` | Inactive `DontDestroyOnLoad` parent for prefabs created at runtime. |
| `Translations.cs` | `TranslationPatcher` | Adds entries to the game's localization tables, retrying while they load. |
| `Lookup.cs` | `SRLookup` | Finds identifiable types, slime definitions and assets. |
| `PlortEconomy.cs` | — | Registers a modded plort in the market: price, saturation and terminal listing. |

## Why cloning works

A new identifiable type is a clone of a vanilla one, so it inherits the categories, group rules and
every serialized field the game expects; only the identity is swapped (reference id, name, prefab,
colours). Registration goes through `LookupDirector.AddIdentifiableTypeToGroup` and the
reference-id dictionary — the same dictionary the save system resolves ids against, which is what
makes modded actors survive a save/load.

A `SlimeDefinition` *is* an `IdentifiableType`, so cloning one yields the slime's data and its
identity in a single asset; it then needs `AddSlimeDefinition` on top.

## Use it

```xml
<Compile Include="..\..\Shared\SR2Kit\**\*.cs" LinkBase="SR2Kit" />
```

```csharp
public override void OnInitializeMelon()
{
    Hooks.OnLookupDirectorReady(director => { /* create types and prefabs */ });
    Hooks.OnSceneContextReady(context => { /* per-save setup */ });
}
```
