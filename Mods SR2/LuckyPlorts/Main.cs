using System;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using Il2Cpp;
using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.Injection;
using Il2CppMonomiPark.SlimeRancher;
using Il2CppMonomiPark.SlimeRancher.DataModel;
using MelonLoader;
using MelonSRML;
using MelonSRML.EnumPatcher;
using MelonSRML.SR2;
using UnityEngine;
using UnityEngine.Localization;

namespace LuckyPlorts
{
    /// <summary>
    /// Lucky Plorts for Slime Rancher 2.
    /// Converted from the SR1 mod by DogeisCut to MelonSRML / MelonLoader (IL2CPP).
    ///
    /// Adds a "Lucky Plort" that is produced when a Lucky Slime eats a Stony Hen.
    /// </summary>
    public class Main : SRMLMelonMod
    {
        internal static Assembly modAssembly = Assembly.GetExecutingAssembly();

        internal static IdentifiableType luckyPlortType;

        public override void PreRegister(LookupDirector lookupDirector)
        {
            CreateLuckyPlort(lookupDirector);
        }

        public override void OnGameContext(GameContext context)
        {
            RegisterInMarket(context);
            ModifyLuckySlimeDiet(context);
        }

        // ------------------------------------------------------------------
        //  Create the lucky plort (IdentifiableType + prefab)
        // ------------------------------------------------------------------

        private void CreateLuckyPlort(LookupDirector lookupDirector)
        {
            luckyPlortType = Ids.LUCKY_PLORT;

            // Translation
            TranslationPatcher.AddTranslation("Actor", "l.lucky_plort", "Lucky Plort");

            // Find a pink plort to clone
            var pinkPlortType = FindIdentifiableTypeByKeyword("pink", "plort");
            if (pinkPlortType == null)
            {
                MelonLogger.Error("Could not find pink plort IdentifiableType!");
                return;
            }

            // Clone the prefab
            GameObject basePrefab = pinkPlortType.prefab;
            if (basePrefab == null)
            {
                MelonLogger.Error("Pink plort has no prefab!");
                return;
            }

            var prefab = UnityEngine.Object.Instantiate(basePrefab, EntryPoint.prefabParent);
            prefab.name = "Plort Lucky";
            prefab.SetActive(false);

            // Set the IdentifiableActor components to our type
            foreach (var ia in prefab.GetComponentsInChildren<IdentifiableActor>(true))
            {
                if (ia != null) ia.identType = luckyPlortType;
            }

            // Recolor to gold
            SetPlortColors(prefab);

            // CRITICAL: set the prefab on the IdentifiableType itself
            luckyPlortType.prefab = prefab;

            MelonLogger.Msg("Lucky Plort prefab created and registered!");
        }

        private void SetPlortColors(GameObject prefab)
        {
            Color goldTop = new Color(1.0f, 0.85f, 0.2f, 1.0f);
            Color goldMid = new Color(1.0f, 0.75f, 0.1f, 1.0f);
            Color goldBottom = new Color(0.9f, 0.6f, 0.05f, 1.0f);

            var renderers = prefab.GetComponentsInChildren<MeshRenderer>();
            foreach (var renderer in renderers)
            {
                if (renderer == null) continue;
                var materials = renderer.materials;
                for (int i = 0; i < materials.Length; i++)
                {
                    if (materials[i] == null) continue;
                    if (materials[i].HasProperty("_TopColor"))
                        materials[i].SetColor("_TopColor", goldTop);
                    if (materials[i].HasProperty("_MiddleColor"))
                        materials[i].SetColor("_MiddleColor", goldMid);
                    if (materials[i].HasProperty("_BottomColor"))
                        materials[i].SetColor("_BottomColor", goldBottom);
                    if (materials[i].HasProperty("_Color"))
                        materials[i].SetColor("_Color", goldMid);
                }
            }
        }

        // ------------------------------------------------------------------
        //  Register in the market economy so the plort can be sold
        // ------------------------------------------------------------------

        private void RegisterInMarket(GameContext context)
        {
            if (luckyPlortType == null) return;

            try
            {
                var econ = context.PlortEconomyDirector;
                if (econ == null) return;

                var settings = econ._settings;
                if (settings == null || settings.PlortsTable == null) return;

                var table = settings.PlortsTable;

                // Check if already registered
                foreach (var p in table.Plorts)
                {
                    if (p != null && p.Type == luckyPlortType) return;
                }

                // Add plort to economy table
                var newPlorts = new Il2CppSystem.Collections.Generic.List<PlortValueConfiguration>();
                foreach (var p in table.Plorts)
                    newPlorts.Add(p);
                newPlorts.Add(new PlortValueConfiguration
                {
                    Type = luckyPlortType,
                    InitialValue = 60,
                    FullSaturation = 300f,
                });
                table.Plorts = newPlorts.ToArray();
                settings.PlortsTable = table;
                econ._settings = settings;

                // Seed runtime value
                var currEntry = new PlortEconomyDirector.CurrValueEntry(60f, 60f, 60f, 300f);
                econ._currValueMap.Add(luckyPlortType, currEntry);

                // Add to MarketUIConfiguration so the shop lists it
                foreach (var cfg in Resources.FindObjectsOfTypeAll<MarketUIConfiguration>())
                {
                    if (cfg == null) continue;
                    var entries = new Il2CppSystem.Collections.Generic.List<PlortEntry>();
                    foreach (var e in cfg._plorts)
                        entries.Add(e);
                    entries.Add(new PlortEntry { IdentType = luckyPlortType });
                    cfg._plorts = entries.ToArray();
                }

                MelonLogger.Msg("Lucky Plort registered in market economy!");
            }
            catch (Exception e)
            {
                MelonLogger.Warning($"Failed to register in market: {e.Message}");
            }
        }

        // ------------------------------------------------------------------
        //  Modify the lucky slime's diet
        // ------------------------------------------------------------------

        private void ModifyLuckySlimeDiet(GameContext context)
        {
            if (luckyPlortType == null) return;

            var slimeDefinitions = context.SlimeDefinitions;
            if (slimeDefinitions == null)
            {
                MelonLogger.Error("SlimeDefinitions not found!");
                return;
            }

            var stonyHenType = FindIdentifiableTypeByKeyword("stony", "hen");
            if (stonyHenType == null)
            {
                MelonLogger.Error("Could not find Stony Hen IdentifiableType!");
                return;
            }

            var luckySlimeDef = FindLuckySlimeDefinition(slimeDefinitions);
            if (luckySlimeDef == null)
            {
                MelonLogger.Error("Could not find Lucky Slime definition!");
                return;
            }

            var diet = luckySlimeDef.Diet;
            if (diet == null)
            {
                MelonLogger.Error("Lucky Slime has no Diet!");
                return;
            }

            if (diet.EatMap == null)
                diet.EatMap = new Il2CppSystem.Collections.Generic.List<SlimeDiet.EatMapEntry>();

            var eatMapEntry = new SlimeDiet.EatMapEntry
            {
                EatsIdent = stonyHenType,
                ProducesIdent = luckyPlortType,
                IsFavorite = true,
                FavoriteProductionCount = 2,
                ProductionCount = 1,
                BecomesIdent = null,
                Driver = 0.5f,
                ExtraDrive = 0f,
                MinDrive = 0f,
            };

            diet.EatMap.Add(eatMapEntry);

            MelonLogger.Msg($"Added Lucky Plort to {luckySlimeDef.name}'s diet!");
        }

        // ------------------------------------------------------------------
        //  Helpers
        // ------------------------------------------------------------------

        private SlimeDefinition FindLuckySlimeDefinition(SlimeDefinitions slimeDefinitions)
        {
            foreach (var type in SRLookup.IdentifiableTypes)
            {
                if (type == null || string.IsNullOrEmpty(type.ReferenceId)) continue;
                if (type.ReferenceId.ToLowerInvariant().Contains("lucky") &&
                    type.ReferenceId.ToLowerInvariant().Contains("slime"))
                {
                    var slime = slimeDefinitions.GetSlimeByIdentifiableId(type);
                    if (slime != null) return slime;
                }
            }

            // Fallback: search all slime definitions
            foreach (var type in SRLookup.IdentifiableTypes)
            {
                if (type == null || string.IsNullOrEmpty(type.ReferenceId)) continue;
                if (type.ReferenceId.ToLowerInvariant().Contains("lucky"))
                {
                    var slime = slimeDefinitions.GetSlimeByIdentifiableId(type);
                    if (slime != null) return slime;
                }
            }

            return null;
        }

        private IdentifiableType FindIdentifiableTypeByKeyword(params string[] keywords)
        {
            foreach (var type in SRLookup.IdentifiableTypes)
            {
                if (type == null || string.IsNullOrEmpty(type.ReferenceId)) continue;
                string refId = type.ReferenceId.ToLowerInvariant();
                bool matches = true;
                foreach (var kw in keywords)
                {
                    if (!refId.Contains(kw.ToLowerInvariant()))
                    {
                        matches = false;
                        break;
                    }
                }
                if (matches) return type;
            }
            return null;
        }
    }

    // ------------------------------------------------------------------
    //  Custom IdentifiableType for the Lucky Plort
    // ------------------------------------------------------------------
    [IdentifiableTypeHolder]
    static class Ids
    {
        [IdentifiableCategorization(IdentifiableCategorization.Rule.PLORT | IdentifiableCategorization.Rule.VACCABLE)]
        public static IdentifiableType LUCKY_PLORT;
    }
}
