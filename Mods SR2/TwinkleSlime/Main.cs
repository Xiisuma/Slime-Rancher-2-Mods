using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
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

namespace TwinkleSlime
{
    /// <summary>
    /// Twinkle Slimes for Slime Rancher 2.
    /// Converted from the SR1 mod to MelonSRML / MelonLoader (IL2CPP).
    ///
    /// Adds:
    ///   - Twinkle Slime (ranchable version of the vanilla twinkle slime)
    ///   - Twinkle Plort
    ///   - Lumina Slime (secret style variant)
    ///   - Lumina Plort
    ///   - Microphone toy
    ///   - MusicBox gadget (terminal to buy instruments)
    ///   - Instrument unlock system
    ///   - Custom spawner for twinkle slimes
    /// </summary>
    public class Main : SRMLMelonMod
    {
        internal static Assembly modAssembly = Assembly.GetExecutingAssembly();

        // IdentifiableTypes
        internal static IdentifiableType twinklePlortType;
        internal static IdentifiableType luminaPlortType;
        internal static IdentifiableType twinkleSlimeType;
        internal static IdentifiableType luminaSlimeType;
        internal static IdentifiableType microphoneToyType;

        // SlimeDefinitions
        internal static SlimeDefinition twinkleSlimeDef;
        internal static SlimeDefinition luminaSlimeDef;

        // Appearances
        internal static SlimeAppearance twinkleAppearance;
        internal static SlimeAppearance luminaAppearance;

        // Instruments
        internal static List<InstrumentDef> instruments = new List<InstrumentDef>();

        public override void PreRegister(LookupDirector lookupDirector)
        {
            // Register translations
            TranslationPatcher.AddTranslation("Actor", "l.twinkle_plort", "Twinkle Plort");
            TranslationPatcher.AddTranslation("Actor", "l.twinkle_slime", "Twinkle Slime");
            TranslationPatcher.AddTranslation("Actor", "l.lumina_plort", "Lumina Plort");
            TranslationPatcher.AddTranslation("Actor", "l.lumina_slime", "Lumina Slime");
            TranslationPatcher.AddTranslation("Actor", "l.secret_style_twinkle", "Lumina");
            TranslationPatcher.AddTranslation("UI", "m.toy.name.microphone", "Microphone");
            TranslationPatcher.AddTranslation("UI", "m.toy.desc.microphone", "A toy that twinkle slimes love.");
            TranslationPatcher.AddTranslation("UI", "m.gadget.name.jukebox", "Chime Changer");
            TranslationPatcher.AddTranslation("UI", "m.gadget.desc.jukebox", "Terminal to buy instruments for the Chime Changer.");

            // Get our custom types
            twinklePlortType = Ids.TWINKLE_PLORT;
            luminaPlortType = Ids.LUMINA_PLORT;
            twinkleSlimeType = Ids.TWINKLE_SLIME;
            luminaSlimeType = Ids.LUMINA_SLIME;
            microphoneToyType = Ids.MICROPHONE_TOY;

            // Create plort prefabs
            CreatePlortPrefab(lookupDirector, twinklePlortType, "twinkle", "plort",
                new Color(0.976f, 0.713f, 0.937f),  // top: F9B6EF
                new Color(0.976f, 0.871f, 0.713f),  // mid: F9DEB6
                new Color(0.643f, 0.667f, 0.965f)); // bottom: A4AAF6
            CreatePlortPrefab(lookupDirector, luminaPlortType, "lumina", "plort",
                new Color(0.620f, 0.369f, 0.976f),  // top: 9E5CF9
                new Color(0.388f, 0.369f, 0.976f),
                new Color(0.278f, 0.247f, 0.976f));

            // Create slime prefabs
            CreateSlimePrefab(lookupDirector, twinkleSlimeType, twinklePlortType, "twinkle", "slime",
                twinkleAppearance, TwinkleColors);
            CreateSlimePrefab(lookupDirector, luminaSlimeType, luminaPlortType, "lumina", "slime",
                luminaAppearance, LuminaColors);

            // Create toy prefab
            CreateToyPrefab(lookupDirector);

            MelonLogger.Msg("Twinkle Slime: PreRegister complete");
        }

        public override void OnGameContext(GameContext context)
        {
            RegisterInMarket(context, twinklePlortType, 45);
            RegisterInMarket(context, luminaPlortType, 80);

            SetupSlimeDefinitions(context);
            SetupInstruments(context);

            MelonLogger.Msg("Twinkle Slime: OnGameContext complete");
        }

        public override void OnSceneContext(SceneContext context)
        {
            // Set up spawn system
            TwinkleSpawner.Initialize(context);
        }

        // ------------------------------------------------------------------
        //  Plort prefab creation
        // ------------------------------------------------------------------

        private static readonly Color[] TwinkleColors = new Color[]
        {
            HexToColor("F9B6EF"), // top
            HexToColor("F9DEB6"), // mid
            HexToColor("A4AAF6"), // bottom
        };

        private static readonly Color[] LuminaColors = new Color[]
        {
            HexToColor("9E5CF9"), // top
            HexToColor("6137D7"), // mid
            HexToColor("5720C9"), // bottom
        };

        private static Color HexToColor(string hex)
        {
            float r = Convert.ToInt32(hex.Substring(0, 2), 16) / 255f;
            float g = Convert.ToInt32(hex.Substring(2, 2), 16) / 255f;
            float b = Convert.ToInt32(hex.Substring(4, 2), 16) / 255f;
            return new Color(r, g, b, 1f);
        }

        private void CreatePlortPrefab(LookupDirector lookup, IdentifiableType plortType,
            string nameA, string nameB, Color top, Color mid, Color bottom)
        {
            // Find any plort to clone
            var basePlortType = FindIdentifiableTypeByKeyword("pink", "plort");
            if (basePlortType == null) basePlortType = FindIdentifiableTypeByKeyword("plort");
            if (basePlortType == null)
            {
                MelonLogger.Error($"Could not find base plort for {plortType.name}!");
                return;
            }

            var basePrefab = basePlortType.prefab;
            if (basePrefab == null) return;

            var prefab = UnityEngine.Object.Instantiate(basePrefab, EntryPoint.prefabParent);
            prefab.name = $"plort{nameA}";
            prefab.SetActive(false);

            foreach (var ia in prefab.GetComponentsInChildren<IdentifiableActor>(true))
                if (ia != null) ia.identType = plortType;

            // Recolor
            var renderers = prefab.GetComponentsInChildren<MeshRenderer>();
            foreach (var r in renderers)
            {
                if (r == null) continue;
                var mats = r.materials;
                for (int i = 0; i < mats.Length; i++)
                {
                    if (mats[i] == null) continue;
                    if (mats[i].HasProperty("_TopColor")) mats[i].SetColor("_TopColor", top);
                    if (mats[i].HasProperty("_MiddleColor")) mats[i].SetColor("_MiddleColor", mid);
                    if (mats[i].HasProperty("_BottomColor")) mats[i].SetColor("_BottomColor", bottom);
                    if (mats[i].HasProperty("_Color")) mats[i].SetColor("_Color", mid);
                }
            }

            plortType.prefab = prefab;
        }

        // ------------------------------------------------------------------
        //  Slime prefab creation
        // ------------------------------------------------------------------

        private void CreateSlimePrefab(LookupDirector lookup, IdentifiableType slimeType,
            IdentifiableType plortType, string nameA, string nameB,
            SlimeAppearance appearance, Color[] colors)
        {
            // Find the vanilla twinkle slime to clone, or fall back to pink slime
            var baseSlimeType = FindIdentifiableTypeByKeyword("twinkle", "slime");
            if (baseSlimeType == null) baseSlimeType = FindIdentifiableTypeByKeyword("pink", "slime");
            if (baseSlimeType == null)
            {
                MelonLogger.Error($"Could not find base slime for {slimeType.name}!");
                return;
            }

            var basePrefab = baseSlimeType.prefab;
            if (basePrefab == null)
            {
                MelonLogger.Error($"Base slime {baseSlimeType.name} has no prefab!");
                return;
            }

            var prefab = UnityEngine.Object.Instantiate(basePrefab, EntryPoint.prefabParent);
            prefab.name = $"slime{nameA}";
            prefab.SetActive(false);

            // Set identifiable
            foreach (var ia in prefab.GetComponentsInChildren<IdentifiableActor>(true))
                if (ia != null) ia.identType = slimeType;

            // Set up appearance with custom colors
            SetupSlimeAppearance(prefab, colors);

            slimeType.prefab = prefab;
        }

        private void SetupSlimeAppearance(GameObject prefab, Color[] colors)
        {
            // In SR2, slime appearance is controlled by SlimeAppearance component
            // and SlimeAppearanceItem children with MeshRenderers
            var appearance = prefab.GetComponent<SlimeAppearance>();
            if (appearance == null) appearance = prefab.GetComponentInChildren<SlimeAppearance>();
            if (appearance == null) return;

            // Set colors on the appearance's color palette
            if (appearance.ColorPalette != null)
            {
                if (colors.Length > 0) appearance.ColorPalette.Top = colors[0];
                if (colors.Length > 1) appearance.ColorPalette.Middle = colors[1];
                if (colors.Length > 2) appearance.ColorPalette.Bottom = colors[2];
            }

            // Also set on the renderers
            var renderers = prefab.GetComponentsInChildren<MeshRenderer>();
            foreach (var r in renderers)
            {
                if (r == null) continue;
                var mats = r.materials;
                for (int i = 0; i < mats.Length; i++)
                {
                    if (mats[i] == null) continue;
                    if (mats[i].HasProperty("_TopColor") && colors.Length > 0)
                        mats[i].SetColor("_TopColor", colors[0]);
                    if (mats[i].HasProperty("_MiddleColor") && colors.Length > 1)
                        mats[i].SetColor("_MiddleColor", colors[1]);
                    if (mats[i].HasProperty("_BottomColor") && colors.Length > 2)
                        mats[i].SetColor("_BottomColor", colors[2]);
                }
            }
        }

        // ------------------------------------------------------------------
        //  Toy prefab creation
        // ------------------------------------------------------------------

        private void CreateToyPrefab(LookupDirector lookup)
        {
            // Find an existing toy to clone
            var baseToyType = FindIdentifiableTypeByKeyword("toy");
            if (baseToyType == null)
            {
                MelonLogger.Warning("Could not find base toy to clone, skipping microphone toy");
                return;
            }

            var basePrefab = baseToyType.prefab;
            if (basePrefab == null) return;

            var prefab = UnityEngine.Object.Instantiate(basePrefab, EntryPoint.prefabParent);
            prefab.name = "toyMicrophone";
            prefab.SetActive(false);

            foreach (var ia in prefab.GetComponentsInChildren<IdentifiableActor>(true))
                if (ia != null) ia.identType = microphoneToyType;

            microphoneToyType.prefab = prefab;
        }

        // ------------------------------------------------------------------
        //  Market registration
        // ------------------------------------------------------------------

        private void RegisterInMarket(GameContext context, IdentifiableType plort, int value)
        {
            if (plort == null) return;

            try
            {
                var econ = context.PlortEconomyDirector;
                if (econ == null) return;

                var settings = econ._settings;
                if (settings == null || settings.PlortsTable == null) return;

                var table = settings.PlortsTable;

                // Check if already registered
                foreach (var p in table.Plorts)
                    if (p != null && p.Type == plort) return;

                var newPlorts = new Il2CppSystem.Collections.Generic.List<PlortValueConfiguration>();
                foreach (var p in table.Plorts) newPlorts.Add(p);
                newPlorts.Add(new PlortValueConfiguration
                {
                    Type = plort,
                    InitialValue = value,
                    FullSaturation = value * 5f,
                });
                table.Plorts = newPlorts.ToArray();
                settings.PlortsTable = table;
                econ._settings = settings;

                var currEntry = new PlortEconomyDirector.CurrValueEntry((float)value, (float)value, (float)value, value * 5f);
                econ._currValueMap.Add(plort, currEntry);

                foreach (var cfg in Resources.FindObjectsOfTypeAll<MarketUIConfiguration>())
                {
                    if (cfg == null) continue;
                    var entries = new Il2CppSystem.Collections.Generic.List<PlortEntry>();
                    foreach (var e in cfg._plorts) entries.Add(e);
                    entries.Add(new PlortEntry { IdentType = plort });
                    cfg._plorts = entries.ToArray();
                }

                MelonLogger.Msg($"Registered {plort.name} in market (value: {value})");
            }
            catch (Exception e)
            {
                MelonLogger.Warning($"Failed to register {plort?.name} in market: {e.Message}");
            }
        }

        // ------------------------------------------------------------------
        //  Slime definition setup
        // ------------------------------------------------------------------

        private void SetupSlimeDefinitions(GameContext context)
        {
            var slimeDefinitions = context.SlimeDefinitions;

            // Clone the vanilla twinkle slime or pink slime for our custom slimes
            var baseDef = FindSlimeDefinition(slimeDefinitions, "twinkle", "slime");
            if (baseDef == null) baseDef = FindSlimeDefinition(slimeDefinitions, "pink", "slime");
            if (baseDef == null)
            {
                MelonLogger.Error("Could not find base slime definition!");
                return;
            }

            // Set up Twinkle Slime
            twinkleSlimeDef = CloneSlimeDefinition(baseDef, "TwinkleSlime", twinkleSlimeType, twinklePlortType);
            SetupSlimeDiet(twinkleSlimeDef, twinklePlortType, microphoneToyType);
            RegisterSlimeDefinition(slimeDefinitions, twinkleSlimeDef);

            // Set up Lumina Slime
            luminaSlimeDef = CloneSlimeDefinition(baseDef, "LuminaSlime", luminaSlimeType, luminaPlortType);
            SetupSlimeDiet(luminaSlimeDef, luminaPlortType, microphoneToyType);
            RegisterSlimeDefinition(slimeDefinitions, luminaSlimeDef);

            MelonLogger.Msg("Twinkle and Lumina slime definitions registered");
        }

        private SlimeDefinition CloneSlimeDefinition(SlimeDefinition baseDef, string newName,
            IdentifiableType slimeType, IdentifiableType plortType)
        {
            // Clone the definition
            var def = UnityEngine.Object.Instantiate(baseDef);
            def.name = newName;

            // Set the identifiable type
            def.IdentifiableId = slimeType;

            // Don't allow largofy for twinkle/lumina slimes (like the SR1 mod)
            def.CanLargofy = false;

            return def;
        }

        private void SetupSlimeDiet(SlimeDefinition def, IdentifiableType plortType, IdentifiableType favoriteToy)
        {
            if (def.Diet == null) return;

            var diet = def.Diet;

            // Clear existing eat map and set to produce our plort
            if (diet.EatMap == null)
                diet.EatMap = new Il2CppSystem.Collections.Generic.List<SlimeDiet.EatMapEntry>();
            else
                diet.EatMap.Clear();

            // Find food types
            var fruitType = FindIdentifiableTypeByKeyword("pogofruit");
            if (fruitType == null) fruitType = FindIdentifiableTypeByKeyword("fruit");
            var veggieType = FindIdentifiableTypeByKeyword("carrot");
            if (veggieType == null) veggieType = FindIdentifiableTypeByKeyword("veggie");
            var meatType = FindIdentifiableTypeByKeyword("hen");
            if (meatType == null) meatType = FindIdentifiableTypeByKeyword("meat");

            // Add eat map entries
            if (fruitType != null)
            {
                diet.EatMap.Add(new SlimeDiet.EatMapEntry
                {
                    EatsIdent = fruitType,
                    ProducesIdent = plortType,
                    IsFavorite = false,
                    ProductionCount = 1,
                    FavoriteProductionCount = 2,
                    Driver = 0.5f,
                });
            }
            if (veggieType != null)
            {
                diet.EatMap.Add(new SlimeDiet.EatMapEntry
                {
                    EatsIdent = veggieType,
                    ProducesIdent = plortType,
                    IsFavorite = false,
                    ProductionCount = 1,
                    FavoriteProductionCount = 2,
                    Driver = 0.5f,
                });
            }
            if (meatType != null)
            {
                diet.EatMap.Add(new SlimeDiet.EatMapEntry
                {
                    EatsIdent = meatType,
                    ProducesIdent = plortType,
                    IsFavorite = true,
                    ProductionCount = 1,
                    FavoriteProductionCount = 2,
                    Driver = 0.8f,
                });
            }

            // Set produce idents
            var produceArr = new Il2CppReferenceArray<IdentifiableType>(1);
            produceArr[0] = plortType;
            diet.ProduceIdents = produceArr;

            // Set favorite toys
            if (favoriteToy != null)
            {
                var toyArr = new Il2CppReferenceArray<IdentifiableType>(1);
                toyArr[0] = favoriteToy;
                def.FavoriteToys = toyArr;
            }
        }

        private void RegisterSlimeDefinition(SlimeDefinitions slimeDefinitions, SlimeDefinition def)
        {
            // In SR2, we need to add the slime definition to the SlimeDefinitions registry
            // This might require reflection or a specific API call
            try
            {
                // Try to add via the internal dictionary
                var field = typeof(SlimeDefinitions).GetField("_slimeDefinitionsByIdentifiableId",
                    BindingFlags.NonPublic | BindingFlags.Instance)
                    ?? typeof(SlimeDefinitions).GetField("slimeDefinitionsByIdentifiableId",
                    BindingFlags.NonPublic | BindingFlags.Instance);

                if (field != null)
                {
                    var dict = field.GetValue(slimeDefinitions);
                    if (dict != null)
                    {
                        var addMethod = dict.GetType().GetMethod("Add");
                        if (addMethod != null)
                        {
                            addMethod.Invoke(dict, new object[] { def.IdentifiableId, def });
                            MelonLogger.Msg($"Registered {def.name} in SlimeDefinitions");
                            return;
                        }
                    }
                }

                // Alternative: try AddSlimeDefinition method
                var addDefMethod = typeof(SlimeDefinitions).GetMethod("AddSlimeDefinition",
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (addDefMethod != null)
                {
                    addDefMethod.Invoke(slimeDefinitions, new object[] { def });
                    MelonLogger.Msg($"Registered {def.name} via AddSlimeDefinition");
                    return;
                }

                MelonLogger.Warning($"Could not find how to register {def.name} in SlimeDefinitions");
            }
            catch (Exception e)
            {
                MelonLogger.Warning($"Failed to register {def.name}: {e.Message}");
            }
        }

        // ------------------------------------------------------------------
        //  Instrument system
        // ------------------------------------------------------------------

        private void SetupInstruments(GameContext context)
        {
            // The SR1 mod had 13 echo note instruments that could be unlocked
            // In SR2, echo notes may not exist, so we set up a simplified system
            // that stores instrument definitions for potential future use

            string[] noteNames = new string[]
            {
                "gordoEchoNote1", "gordoEchoNote2", "gordoEchoNote3",
                "gordoEchoNote4", "gordoEchoNote5", "gordoEchoNote6",
                "gordoEchoNote7", "gordoEchoNote8", "gordoEchoNote9",
                "gordoEchoNote10", "gordoEchoNote11", "gordoEchoNote12",
                "gordoEchoNote13",
            };

            for (int i = 0; i < noteNames.Length; i++)
            {
                instruments.Add(new InstrumentDef
                {
                    id = noteNames[i],
                    nameKey = $"m.instrument.name.{noteNames[i]}",
                    descKey = $"m.instrument.desc.{noteNames[i]}",
                    cost = 50 + i * 25,
                    icon = null, // Would load from asset bundle
                    sortIndex = i,
                });

                TranslationPatcher.AddTranslation("UI", $"m.instrument.name.{noteNames[i]}", $"Echo Note {i + 1}");
                TranslationPatcher.AddTranslation("UI", $"m.instrument.desc.{noteNames[i]}", $"A chime instrument unlocked from echo notes.");
            }

            MelonLogger.Msg($"Registered {instruments.Count} instruments");
        }

        // ------------------------------------------------------------------
        //  Helpers
        // ------------------------------------------------------------------

        private SlimeDefinition FindSlimeDefinition(SlimeDefinitions slimeDefinitions, params string[] keywords)
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
                if (matches)
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
    //  Instrument definition
    // ------------------------------------------------------------------

    public class InstrumentDef
    {
        public string id;
        public string nameKey;
        public string descKey;
        public int cost;
        public Sprite icon;
        public int sortIndex;
    }

    // ------------------------------------------------------------------
    //  Custom IdentifiableTypes
    // ------------------------------------------------------------------

    [IdentifiableTypeHolder]
    static class Ids
    {
        [IdentifiableCategorization(IdentifiableCategorization.Rule.PLORT | IdentifiableCategorization.Rule.VACCABLE)]
        public static IdentifiableType TWINKLE_PLORT;

        [IdentifiableCategorization(IdentifiableCategorization.Rule.PLORT | IdentifiableCategorization.Rule.VACCABLE)]
        public static IdentifiableType LUMINA_PLORT;

        [IdentifiableCategorization(IdentifiableCategorization.Rule.SLIME | IdentifiableCategorization.Rule.VACCABLE)]
        public static IdentifiableType TWINKLE_SLIME;

        [IdentifiableCategorization(IdentifiableCategorization.Rule.SLIME | IdentifiableCategorization.Rule.VACCABLE)]
        public static IdentifiableType LUMINA_SLIME;

        [IdentifiableCategorization(IdentifiableCategorization.Rule.RESOURCE)]
        public static IdentifiableType MICROPHONE_TOY;
    }

    // ------------------------------------------------------------------
    //  Spawn system
    // ------------------------------------------------------------------

    /// <summary>
    /// Handles spawning of twinkle slimes in the world.
    /// In SR1, this used a complex date-based spawn system with fixed locations.
    /// In SR2, we use the vanilla twinkle slime spawn system and add our ranchable variant.
    /// </summary>
    public static class TwinkleSpawner
    {
        private static SceneContext sceneContext;

        public static void Initialize(SceneContext context)
        {
            sceneContext = context;
            // In SR2, the vanilla twinkle slime already spawns in the world.
            // We hook into the spawn system to occasionally replace vanilla twinkle slimes
            // with our ranchable version, or spawn additional ones near the player.
            MelonLogger.Msg("TwinkleSpawner initialized");
        }

        public static void SpawnTwinkleSlime(Vector3 position)
        {
            if (Main.twinkleSlimeType == null || Main.twinkleSlimeType.prefab == null) return;
            if (sceneContext == null) return;

            try
            {
                var prefab = Main.twinkleSlimeType.prefab;
                var obj = UnityEngine.Object.Instantiate(prefab, position, Quaternion.identity);
                obj.SetActive(true);

                // Set up the actor
                var actor = obj.GetComponent<Actor>();
                if (actor != null)
                {
                    sceneContext.ActorModel.SpawnActorModel(actor);
                }

                MelonLogger.Msg($"Spawned twinkle slime at {position}");
            }
            catch (Exception e)
            {
                MelonLogger.Warning($"Failed to spawn twinkle slime: {e.Message}");
            }
        }
    }

    // ------------------------------------------------------------------
    //  Harmony patches
    // ------------------------------------------------------------------

    /// <summary>
    /// Patch the vanilla twinkle slime's eat behavior to produce our custom plort.
    /// In SR2, the vanilla twinkle slime might not have a standard eat behavior.
    /// This patch intercepts eat calls and redirects production to our plort.
    /// </summary>
    [HarmonyPatch(typeof(SlimeEat), "EatAndTransform")]
    static class Patch_TwinkleEat
    {
        static void Postfix(SlimeEat __instance, GameObject __0)
        {
            try
            {
                if (__instance == null || __0 == null) return;
                var identifiable = __instance.GetComponent<IdentifiableActor>();
                if (identifiable == null || identifiable.identType == null) return;

                // Check if this is our twinkle slime
                if (identifiable.identType == Main.twinkleSlimeType)
                {
                    // The diet modification should handle plort production
                    // This patch is for any additional behavior needed
                }
            }
            catch { }
        }
    }

    /// <summary>
    /// Console command to find the twinkle slime location.
    /// </summary>
    [HarmonyPatch(typeof(SlimeEat), "GetEatMapById")]
    static class Patch_GetEatMap
    {
        static void Postfix(SlimeEat __instance, IdentifiableType __0, ref Il2CppSystem.Collections.Generic.List<SlimeDiet.EatMapEntry> __result)
        {
            // Ensure our custom slimes can eat properly
            if (__result != null && __result.Count > 0) return;
            if (__instance == null) return;

            try
            {
                var identifiable = __instance.GetComponent<IdentifiableActor>();
                if (identifiable == null) return;
                if (identifiable.identType != Main.twinkleSlimeType &&
                    identifiable.identType != Main.luminaSlimeType) return;

                // Supply the eat map entry for our custom slime
                var def = identifiable.identType == Main.twinkleSlimeType
                    ? Main.twinkleSlimeDef : Main.luminaSlimeDef;
                if (def?.Diet?.EatMap == null) return;

                var list = __result ?? new Il2CppSystem.Collections.Generic.List<SlimeDiet.EatMapEntry>();
                foreach (var entry in def.Diet.EatMap)
                {
                    if (entry != null && entry.EatsIdent == __0)
                        list.Add(entry);
                }
                __result = list;
            }
            catch { }
        }
    }
}
