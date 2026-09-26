using System.Collections.Generic;
using EliteCreaturesReborn.Rules;
using EliteCreaturesReborn.Util;

namespace EliteCreaturesReborn.Runtime
{
    /// <summary>
    /// The world tier as this machine sees it: how many of the listed bosses the world carries a defeat key for. It is
    /// derived, never stored. The server owns the game's global keys and already sends them to every client on join and
    /// on every change, so the client that owns and rolls a creature reads the same tier as the server without a
    /// message of the mod's own. With tiers off, or before the world is up, the tier is 0.
    /// </summary>
    public static class WorldTier
    {
        public static int Current()
        {
            TierRules rules = RuleState.Active.Tiers;
            ZoneSystem zones = ZoneSystem.instance;
            if (!rules.Enabled || zones == null)
            {
                return 0;
            }
            int tier = 0;
            foreach (string key in rules.BossKeys)
            {
                if (zones.GetGlobalKey(key))
                {
                    tier++;
                }
            }
            return tier;
        }

        /// <summary>The highest tier the listed bosses can raise the world to; 0 with tiers off.</summary>
        public static int Ceiling()
        {
            TierRules rules = RuleState.Active.Tiers;
            return rules.Enabled ? rules.BossKeys.Count : 0;
        }

        /// <summary>True when the world carries this defeat key.</summary>
        public static bool IsDefeated(string key) => ZoneSystem.instance != null && ZoneSystem.instance.GetGlobalKey(key);

        /// <summary>
        /// Every boss the game knows that sets a defeat key when it dies, vanilla and modded, as key to prefab name - the
        /// name the rule file's `per boss` block matches on. This is how `elite tier` names the listed keys and how a
        /// mistyped key gets noticed.
        /// </summary>
        public static Dictionary<string, string> KnownBosses()
        {
            Dictionary<string, string> bosses = new Dictionary<string, string>();
            if (ZNetScene.instance == null)
            {
                return bosses;
            }
            foreach (UnityEngine.GameObject prefab in ZNetScene.instance.m_prefabs)
            {
                Character? boss = prefab != null ? prefab.GetComponent<Character>() : null;
                if (boss != null && boss.m_boss && !string.IsNullOrEmpty(boss.m_defeatSetGlobalKey))
                {
                    bosses[boss.m_defeatSetGlobalKey.ToLowerInvariant()] = prefab!.name;
                }
            }
            return bosses;
        }

        /// <summary>Logs every listed key that no known boss sets, once per world start, so a typo does not sit silent.</summary>
        public static void WarnUnknownKeys()
        {
            Dictionary<string, string> known = KnownBosses();
            if (known.Count == 0)
            {
                return;
            }
            foreach (string key in RuleState.Active.Tiers.BossKeys)
            {
                if (!known.ContainsKey(key))
                {
                    Log.Warn($"world tiers: no boss this game knows sets '{key}', so it only counts if something else sets it");
                }
            }
        }
    }
}
