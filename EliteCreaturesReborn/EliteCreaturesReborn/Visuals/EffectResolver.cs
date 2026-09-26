using System.Collections.Generic;
using EliteCreaturesReborn.Util;
using UnityEngine;

namespace EliteCreaturesReborn.Visuals
{
    /// <summary>
    /// Turns a configured vanilla-prefab name into an actual effect prefab through ZNetScene, so a server retunes a
    /// look by editing a string. A name that resolves is used as-is; a name that does not falls back to the nearest
    /// cosmetic effect - a non-networked prefab whose name matches the role's keywords - logged once, so a mistyped or
    /// version-shifted name self-heals into a visible effect rather than an invisible hazard. Nothing here throws.
    /// </summary>
    public static class EffectResolver
    {
        public static readonly string[] Cloud = { "poison", "blob", "ooze", "puke", "gas", "spore", "smoke" };
        public static readonly string[] Blast = { "explos", "blast", "fire", "bomb", "death", "burst", "flame" };
        public static readonly string[] Warning = { "smoke", "fire", "burn", "charge", "glow", "spark" };
        // Warding and Devouring get no prefab field in the spec's power table, so they resolve by keyword only.
        public static readonly string[] Reflect = { "shield", "spark", "hit", "block", "staff" };
        // The instant-kill tell: a death/gore burst that carries its own sound, so a distant player sees and hears a creature cease.
        public static readonly string[] Devour = { "death", "gore", "corpse", "destr", "blood", "hit" };
        // Thieving gets no prefab field in the spec's power table either; the steal tell resolves by keyword only.
        public static readonly string[] Steal = { "steal", "pickpocket", "pocket", "grab", "loot", "coin" };
        // Boss aspects: Summoner's arrival tell and a Phantom copy's vanishing puff, keyword-only like the rest.
        public static readonly string[] Summon = { "spawn", "summon", "portal", "smoke", "puff" };
        public static readonly string[] Phantom = { "ghost", "wisp", "puff", "smoke", "vanish", "poof" };

        private static readonly Dictionary<string, GameObject?> Cache = new Dictionary<string, GameObject?>();

        /// <summary>
        /// The prefab for a configured name, resolving and logging at most once; null only when nothing fits. The
        /// <paramref name="setting"/> is the rule-file field the name came from (e.g. "Miasmic cloud effect"); it is named
        /// in the log when a name fails to resolve, so a silently substituted effect can be traced to the setting to fix.
        /// </summary>
        public static GameObject? Resolve(string name, string[] keywords, string setting = "")
        {
            ZNetScene scene = ZNetScene.instance;
            if (scene == null || string.IsNullOrEmpty(name))
            {
                return null;
            }
            if (Cache.TryGetValue(name, out GameObject? cached))
            {
                return cached;
            }
            GameObject? found = Find(scene, name, keywords, setting);
            Cache[name] = found;
            return found;
        }

        /// <summary>Resolves a role name (as it travels over the effect bus) to its keyword-only tell; null if unknown.</summary>
        public static GameObject? ForRole(string role)
        {
            if (role == "reflect")
            {
                return ByKeyword(role, Reflect);
            }
            if (role == "devour")
            {
                return ByKeyword(role, Devour);
            }
            if (role == "summon" || role == "phantom")
            {
                return ByKeyword(role, role == "summon" ? Summon : Phantom);
            }
            return role == "steal" ? ByKeyword(role, Steal) : null;
        }

        /// <summary>Resolves an effect purely by keyword for tells the spec gives no config field; logged once per role.</summary>
        public static GameObject? ByKeyword(string role, string[] keywords)
        {
            ZNetScene scene = ZNetScene.instance;
            if (scene == null)
            {
                return null;
            }
            if (Cache.TryGetValue(role, out GameObject? cached))
            {
                return cached;
            }
            GameObject? found = Nearest(scene, keywords);
            if (found == null)
            {
                Log.Warn($"no vanilla effect matched the {role} tell; it will not be shown");
            }
            Cache[role] = found;
            return found;
        }

        private static GameObject? Find(ZNetScene scene, string name, string[] keywords, string setting)
        {
            GameObject? direct = Usable(scene.GetPrefab(name));
            if (direct != null)
            {
                return direct;
            }
            string from = string.IsNullOrEmpty(setting) ? "" : $" (from setting '{setting}')";
            GameObject? nearest = Nearest(scene, keywords);
            Log.Warn(nearest != null
                ? $"effect prefab '{name}'{from} not found; falling back to '{nearest.name}'"
                : $"effect prefab '{name}'{from} not found and no fallback effect exists - this hazard stays invisible");
            return nearest;
        }

        private static GameObject? Nearest(ZNetScene scene, string[] keywords)
        {
            foreach (GameObject prefab in scene.m_nonNetViewPrefabs)
            {
                if (Usable(prefab) != null && MatchesAny(prefab.name, keywords))
                {
                    return prefab;
                }
            }
            return null;
        }

        private static bool MatchesAny(string prefabName, string[] keywords)
        {
            string lower = prefabName.ToLowerInvariant();
            foreach (string keyword in keywords)
            {
                if (lower.Contains(keyword))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>A prefab is usable as an effect only if it actually renders something.</summary>
        private static GameObject? Usable(GameObject? prefab)
        {
            if (prefab == null)
            {
                return null;
            }
            bool renders = prefab.GetComponentInChildren<ParticleSystem>(true) != null
                || prefab.GetComponentInChildren<Renderer>(true) != null;
            return renders ? prefab : null;
        }
    }
}
