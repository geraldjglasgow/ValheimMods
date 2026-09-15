using System.Collections.Generic;
using BepInEx.Configuration;
using SyncedConfig;
using UnityEngine;

namespace FeastMaster
{
    /// <summary>Section 0 and one section per food and per mead, bound from the item database.</summary>
    public static class FeastMasterData
    {
        public const string GlobalSection = "0. Global Settings";
        private const string OldLockSection = "General";
        private const string OldMeadPrefix = "0Meads_";

        public const string Health = "Health";
        public const string Stamina = "Stamina";
        public const string Duration = "Duration";
        public const string HealthRegen = "HealthRegen";
        public const string Eitr = "Eitr";
        public const string Vigor = "Vigor";
        public const string EitrVigor = "EitrVigor";

        /// <summary>Per food prefab name: Health / Stamina / Duration / HealthRegen / Eitr / Vigor / EitrVigor entries.</summary>
        public static Dictionary<string, Dictionary<string, ConfigEntry<float>>> FoodConfigs { get; }
            = new Dictionary<string, Dictionary<string, ConfigEntry<float>>>();

        /// <summary>Per mead prefab name.</summary>
        public static Dictionary<string, MeadEffectConfig> MeadConfigs { get; }
            = new Dictionary<string, MeadEffectConfig>();

        /// <summary>Mead configs keyed by the name hash of the mead's consume status effect, for the SEMan patch.</summary>
        public static Dictionary<int, MeadEffectConfig> MeadsByEffectHash { get; }
            = new Dictionary<int, MeadEffectConfig>();

        // Localization token ($item_...) to prefab name, for item data without a prefab reference.
        private static readonly Dictionary<string, string> foodsByToken = new Dictionary<string, string>();

        public static ConfigEntry<float> HealthModifier { get; private set; }
        public static ConfigEntry<float> StaminaModifier { get; private set; }
        public static ConfigEntry<float> DurationModifier { get; private set; }
        public static ConfigEntry<float> HealthRegenModifier { get; private set; }
        public static ConfigEntry<float> EitrModifier { get; private set; }
        public static ConfigEntry<bool> DisableFoodDegradation { get; private set; }
        public static ConfigEntry<float> DegradationCurve { get; private set; }
        public static ConfigEntry<float> EatAgainAt { get; private set; }
        public static ConfigEntry<bool> LockConfiguration { get; private set; }

        private static SyncedConfiguration synced;

        public static void Initialize(SyncedConfiguration config)
        {
            synced = config;
            HealthModifier = config.Bind(GlobalSection, "Health Modifier", 1f, "Multiplier for health value of all foods.");
            StaminaModifier = config.Bind(GlobalSection, "Stamina Modifier", 1f, "Multiplier for stamina value of all foods.");
            DurationModifier = config.Bind(GlobalSection, "Duration Modifier", 1f, "Multiplier for duration of all foods. Food Rate in section 7 also changes how fast food runs out; both apply.");
            HealthRegenModifier = config.Bind(GlobalSection, "Health Regen Modifier", 1f, "Multiplier for health regeneration of all foods.");
            EitrModifier = config.Bind(GlobalSection, "Eitr Modifier", 1f, "Multiplier for eitr value of all foods.");
            DisableFoodDegradation = config.Bind(GlobalSection, "Disable Food Degradation", false,
                "When enabled, food stats do not decrease over time.");
            DegradationCurve = config.Bind(GlobalSection, "Degradation Curve", 0.3f,
                "Exponent of the food strength curve: strength = remaining fraction ^ curve. 0.3 is the game's curve, 1 is linear, higher values make food fade sooner, 0 means no fading. Disable Food Degradation overrides it.");
            EatAgainAt = config.Bind(GlobalSection, "Eat Again At", 0.5f,
                "Fraction of a food's duration below which it can be eaten again. The game uses 0.5; 1 allows it at any time, 0 only once the food has run out.", acceptableValues: new AcceptableValueRange<float>(0f, 1f));
            BindLock(config);
        }

        /// <summary>Lock Configuration lived in section General up to 3.3.5; its value is carried over.</summary>
        private static void BindLock(SyncedConfiguration config)
        {
            const string key = "Lock Configuration";
            string migrated = ConfigMigration.TakeOrphan(config.Config, OldLockSection, GlobalSection, key);
            LockConfiguration = config.BindLocking(GlobalSection, key, true,
                "Server only. When on, every player uses the server's values for this file and cannot override them locally.");
            ConfigMigration.Apply(LockConfiguration, migrated);
        }

        /// <summary>
        /// Binds one section per food and per mead found in the item database and writes the configured values
        /// into the items. Safe to call repeatedly: items that already have entries are skipped, so the start scene
        /// and the main scene database can both feed it; the item references are refreshed every time.
        /// </summary>
        public static void LoadConfigurations()
        {
            ObjectDB db = ObjectDB.instance;
            if (db == null || db.m_items.Count == 0)
                return;

            System.Diagnostics.Stopwatch stopwatch = System.Diagnostics.Stopwatch.StartNew();
            if (BindAll(db))
                FeastMaster.Log.LogInfo($"Loaded {FoodConfigs.Count} food and {MeadConfigs.Count} mead configurations in {stopwatch.ElapsedMilliseconds} ms.");
            ItemValues.ApplyAll();
        }

        /// <summary>
        /// Binds every consumable of the database. BepInEx rewrites the whole .cfg on every Bind by default; with
        /// hundreds of entries that is hundreds of full file writes during scene load, so the file is written once
        /// at the end instead, and only when new entries appeared. Returns whether new entries appeared.
        /// </summary>
        private static bool BindAll(ObjectDB db)
        {
            int entriesBefore = FoodConfigs.Count + MeadConfigs.Count;
            ConfigFile configFile = synced.Config;
            bool saveOnSet = configFile.SaveOnConfigSet;
            configFile.SaveOnConfigSet = false;
            ItemValues.SuspendWhileBinding(true);
            try
            {
                foreach (GameObject prefab in db.m_items)
                    BindConsumable(prefab);
            }
            finally
            {
                configFile.SaveOnConfigSet = saveOnSet;
                ItemValues.SuspendWhileBinding(false);
            }
            bool changed = FoodConfigs.Count + MeadConfigs.Count != entriesBefore;
            if (changed && saveOnSet)
                configFile.Save();
            return changed;
        }

        private static void BindConsumable(GameObject prefab)
        {
            ItemDrop itemDrop = prefab != null ? prefab.GetComponent<ItemDrop>() : null;
            if (itemDrop == null)
                return;

            ItemDrop.ItemData.SharedData shared = itemDrop.m_itemData.m_shared;
            if (shared.m_itemType != ItemDrop.ItemData.ItemType.Consumable)
                return;

            if (shared.m_food > 0f || shared.m_foodStamina > 0f || shared.m_foodEitr > 0f)
            {
                BindFood(prefab.name, shared);
                ItemValues.RegisterFood(prefab.name);
            }
            else if (shared.m_consumeStatusEffect is SE_Stats effect)
            {
                BindMead(prefab.name, effect);
                ItemValues.RegisterMead(prefab.name, effect);
            }
        }

        private static void BindFood(string prefabName, ItemDrop.ItemData.SharedData shared)
        {
            if (FoodConfigs.ContainsKey(prefabName))
                return;

            FoodConfigs[prefabName] = new Dictionary<string, ConfigEntry<float>>
            {
                [Health] = synced.Bind(prefabName, Health, shared.m_food, $"Health value of {prefabName}."),
                [Stamina] = synced.Bind(prefabName, Stamina, shared.m_foodStamina, $"Stamina value of {prefabName}."),
                [Duration] = synced.Bind(prefabName, Duration, shared.m_foodBurnTime, $"Duration of {prefabName} in seconds."),
                [HealthRegen] = synced.Bind(prefabName, HealthRegen, shared.m_foodRegen, $"Health regeneration of {prefabName}."),
                [Eitr] = synced.Bind(prefabName, Eitr, shared.m_foodEitr, $"Eitr value of {prefabName}."),
                [Vigor] = synced.Bind(prefabName, Vigor, 0f,
                    $"Extra Vigor of {prefabName}: percent added to stamina regeneration while the food is active, on top of its Stamina times Vigor Per Stamina Point. Does not fade with the food. Stacks with Regen Per Extra Stamina Point."),
                [EitrVigor] = synced.Bind(prefabName, EitrVigor, 0f,
                    $"Extra Eitr Vigor of {prefabName}: percent added to eitr regeneration while the food is active, on top of its Eitr times Eitr Vigor Per Eitr Point. Does not fade with the food."),
            };
            if (!string.IsNullOrEmpty(shared.m_name))
                foodsByToken[shared.m_name] = prefabName;
        }

        /// <summary>Mead sections were named 0Meads_prefab up to 3.3.5; the four old values are carried over.</summary>
        private static void BindMead(string prefabName, SE_Stats effect)
        {
            if (MeadConfigs.ContainsKey(prefabName))
                return;

            MeadEffectConfig config = new MeadEffectConfig
            {
                Duration = BindMigratedMead(prefabName, "Duration", effect.m_ttl, $"Duration of {prefabName} effect in seconds."),
                HealthOverTime = BindMigratedMead(prefabName, "HealthOverTime", effect.m_healthOverTime, $"Total health restored over time by {prefabName}."),
                StaminaOverTime = BindMigratedMead(prefabName, "StaminaOverTime", effect.m_staminaOverTime, $"Total stamina restored over time by {prefabName}."),
                EitrOverTime = BindMigratedMead(prefabName, "EitrOverTime", effect.m_eitrOverTime, $"Total eitr restored over time by {prefabName}."),
            };
            BindMeadBonuses(prefabName, effect, config);
            MeadConfigs[prefabName] = config;
            MeadsByEffectHash[effect.NameHash()] = config;
        }

        private static ConfigEntry<float> BindMigratedMead(string prefabName, string key, float defaultValue, string description)
        {
            string migrated = ConfigMigration.TakeOrphan(synced.Config, OldMeadPrefix + prefabName, prefabName, key);
            ConfigEntry<float> entry = synced.Bind(prefabName, key, defaultValue, description);
            ConfigMigration.Apply(entry, migrated);
            return entry;
        }

        private static void BindMeadBonuses(string prefabName, SE_Stats effect, MeadEffectConfig config)
        {
            config.HealthRegenMultiplier = synced.Bind(prefabName, "HealthRegenMultiplier", effect.m_healthRegenMultiplier,
                $"Health regeneration multiplier while {prefabName} lasts (1 = no change).");
            config.StaminaRegenMultiplier = synced.Bind(prefabName, "StaminaRegenMultiplier", effect.m_staminaRegenMultiplier,
                $"Stamina regeneration multiplier while {prefabName} lasts (1 = no change). Combines with the rules in section 2 like the game's own meads.");
            config.EitrRegenMultiplier = synced.Bind(prefabName, "EitrRegenMultiplier", effect.m_eitrRegenMultiplier,
                $"Eitr regeneration multiplier while {prefabName} lasts (1 = no change). Combines with the rules in section 3.");
            config.RunStaminaModifier = synced.Bind(prefabName, "RunStaminaModifier", effect.m_runStaminaDrainModifier,
                $"Added fraction of the run stamina drain while {prefabName} lasts: -0.2 makes running 20% cheaper.");
            config.JumpStaminaModifier = synced.Bind(prefabName, "JumpStaminaModifier", effect.m_jumpStaminaUseModifier,
                $"Added fraction of the jump stamina cost while {prefabName} lasts: -0.2 makes jumping 20% cheaper.");
        }

        /// <summary>Finds the config of a food item by its prefab, falling back to its localization token.</summary>
        public static bool TryGetFood(ItemDrop.ItemData item, out Dictionary<string, ConfigEntry<float>> configs)
        {
            configs = null;
            if (item == null)
                return false;

            if (item.m_dropPrefab != null && FoodConfigs.TryGetValue(item.m_dropPrefab.name, out configs))
                return true;

            return item.m_shared.m_name != null
                && foodsByToken.TryGetValue(item.m_shared.m_name, out string prefabName)
                && FoodConfigs.TryGetValue(prefabName, out configs);
        }
    }
}
