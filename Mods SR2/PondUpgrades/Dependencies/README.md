# Dependencies

Place the following DLLs here before building (locally or via GitHub Actions).

## Where to find them

After installing **MelonLoader** on Slime Rancher 2 and launching the game at least once:

| DLL | Source folder |
|-----|--------------|
| `MelonLoader.dll` | `Slime Rancher 2/MelonLoader/` |
| `0Harmony.dll` | `Slime Rancher 2/MelonLoader/` |
| `Il2CppInterop.Runtime.dll` | `Slime Rancher 2/MelonLoader/` |
| `Il2CppInterop.Common.dll` | `Slime Rancher 2/MelonLoader/` |
| `Assembly-CSharp.dll` | `Slime Rancher 2/MelonLoader/Il2CppAssemblies/` |
| `UnityEngine.CoreModule.dll` | `Slime Rancher 2/MelonLoader/Il2CppAssemblies/` |
| `UnityEngine.AssetBundleModule.dll` | `Slime Rancher 2/MelonLoader/Il2CppAssemblies/` |
| `UnityEngine.TextRenderingModule.dll` | `Slime Rancher 2/MelonLoader/Il2CppAssemblies/` |
| `UnityEngine.UI.dll` | `Slime Rancher 2/MelonLoader/Il2CppAssemblies/` |
| `Unity.Localization.dll` | `Slime Rancher 2/MelonLoader/Il2CppAssemblies/` |
| `Unity.InputSystem.dll` | `Slime Rancher 2/MelonLoader/Il2CppAssemblies/` |
| `MelonSRML.dll` | `Slime Rancher 2/Mods/` (install MelonSRML as a mod first) |

## Quick copy (PowerShell)

```powershell
$game = "C:\Program Files (x86)\Steam\steamapps\common\Slime Rancher 2"
$dest = "Mods SR2\PondUpgrades\Dependencies"

Copy-Item "$game\MelonLoader\MelonLoader.dll" $dest
Copy-Item "$game\MelonLoader\0Harmony.dll" $dest
Copy-Item "$game\MelonLoader\Il2CppInterop.Runtime.dll" $dest
Copy-Item "$game\MelonLoader\Il2CppInterop.Common.dll" $dest
Copy-Item "$game\MelonLoader\Il2CppAssemblies\Assembly-CSharp.dll" $dest
Copy-Item "$game\MelonLoader\Il2CppAssemblies\UnityEngine.CoreModule.dll" $dest
Copy-Item "$game\MelonLoader\Il2CppAssemblies\UnityEngine.AssetBundleModule.dll" $dest
Copy-Item "$game\MelonLoader\Il2CppAssemblies\UnityEngine.TextRenderingModule.dll" $dest
Copy-Item "$game\MelonLoader\Il2CppAssemblies\UnityEngine.UI.dll" $dest
Copy-Item "$game\MelonLoader\Il2CppAssemblies\Unity.Localization.dll" $dest
Copy-Item "$game\MelonLoader\Il2CppAssemblies\Unity.InputSystem.dll" $dest
Copy-Item "$game\Mods\MelonSRML.dll" $dest
```
