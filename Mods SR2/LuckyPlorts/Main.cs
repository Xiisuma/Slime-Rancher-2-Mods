using System;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using Il2Cpp;
using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.Injection;
using Il2CppMonomiPark.SlimeRancher;
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

        // Cached in PreRegister / OnGameContext
        internal static IdentifiableType luckyPlortType;
        internal static GameObject luckyPlortPrefab;

        public override void PreRegister(LookupDirector lookupDirector)
        {
            CreateLuckyPlortPrefab(lookupDirector);
        }

        public override void OnGameContext(GameContext context)
        {
            ModifyLuckySlimeDiet(context);
        }

        // ------------------------------------------------------------------
        //  Create the lucky plort prefab
        // ------------------------------------------------------------------

        private void CreateLuckyPlortPrefab(LookupDirector lookupDirector)
        {
            // Get our custom IdentifiableType (created by IdentifiableTypeResolver)
            luckyPlortType = Ids.LUCKY_PLORT;

            // Add translation for the plort name
            TranslationPatcher.AddTranslation("Actor", "l.lucky_plort", "Lucky Plort");

            // Copy an existing plort prefab (pink plort is the most basic)
            var pinkPlortType = FindIdentifiableType("pink_plort", "PinkPlort", "plort_pink");
            if (pinkPlortType == null)
            {
                pinkPlortType = FindIdentifiableTypeByKeyword("pink", "plort");
            }

            if (pinkPlortType == null)
            {
                MelonLogger.Error("Could not find pink plort IdentifiableType to copy!");
                return;
            }

            // Get the prefab for the pink plort
            GameObject basePrefab = lookupDirector.GetPrefab(pinkPlortType);
            if (basePrefab == null)
            {
                MelonLogger.Error("Could not find pink plort prefab!");
                return;
            }

            // Create a copy
            luckyPlortPrefab = UnityEngine.Object.Instantiate(basePrefab, EntryPoint.prefabParent);
            luckyPlortPrefab.name = "Plort Lucky";
            luckyPlortPrefab.SetActive(false);

            // Set the Identifiable component to our custom type
            var identifiable = luckyPlortPrefab.GetComponent<Identifiable>();
            if (identifiable != null)
            {
                identifiable._identifiableType = luckyPlortType;
            }

            // Change the color to gold/lucky
            SetPlortColors(luckyPlortPrefab);

            // Register the prefab with the LookupDirector
            RegisterPrefabWithLookup(lookupDirector, luckyPlortType, luckyPlortPrefab);

            MelonLogger.Msg("Lucky Plort prefab created and registered!");
        }

        private void SetPlortColors(GameObject prefab)
        {
            // The SR1 mod set TopColor, MiddleColor, BottomColor to gold.
            // In SR2, the plort renderer uses a MeshRenderer with materials.
            // We'll tint the materials gold.
            Color goldTop = new Color(1.0f, 0.85f, 0.2f, 1.0f);      // bright gold
            Color goldMid = new Color(1.0f, 0.75f, 0.1f, 1.0f);      // golden
            Color goldBottom = new Color(0.9f, 0.6f, 0.05f, 1.0f);   // dark gold

            var renderers = prefab.GetComponentsInChildren<MeshRenderer>();
            foreach (var renderer in renderers)
            {
                if (renderer == null) continue;
                var materials = renderer.materials;
                for (int i = 0; i < materials.Length; i++)
                {
                    if (materials[i] == null) continue;
                    // Try to set color via shader properties
                    if (materials[i].HasProperty("_TopColor"))
                        materials[i].SetColor("_TopColor", goldTop);
                    if (materials[i].HasProperty("_MiddleColor"))
                        materials[i].SetColor("_MiddleColor", goldMid);
                    if (materials[i].HasProperty("_BottomColor"))
                        materials[i].SetColor("_BottomColor", goldBottom);
                    // Fallback: set the main color
                    if (materials[i].HasProperty("_Color"))
                        materials[i].SetColor("_Color", goldMid);
                }
            }
        }

        private void RegisterPrefabWithLookup(LookupDirector lookupDirector, IdentifiableType type, GameObject prefab)
        {
            // In SR2, LookupDirector stores prefabs in a dictionary keyed by IdentifiableType.
            // We add our entry so the game can spawn our plort.
            try
            {
                // Try using the dictionary directly
                var prefabsField = typeof(LookupDirector).GetField("_prefabs", BindingFlags.NonPublic | BindingFlags.Instance)
                    ?? typeof(LookupDirector).GetField("prefabs", BindingFlags.NonPublic | BindingFlags.Instance)
                    ?? typeof(LookupDirector).GetField("_prefabDict", BindingFlags.NonPublic | BindingFlags.Instance);

                if (prefabsField != null)
                {
                    var dict = prefabsField.GetValue(lookupDirector);
                    if (dict != null)
                    {
                        // Use reflection to call Add on the Il2Cpp dictionary
                        var addMethod = dict.GetType().GetMethod("Add");
                        if (addMethod != null)
                        {
                            addMethod.Invoke(dict, new object[] { type, prefab });
                            MelonLogger.Msg("Registered lucky plort prefab via _prefabs dictionary");
                            return;
                        }
                    }
                }

                // Alternative: try Il2Cpp field access
                foreach (var field in typeof(Il2Cpp.LookupDirector).GetFields(BindingFlags.NonPublic | BindingFlags.Instance))
                {
                    if (field.FieldType.Name.Contains("Dictionary") || field.FieldType.Name.Contains("Map"))
                    {
                        try
                        {
                            var dict = field.GetValue(lookupDirector);
                            if (dict != null)
                            {
                                var addMethod = dict.GetType().GetMethod("Add");
                                if (addMethod != null)
                                {
                                    addMethod.Invoke(dict, new object[] { type, prefab });
                                    MelonLogger.Msg($"Registered lucky plort prefab via {field.Name}");
                                    return;
                                }
                            }
                        }
                        catch { }
                    }
                }
            }
            catch (Exception e)
            {
                MelonLogger.Error($"Failed to register prefab: {e.Message}");
            }
        }

        // ------------------------------------------------------------------
        //  Modify the lucky slime's diet
        // ------------------------------------------------------------------

        private void ModifyLuckySlimeDiet(GameContext context)
        {
            // Find the lucky slime definition
            var slimeDefinitions = SRSingleton<GameContext>.Instance.SlimeDefinitions;
            if (slimeDefinitions == null)
            {
                MelonLogger.Error("SlimeDefinitions not found!");
                return;
            }

            // Find the stony hen IdentifiableType
            var stonyHenType = FindIdentifiableType("stony_hen", "StonyHen", "stonyHen");
            if (stonyHenType == null)
            {
                stonyHenType = FindIdentifiableTypeByKeyword("stony", "hen");
            }

            if (stonyHenType == null)
            {
                MelonLogger.Error("Could not find Stony Hen IdentifiableType!");
                return;
            }

            // Find the lucky slime
            var luckySlimeDef = FindLuckySlimeDefinition(slimeDefinitions);

            if (luckySlimeDef == null)
            {
                MelonLogger.Error("Could not find Lucky Slime definition!");
                return;
            }

            // Add the diet entry: stony hen → lucky plort
            var diet = luckySlimeDef.Diet;
            if (diet == null)
            {
                MelonLogger.Error("Lucky Slime has no Diet!");
                return;
            }

            // Create a new EatMapEntry
            var eatMapEntry = new SlimeDiet.EatMapEntry
            {
                eats = stonyHenType,
                producesId = luckyPlortType,
                isFavorite = true,
                favoriteProductionCount = 2,
            };

            // Add to the EatMap
            diet.EatMap.Add(eatMapEntry);

            MelonLogger.Msg($"Added Lucky Plort to {luckySlimeDef.name}'s diet (eats: {stonyHenType.name}, produces: {luckyPlortType.name})");
        }

        // ------------------------------------------------------------------
        //  Helper methods
        // ------------------------------------------------------------------

        private SlimeDefinition FindLuckySlimeDefinition(SlimeDefinitions slimeDefinitions)
        {
            // Search through all slime definitions for one containing "lucky"
            var allSlimes = slimeDefinitions.GetAll();
            if (allSlimes != null)
            {
                foreach (var slime in allSlimes)
                {
                    if (slime == null) continue;
                    string name = slime.name?.ToLowerInvariant() ?? "";
                    string refId = slime.ReferenceId?.ToLowerInvariant() ?? "";
                    if (name.Contains("lucky") || refId.Contains("lucky"))
                        return slime;
                }
            }

            // Alternative: search by IdentifiableType reference ID
            var luckyType = FindIdentifiableTypeByKeyword("lucky", "slime");
            if (luckyType != null)
            {
                var slime = slimeDefinitions.GetSlimeByIdentifiableId(luckyType);
                if (slime != null) return slime;
            }

            return null;
        }

        private IdentifiableType FindIdentifiableType(params string[] possibleNames)
        {
            foreach (var type in SRLookup.IdentifiableTypes)
            {
                if (type == null || string.IsNullOrEmpty(type.ReferenceId)) continue;
                string refId = type.ReferenceId.ToLowerInvariant();
                foreach (var name in possibleNames)
                {
                    if (refId == name.ToLowerInvariant())
                        return type;
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
    //  MelonSRML's IdentifiableTypeResolver auto-creates the ScriptableObject
    //  and registers it in the IdentifiableTypeGroups.
    // ------------------------------------------------------------------
    [IdentifiableTypeHolder]
    static class Ids
    {
        [IdentifiableCategorization(IdentifiableCategorization.Rule.PLORT)]
        public static IdentifiableType LUCKY_PLORT;
    }
}
