using System;
using System.Collections.Generic;
using UnityEngine;

namespace HighSeat
{
    /// <summary>
    /// What the server runs: every prefab with a creature mind (the game's BaseAI: monsters, animals, tamed ones, bosses)
    /// and every spawner (a zone's SpawnSystem, which also spawns the game's raids, through <see cref="ServerSpawning"/>;
    /// nests, through <see cref="ActiveArea"/>; spawn points; trigger spawners; boss altars), so what they spawn starts on
    /// the server. Only a console spawn still begins on the typing player's game, handed over on the next pass. Fish stay
    /// with players: a hooked fish is claimed by the angler. Decided once per prefab, by its components, so another mod's
    /// creatures count too. With "Creatures only where shared" on (<see cref="WhereShared"/>) creatures are told apart from
    /// spawners, because only a creature goes to a lone player (<see cref="SharedRule"/>), and boss altars from the other
    /// spawners, because the ground round an altar is loaded in full (<see cref="FullSectors"/>).
    /// </summary>
    internal static class ServerRun
    {
        [Flags]
        private enum Kind : byte
        {
            None = 0,
            Creature = 1,
            Spawner = 2,
            Altar = 4,
        }

        private static readonly Dictionary<int, Kind> Kinds = new Dictionary<int, Kind>();

        /// <summary>True on the server (dedicated or hosting) while the setting is on.</summary>
        public static bool Active => Settings.ServerRunsCreatures.Value && ZNet.instance != null && ZNet.instance.IsServer();

        /// <summary>True on the server while "Creatures only where shared" is on as well.</summary>
        public static bool WhereShared => Active && Settings.OnlyWhereShared.Value;

        public static bool Is(ZDO zdo) => KindOf(zdo) != Kind.None;

        public static bool IsCreature(ZDO zdo) => (KindOf(zdo) & Kind.Creature) != 0;

        public static bool IsAltar(ZDO zdo) => (KindOf(zdo) & Kind.Altar) != 0;

        /// <summary>A prefab carrying one of the game's spawners.</summary>
        public static bool IsSpawner(GameObject go) => go.GetComponent<SpawnSystem>() != null
            || go.GetComponent<CreatureSpawner>() != null || go.GetComponent<SpawnArea>() != null
            || go.GetComponent<TriggerSpawner>() != null || go.GetComponent<OfferingBowl>() != null;

        private static Kind KindOf(ZDO zdo)
        {
            int prefab = zdo.GetPrefab();
            if (!Kinds.TryGetValue(prefab, out Kind kind))
            {
                kind = Decide(prefab);
                Kinds[prefab] = kind;
            }
            return kind;
        }

        private static Kind Decide(int prefab)
        {
            GameObject go = ZNetScene.instance != null ? ZNetScene.instance.GetPrefab(prefab) : null;
            if (go == null || go.GetComponent<Player>() != null || go.GetComponent<Fish>() != null)
            {
                return Kind.None;
            }
            if (go.GetComponent<BaseAI>() != null)
            {
                return Kind.Creature;
            }
            if (go.GetComponent<OfferingBowl>() != null)
            {
                return Kind.Spawner | Kind.Altar;
            }
            return IsSpawner(go) ? Kind.Spawner : Kind.None;
        }
    }
}
