# Dependencies

Place the following DLLs here before building (locally or via GitHub Actions).
They are game files, so they are git-ignored — every developer fills this folder themselves.

## Where to find them

After installing **MelonLoader** on Slime Rancher 2 and launching the game at least once:

| DLL | Source folder | Needed by |
|-----|--------------|-----------|
| `MelonLoader.dll` | `Slime Rancher 2/MelonLoader/net6/` | all |
| `0Harmony.dll` | `Slime Rancher 2/MelonLoader/net6/` | all |
| `Il2CppInterop.Runtime.dll` | `Slime Rancher 2/MelonLoader/net6/` | all |
| `Il2CppInterop.Common.dll` | `Slime Rancher 2/MelonLoader/net6/` | all |
| `Assembly-CSharp.dll` | `Slime Rancher 2/MelonLoader/Il2CppAssemblies/` | all |
| `Il2Cppmscorlib.dll` | `Slime Rancher 2/MelonLoader/Il2CppAssemblies/` | all |
| `Il2CppSystem.dll` | `Slime Rancher 2/MelonLoader/Il2CppAssemblies/` | all |
| `Il2CppSystem.Core.dll` | `Slime Rancher 2/MelonLoader/Il2CppAssemblies/` | all |
| `UnityEngine.dll` | `Slime Rancher 2/MelonLoader/Il2CppAssemblies/` | all |
| `UnityEngine.CoreModule.dll` | `Slime Rancher 2/MelonLoader/Il2CppAssemblies/` | all |
| `UnityEngine.SharedInternalsModule.dll` | `Slime Rancher 2/MelonLoader/Il2CppAssemblies/` | all |
| `UnityEngine.AssetBundleModule.dll` | `Slime Rancher 2/MelonLoader/Il2CppAssemblies/` | PondUpgrades |
| `UnityEngine.TextRenderingModule.dll` | `Slime Rancher 2/MelonLoader/Il2CppAssemblies/` | PondUpgrades |
| `UnityEngine.UI.dll` | `Slime Rancher 2/MelonLoader/Il2CppAssemblies/` | PondUpgrades |
| `Unity.InputSystem.dll` | `Slime Rancher 2/MelonLoader/Il2CppAssemblies/` | PondUpgrades |
| `Unity.Localization.dll` | `Slime Rancher 2/MelonLoader/Il2CppAssemblies/` | MoreCorralUpgrades |
| `UnityEngine.UIElementsModule.dll` | `Slime Rancher 2/MelonLoader/Il2CppAssemblies/` | MoreCorralUpgrades |
| `UnityEngine.PhysicsModule.dll` | `Slime Rancher 2/MelonLoader/Il2CppAssemblies/` | MoreCorralUpgrades |
| `UnityEngine.AnimationModule.dll` | `Slime Rancher 2/MelonLoader/Il2CppAssemblies/` | MoreCorralUpgrades |
| `UnityEngine.AudioModule.dll` | `Slime Rancher 2/MelonLoader/Il2CppAssemblies/` | MoreCorralUpgrades |
| `UnityEngine.ImageConversionModule.dll` | `Slime Rancher 2/MelonLoader/Il2CppAssemblies/` | MoreCorralUpgrades |
| `Unity.Mathematics.dll` | `Slime Rancher 2/MelonLoader/Il2CppAssemblies/` | MoreCorralUpgrades |
| `Unity.Addressables.dll` | `Slime Rancher 2/MelonLoader/Il2CppAssemblies/` | MoreCorralUpgrades |
| `Unity.ResourceManager.dll` | `Slime Rancher 2/MelonLoader/Il2CppAssemblies/` | MoreCorralUpgrades |
| `MelonSRML.dll` | `Slime Rancher 2/Mods/` (install MelonSRML as a mod first) | PondUpgrades |

MelonLoader 0.7.x keeps its own assemblies in `MelonLoader/net6/`; older layouts put them directly in
`MelonLoader/`.

## Quick copy (PowerShell)

Run from the repo root.

```powershell
$game = "C:\Program Files (x86)\Steam\steamapps\common\Slime Rancher 2"
$dest = "Dependencies"

$melon = "MelonLoader.dll", "0Harmony.dll", "Il2CppInterop.Runtime.dll", "Il2CppInterop.Common.dll"
foreach ($dll in $melon) { Copy-Item "$game\MelonLoader\net6\$dll" $dest -Force }

$il2cpp =
    "Assembly-CSharp.dll", "Il2Cppmscorlib.dll", "Il2CppSystem.dll", "Il2CppSystem.Core.dll",
    "UnityEngine.dll", "UnityEngine.CoreModule.dll", "UnityEngine.SharedInternalsModule.dll",
    "UnityEngine.AssetBundleModule.dll", "UnityEngine.TextRenderingModule.dll", "UnityEngine.UI.dll",
    "UnityEngine.UIElementsModule.dll", "UnityEngine.PhysicsModule.dll", "UnityEngine.AnimationModule.dll",
    "UnityEngine.AudioModule.dll", "UnityEngine.ImageConversionModule.dll",
    "Unity.Localization.dll", "Unity.InputSystem.dll", "Unity.Mathematics.dll",
    "Unity.Addressables.dll", "Unity.ResourceManager.dll"
foreach ($dll in $il2cpp) { Copy-Item "$game\MelonLoader\Il2CppAssemblies\$dll" $dest -Force }

Copy-Item "$game\Mods\MelonSRML.dll" $dest -Force   # only if MelonSRML is installed
```
