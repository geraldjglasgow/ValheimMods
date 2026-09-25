using System.Collections.Generic;
using BepInEx.Configuration;
using ItemCopies;

namespace FeastMaster
{
    /// <summary>
    /// Writes the configured values (per item value times the global multiplier) into the items' shared data and
    /// the meads' status effect assets, when the item database loads and whenever a setting changes. Tooltips
    /// and the consume code then read the configured numbers; the consume-time patches stay as a safety net.
    /// Foods are written through ItemCopies: the prefab and every live copy of its shared data (world drops, the
    /// inventory, the foods already eaten, the open container), and new copies as they appear. Meads are written
    /// into the asset and into the clone the local player is currently under.
    /// </summary>
    public static class ItemValues
    {
        private static readonly HashSet<string> foods = new HashSet<string>();
        private static readonly Dictionary<string, SE_Stats> meads = new Dictionary<string, SE_Stats>();
        private static bool suspended;

        /// <summary>
        /// Binding an entry that exists in the .cfg raises SettingChanged; while the item database is bound the
        /// handler is switched off and <see cref="ApplyAll"/> runs once at the end instead.
        /// </summary>
        public static void SuspendWhileBinding(bool suspend) => suspended = suspend;

        /// <summary>Re-applies every item on any setting change: edits in game, file reloads and server pushes.</summary>
        public static void HookConfig(ConfigFile config)
        {
            config.SettingChanged += OnSettingChanged;
            config.ConfigReloaded += (_, __) => ApplyAll();
        }

        private static void OnSettingChanged(object sender, SettingChangedEventArgs args)
        {
            if (suspended)
                return;
            // Foods and meads are both keyed by prefab name, which is also their section name.
            string section = args.ChangedSetting.Definition.Section;
            if (foods.Contains(section))
                ApplyFood(section);
            else if (meads.TryGetValue(section, out SE_Stats effect))
                ApplyMead(section, effect);
            else
                ApplyAll();
        }

        public static void RegisterFood(string prefabName) => foods.Add(prefabName);

        public static void RegisterMead(string prefabName, SE_Stats effect) => meads[prefabName] = effect;

        public static void ApplyAll()
        {
            Copies.ApplyAll(ApplyNamed);
            foreach (KeyValuePair<string, SE_Stats> mead in meads)
                ApplyMead(mead.Key, mead.Value);
            Rested.ApplyAll();
            RefreshEatenFood();
        }

        /// <summary>One food: its prefab and every live copy, then the player's food update.</summary>
        private static void ApplyFood(string prefabName)
        {
            if (!FeastMasterData.FoodConfigs.TryGetValue(prefabName, out Dictionary<string, ConfigEntry<float>> configs))
                return;
            Copies.Apply(prefabName, shared => Apply(shared, configs));
            RefreshEatenFood();
        }

        /// <summary>The ItemCopies.ApplyAll write: foods get their values, every other item is left alone.</summary>
        private static void ApplyNamed(string prefabName, ItemDrop.ItemData.SharedData shared)
        {
            if (FeastMasterData.FoodConfigs.TryGetValue(prefabName, out Dictionary<string, ConfigEntry<float>> configs))
                Apply(shared, configs);
        }

        /// <summary>The ItemCopies.HookSpawns callback: a new world item or an item entering an inventory.</summary>
        public static void ApplyCopy(ItemDrop.ItemData item)
        {
            if (FeastMasterData.TryGetFood(item, out Dictionary<string, ConfigEntry<float>> configs))
                Apply(item.m_shared, configs);
        }

        /// <summary>Writes the configured values of one food into its shared data.</summary>
        public static void Apply(ItemDrop.ItemData.SharedData shared, Dictionary<string, ConfigEntry<float>> configs)
        {
            shared.m_food = configs[FeastMasterData.Health].Value * FeastMasterData.HealthModifier.Value;
            shared.m_foodStamina = configs[FeastMasterData.Stamina].Value * FeastMasterData.StaminaModifier.Value;
            shared.m_foodBurnTime = configs[FeastMasterData.Duration].Value * FeastMasterData.DurationModifier.Value;
            shared.m_foodRegen = configs[FeastMasterData.HealthRegen].Value * FeastMasterData.HealthRegenModifier.Value;
            shared.m_foodEitr = configs[FeastMasterData.Eitr].Value * FeastMasterData.EitrModifier.Value;
        }

        /// <summary>
        /// The eaten foods point at their own shared data copies, written above; the game recomputes max health,
        /// stamina and eitr from them only on its next food tick, so it is forced now (the call the game itself
        /// makes after eating; UpdateFood is private, the assembly is publicized).
        /// </summary>
        private static void RefreshEatenFood()
        {
            Player player = Player.m_localPlayer;
            if (player != null && player.GetFoods().Count > 0)
                player.UpdateFood(0f, true);
        }

        /// <summary>One mead: its status effect asset, then the clone the local player may be under right now.</summary>
        private static void ApplyMead(string prefabName, SE_Stats effect)
        {
            if (effect == null || !FeastMasterData.MeadConfigs.TryGetValue(prefabName, out MeadEffectConfig config))
                return;
            Apply(effect, config);
            RefreshActiveMead(effect.NameHash(), config);
        }

        /// <summary>
        /// SEMan.AddStatusEffect clones the asset onto the player, so the running effect is a separate object;
        /// the configured values are written into that clone too, keeping its progress through the duration.
        /// </summary>
        private static void RefreshActiveMead(int nameHash, MeadEffectConfig config)
        {
            Player player = Player.m_localPlayer;
            if (player == null)
                return;
            foreach (StatusEffect active in player.GetSEMan().GetStatusEffects())
            {
                if (active is SE_Stats stats && stats.NameHash() == nameHash)
                    ApplyKeepingProgress(stats, config);
            }
        }

        /// <summary>
        /// The effect counts m_time up from 0 and ends when it passes m_ttl. The same fraction of the duration
        /// stays remaining after a Duration change: m_time is rescaled by new ttl / old ttl instead of reset.
        /// </summary>
        private static void ApplyKeepingProgress(SE_Stats stats, MeadEffectConfig config)
        {
            float elapsedFraction = stats.m_ttl > 0f ? stats.m_time / stats.m_ttl : 0f;
            Apply(stats, config);
            stats.m_time = elapsedFraction * stats.m_ttl;
        }

        /// <summary>Writes the configured values of one mead into its status effect (the asset or a clone).</summary>
        public static void Apply(SE_Stats effect, MeadEffectConfig config)
        {
            effect.m_ttl = config.Duration.Value;
            effect.m_healthOverTime = config.HealthOverTime.Value;
            effect.m_staminaOverTime = config.StaminaOverTime.Value;
            effect.m_eitrOverTime = config.EitrOverTime.Value;
            effect.m_healthRegenMultiplier = config.HealthRegenMultiplier.Value;
            effect.m_staminaRegenMultiplier = config.StaminaRegenMultiplier.Value;
            effect.m_eitrRegenMultiplier = config.EitrRegenMultiplier.Value;
            effect.m_runStaminaDrainModifier = config.RunStaminaModifier.Value;
            effect.m_jumpStaminaUseModifier = config.JumpStaminaModifier.Value;
        }

        /// <summary>
        /// The Vigor of a food in percent: its own Vigor entry plus its configured Stamina times Vigor Per Stamina
        /// Point, scaled by Vigor Multiplier. Every term is read live, so all three hot reload. 0 for unknown items.
        /// </summary>
        public static float VigorOf(ItemDrop.ItemData item)
        {
            if (!FeastMasterData.TryGetFood(item, out Dictionary<string, ConfigEntry<float>> configs))
                return 0f;
            float stamina = configs[FeastMasterData.Stamina].Value * FeastMasterData.StaminaModifier.Value;
            float derived = stamina * Settings.VigorPerStaminaPoint.Value;
            return (configs[FeastMasterData.Vigor].Value + derived) * Settings.VigorMultiplier.Value;
        }

        /// <summary>The Eitr Vigor of a food in percent, built like <see cref="VigorOf"/> from its configured Eitr.</summary>
        public static float EitrVigorOf(ItemDrop.ItemData item)
        {
            if (!FeastMasterData.TryGetFood(item, out Dictionary<string, ConfigEntry<float>> configs))
                return 0f;
            float eitr = configs[FeastMasterData.Eitr].Value * FeastMasterData.EitrModifier.Value;
            float derived = eitr * Settings.EitrVigorPerEitrPoint.Value;
            return (configs[FeastMasterData.EitrVigor].Value + derived) * Settings.EitrVigorMultiplier.Value;
        }
    }
}
