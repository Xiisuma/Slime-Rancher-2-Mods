using HarmonyLib;
using Il2Cpp;

namespace KookadobaGingerGrower;

/// <summary>
/// A garden resolves what it accepts once, in <c>Awake</c>, into two dictionaries. Gardens built
/// from the patched prefab already contain ginger; this catches the ones that were instantiated
/// before the crop existed, and costs nothing for the rest.
/// </summary>
[HarmonyPatch(typeof(GardenCatcher), nameof(GardenCatcher.Awake))]
internal static class Patch_GardenCatcher_Awake
{
    private static void Postfix(GardenCatcher __instance) => GingerCrop.Inject(__instance);
}
