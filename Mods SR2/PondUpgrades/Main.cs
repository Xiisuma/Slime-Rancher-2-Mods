using System;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using Il2Cpp;
using Il2CppInterop.Runtime.Injection;
using Il2CppMonomiPark.SlimeRancher.Pedia;
using MelonLoader;
using MelonSRML;
using MelonSRML.SR2;
using MelonSRML.SR2.Ranch;
using UnityEngine;
using UnityEngine.Localization;

namespace PondUpgrades
{
    /// <summary>
    /// Pond Upgrades for Slime Rancher 2.
    /// Converted from the SR1 mod by Aidanamite to MelonSRML / MelonLoader (IL2CPP).
    /// 
    /// Adds three pond upgrades:
    ///   - Slime Capacity  (x2 slime density)
    ///   - Plort Capacity  (x2 plort density)
    ///   - Ancient Blessing (x6 both, requires the two above; changes water texture)
    /// </summary>
    public class Main : SRMLMelonMod
    {
        internal static Assembly modAssembly = Assembly.GetExecutingAssembly();
        internal static string modName = modAssembly.GetName().Name;

        /// <summary>
        /// Set by Patch_SetCurrentPlot before the MelonSRML UI patch runs,
        /// so the isAvailable / isHidden lambdas can inspect the active plot.
        /// </summary>
        internal static LandPlot currentPlot;

        // Filled in OnGameContext so lambdas can reference them.
        internal static int pondCost = 500;
        internal static Sprite pondIcon;
        internal static Sprite ancientWaterIcon;

        public override void OnInitializeMelon()
        {
            // Register our custom ModdedPlotUpgrader subclass with IL2CPP.
            ClassInjector.RegisterTypeInIl2Cpp<AncientWaterUpgrader>();
        }

        public override void OnGameContext(GameContext context)
        {
            CacheIcons(context);
            RegisterUpgrades(context);
            RegisterUpgraders(context);
        }

        // ------------------------------------------------------------------
        //  Setup helpers
        // ------------------------------------------------------------------

        private void CacheIcons(GameContext context)
        {
            // --- Pond icon + cost ---
            // In SR1 these came from EmptyPlotUI.pond. In SR2 we look for the
            // PlotPatchPurchaseItemModel whose name contains "pond".
            var plotPurchaseItems = Resources.FindObjectsOfTypeAll<PlotPatchPurchaseItemModel>();
            foreach (var item in plotPurchaseItems)
            {
                if (item == null) continue;
                string n = item.name?.ToLowerInvariant() ?? "";
                if (n.Contains("pond"))
                {
                    pondCost = item._purchaseCost?.newbuckCost ?? 500;
                    pondIcon = item._icon;
                    break;
                }
            }

            // --- Ancient water icon ---
            // SR1 used GameContext.Instance.LookupDirector.GetIcon(Identifiable.Id.MAGIC_WATER_LIQUID).
            // In SR2 we search through IdentifiableTypes for the magic-water identifiable.
            foreach (var type in SRLookup.IdentifiableTypes)
            {
                if (type == null || string.IsNullOrEmpty(type.ReferenceId)) continue;
                if (type.ReferenceId.ToLowerInvariant().Contains("magic_water"))
                {
                    ancientWaterIcon = type.Icon;
                    break;
                }
            }
        }

        private void RegisterUpgrades(GameContext context)
        {
            // ---- Slime Capacity (x2) ----
            LandPlotUpgradeRegistry.RegisterPurchasableUpgrade(new LandPlotUpgradeRegistry.UpgradeShopEntry
            {
                cost = pondCost * 2,
                icon = pondIcon,
                upgrade = Ids.POND_SLIME_CAPACITY,
                LandPlotName = "pond",
                pediaEntry = null, // set to a PediaEntry if you want an info page
                isAvailable = () => currentPlot == null || !currentPlot.HasUpgrade(Ids.POND_SLIME_CAPACITY),
                isHidden = () => false,
                NameKey = TranslationPatcher.AddTranslation("UI",
                    "m.upgrade.name.pond.pond_slime_capacity", "Slime Capacity"),
                DescKey = TranslationPatcher.AddTranslation("UI",
                    "m.upgrade.desc.pond.pond_slime_capacity",
                    "Doubles the number of slimes that the pond can contain"),
            }, LandPlot.Id.POND);

            // ---- Plort Capacity (x2) ----
            LandPlotUpgradeRegistry.RegisterPurchasableUpgrade(new LandPlotUpgradeRegistry.UpgradeShopEntry
            {
                cost = pondCost * 2,
                icon = pondIcon,
                upgrade = Ids.POND_PLORT_CAPACITY,
                LandPlotName = "pond",
                pediaEntry = null,
                isAvailable = () => currentPlot == null || !currentPlot.HasUpgrade(Ids.POND_PLORT_CAPACITY),
                isHidden = () => false,
                NameKey = TranslationPatcher.AddTranslation("UI",
                    "m.upgrade.name.pond.pond_plort_capacity", "Plort Capacity"),
                DescKey = TranslationPatcher.AddTranslation("UI",
                    "m.upgrade.desc.pond.pond_plort_capacity",
                    "Doubles the number of plorts that the pond can contain"),
            }, LandPlot.Id.POND);

            // ---- Ancient Blessing (x6 both, requires the two above) ----
            LandPlotUpgradeRegistry.RegisterPurchasableUpgrade(new LandPlotUpgradeRegistry.UpgradeShopEntry
            {
                cost = pondCost * 10,
                icon = ancientWaterIcon ?? pondIcon,
                upgrade = Ids.POND_ANCIENT_BLESSING,
                LandPlotName = "pond",
                pediaEntry = null,
                isAvailable = () => currentPlot == null || !currentPlot.HasUpgrade(Ids.POND_ANCIENT_BLESSING),
                isHidden = () => currentPlot != null &&
                             (!currentPlot.HasUpgrade(Ids.POND_PLORT_CAPACITY) ||
                              !currentPlot.HasUpgrade(Ids.POND_SLIME_CAPACITY)),
                NameKey = TranslationPatcher.AddTranslation("UI",
                    "m.upgrade.name.pond.pond_ancient_blessing", "Ancient Blessing"),
                DescKey = TranslationPatcher.AddTranslation("UI",
                    "m.upgrade.desc.pond.pond_ancient_blessing",
                    "Blesses the water with an ancient power, increasing the slime and plort capacity by 3 times"),
            }, LandPlot.Id.POND);
        }

        private void RegisterUpgraders(GameContext context)
        {
            LandPlotUpgradeRegistry.RegisterPlotUpgrader<AncientWaterUpgrader>(LandPlot.Id.POND);

            // Compatibility with the "SlimePonds" mod if installed.
            if (IsMelonPresent("SlimePonds"))
            {
                try
                {
                    var pondSlimeId = (LandPlot.Id)Enum.Parse(typeof(LandPlot.Id), "POND_SLIME");
                    LandPlotUpgradeRegistry.RegisterPlotUpgrader<AncientWaterUpgrader>(pondSlimeId);
                }
                catch { /* POND_SLIME not defined — ignore */ }
            }
        }

        private static bool IsMelonPresent(string name)
        {
            foreach (var melon in MelonMod.LoadedMelons)
            {
                if (melon.Info?.Name?.Equals(name, StringComparison.OrdinalIgnoreCase) == true)
                    return true;
            }
            return false;
        }

        // ------------------------------------------------------------------
        //  PlotUpgrader — changes water appearance for Ancient Blessing
        // ------------------------------------------------------------------

        public class AncientWaterUpgrader : ModdedPlotUpgrader
        {
            public AncientWaterUpgrader(IntPtr ptr) : base(ptr) { }

            public override void Apply(LandPlot.Upgrade upgrade)
            {
                if (upgrade == Ids.POND_ANCIENT_BLESSING)
                {
                    // Path might differ in SR2 — adjust if the pond prefab changed.
                    var surface = transform.Find("Water/Water Scaler/Surface");
                    if (surface != null)
                    {
                        var renderer = surface.GetComponent<MeshRenderer>();
                        if (renderer != null)
                        {
                            var mat = SRLookup.Get<Material>("Depth Magic Water Ball");
                            if (mat != null)
                                renderer.material = mat;
                        }
                    }
                }
            }
        }
    }

    // ------------------------------------------------------------------
    //  Custom upgrade IDs (auto-assigned by MelonSRML's EnumHolderResolver)
    // ------------------------------------------------------------------
    [EnumHolder]
    static class Ids
    {
        public static LandPlot.Upgrade POND_SLIME_CAPACITY;
        public static LandPlot.Upgrade POND_PLORT_CAPACITY;
        public static LandPlot.Upgrade POND_ANCIENT_BLESSING;
    }

    // ------------------------------------------------------------------
    //  Harmony patches
    // ------------------------------------------------------------------

    /// <summary>
    /// Runs before MelonSRML's LandPlotUIActivatorSetupUI patch so that
    /// currentPlot is set when the isAvailable / isHidden lambdas evaluate.
    /// </summary>
    [HarmonyPatch(typeof(LandPlotUIActivator), nameof(LandPlotUIActivator.SetupUI))]
    [HarmonyPriority(Priority.High)]
    static class Patch_SetCurrentPlot
    {
        static void Prefix(LandPlotUIActivator __instance)
        {
            Main.currentPlot = __instance.landPlot;
        }
    }

    /// <summary>
    /// Multiplies the maximum slime density based on pond upgrades.
    /// Slime Capacity  → x2
    /// Ancient Blessing → x6
    /// </summary>
    [HarmonyPatch(typeof(SlimeEatWater), "CalcMaximumSlimeDensity")]
    static class Patch_CalcMaximumSlimeDensity
    {
        static void Postfix(SlimeEatWater __instance, ref int __result)
        {
            int mult = 1;
            foreach (var w in __instance.waters)
            {
                if (w == null) continue;
                var landPlot = w.GetComponentInParent<LandPlot>();
                if (landPlot == null) continue;

                if (mult < 2 && landPlot.HasUpgrade(Ids.POND_SLIME_CAPACITY))
                    mult = 2;
                if (mult < 6 && landPlot.HasUpgrade(Ids.POND_ANCIENT_BLESSING))
                    mult = 6;
            }
            __result *= mult;
        }
    }

    /// <summary>
    /// Multiplies the maximum plort density based on pond upgrades.
    /// Plort Capacity   → x2
    /// Ancient Blessing  → x6
    /// </summary>
    [HarmonyPatch(typeof(SlimeEatWater), "CalcMaximumPlortDensity")]
    static class Patch_CalcMaximumPlortDensity
    {
        static void Postfix(SlimeEatWater __instance, ref int __result)
        {
            int mult = 1;
            foreach (var w in __instance.waters)
            {
                if (w == null) continue;
                var landPlot = w.GetComponentInParent<LandPlot>();
                if (landPlot == null) continue;

                if (mult < 2 && landPlot.HasUpgrade(Ids.POND_PLORT_CAPACITY))
                    mult = 2;
                if (mult < 6 && landPlot.HasUpgrade(Ids.POND_ANCIENT_BLESSING))
                    mult = 6;
            }
            __result *= mult;
        }
    }
}
