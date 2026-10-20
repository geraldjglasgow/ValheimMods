using System.Collections.Generic;
using UnityEngine;

namespace EliteCreaturesReborn.Arena.Trial
{
    /// <summary>
    /// Each creature's home biome, read from the game's own open-world spawn lists (the zone controller's
    /// <c>SpawnSystem</c>, and a live one's when another mod added lists there): every switched-on entry for a creature
    /// that fits the arena - hostile to players, on land, no boss, nothing passive (<see cref="Fits"/>). A creature's home
    /// is the first biome of the trial's order that an entry with no required key names it in; failing that, the first
    /// that an entry naming a single biome does (the Mistlands' Ticks and Brood come only after the Queen, the Deep
    /// North's Jotun after the first is killed). So the late-game invasions of earlier biomes - Skeletons, Draugr, Fulings,
    /// Seekers and the Charred in the Meadows once their bosses fall - stay in their own biome's waves, and an entry naming
    /// every biome at once (a list for events, not the open world) is no home at all. Not for a hot path: once a session.
    /// </summary>
    internal static class ArenaSpawnLists
    {
        /// <summary>Creature prefab name to its home's place in <paramref name="order"/>.</summary>
        public static Dictionary<string, int> Homes(IReadOnlyList<Heightmap.Biome> order)
        {
            Heightmap.Biome all = Heightmap.Biome.None;
            foreach (Heightmap.Biome biome in order)
            {
                all |= biome;
            }
            Dictionary<string, Heightmap.Biome> open = new Dictionary<string, Heightmap.Biome>();
            Dictionary<string, Heightmap.Biome> keyed = new Dictionary<string, Heightmap.Biome>();
            foreach (SpawnSystem.SpawnData data in Entries())
            {
                Note(data, all, open, keyed);
            }
            Dictionary<string, int> homes = new Dictionary<string, int>();
            AddHomes(open, order, homes);
            AddHomes(keyed, order, homes); // only where no open entry gave one
            return homes;
        }

        /// <summary>The creature a prefab name makes, when it is a networked creature this game knows that fits the
        /// arena; null otherwise.</summary>
        public static Character? Creature(string name)
        {
            GameObject? prefab = ZNetScene.instance != null ? ZNetScene.instance.GetPrefab(name) : null;
            bool fits = prefab != null && prefab.GetComponent<ZNetView>() != null && Fits(prefab);
            return fits ? prefab!.GetComponent<Character>() : null;
        }

        /// <summary>A creature that fights players on land: a monster's mind (no animal that flees), no boss, not a
        /// water dweller, not one that waits to be provoked, of a faction hostile to players, one that picks targets at
        /// all, and one a player's blows can hurt. The Deep North villages' spirits (ShadowPerson) fail the last two: a
        /// decoration that wanders, ignores every damage and never fights (GitHub #37).</summary>
        public static bool Fits(GameObject prefab)
        {
            Character? body = prefab.GetComponent<Character>();
            MonsterAI? ai = prefab.GetComponent<MonsterAI>();
            if (body == null || ai == null || body.m_boss || ai.m_avoidLand || ai.m_aggravatable
                || body.m_aiCannotTargetOthers || Unhurt(body.m_damageModifiers))
            {
                return false;
            }
            Character.Faction faction = body.m_faction;
            return faction != Character.Faction.Players && faction != Character.Faction.Dverger
                && faction != Character.Faction.PlayerSpawned && faction != Character.Faction.TrainingDummy;
        }

        // Every damage a player's weapon or staff deals to a creature ignored or shrugged off: only the console kills it.
        private static bool Unhurt(HitData.DamageModifiers mods)
        {
            HitData.DamageModifier[] taken =
            {
                mods.m_blunt, mods.m_slash, mods.m_pierce, mods.m_fire, mods.m_frost, mods.m_lightning, mods.m_poison,
                mods.m_spirit,
            };
            return System.Array.TrueForAll(taken,
                m => m == HitData.DamageModifier.Immune || m == HitData.DamageModifier.Ignore);
        }

        private static void Note(SpawnSystem.SpawnData data, Heightmap.Biome all, Dictionary<string, Heightmap.Biome> open,
            Dictionary<string, Heightmap.Biome> keyed)
        {
            Heightmap.Biome mask = data.m_biome & all;
            if (!data.m_enabled || data.m_prefab == null || mask == Heightmap.Biome.None || mask == all
                || !Fits(data.m_prefab))
            {
                return;
            }
            string name = data.m_prefab.name;
            if (string.IsNullOrEmpty(data.m_requiredGlobalKey))
            {
                open[name] = (open.TryGetValue(name, out Heightmap.Biome had) ? had : Heightmap.Biome.None) | mask;
            }
            else if (Single(mask))
            {
                keyed[name] = (keyed.TryGetValue(name, out Heightmap.Biome had) ? had : Heightmap.Biome.None) | mask;
            }
        }

        private static void AddHomes(Dictionary<string, Heightmap.Biome> masks, IReadOnlyList<Heightmap.Biome> order,
            Dictionary<string, int> homes)
        {
            foreach (KeyValuePair<string, Heightmap.Biome> entry in masks)
            {
                for (int i = 0; i < order.Count && !homes.ContainsKey(entry.Key); i++)
                {
                    if ((entry.Value & order[i]) != Heightmap.Biome.None)
                    {
                        homes[entry.Key] = i;
                    }
                }
            }
        }

        private static bool Single(Heightmap.Biome mask)
        {
            int bits = (int)mask;
            return bits != 0 && (bits & (bits - 1)) == 0;
        }

        // Every spawn entry of the game's open-world lists, each list once: the zone controller prefab's, and the first
        // live spawn system's when it carries lists the prefab does not.
        private static IEnumerable<SpawnSystem.SpawnData> Entries()
        {
            HashSet<SpawnSystemList> seen = new HashSet<SpawnSystemList>();
            foreach (SpawnSystem? system in Systems())
            {
                foreach (SpawnSystemList list in system != null ? system.m_spawnLists : new List<SpawnSystemList>())
                {
                    if (list == null || !seen.Add(list))
                    {
                        continue;
                    }
                    foreach (SpawnSystem.SpawnData data in list.m_spawners)
                    {
                        yield return data;
                    }
                }
            }
        }

        private static IEnumerable<SpawnSystem?> Systems()
        {
            GameObject? zoneCtrl = ZoneSystem.instance != null ? ZoneSystem.instance.m_zoneCtrlPrefab : null;
            yield return zoneCtrl != null ? zoneCtrl.GetComponent<SpawnSystem>() : null;
            yield return SpawnSystem.m_instances.Count > 0 ? SpawnSystem.m_instances[0] : null;
        }
    }
}
