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
    /// creatures count too.
    /// </summary>
    internal static class ServerRun
    {
        private static readonly Dictionary<int, bool> Kinds = new Dictionary<int, bool>();

        /// <summary>True on the server (dedicated or hosting) while the setting is on.</summary>
        public static bool Active => Settings.ServerRunsCreatures.Value && ZNet.instance != null && ZNet.instance.IsServer();

        public static bool Is(ZDO zdo)
        {
            int prefab = zdo.GetPrefab();
            if (!Kinds.TryGetValue(prefab, out bool runs))
            {
                runs = Decide(prefab);
                Kinds[prefab] = runs;
            }
            return runs;
        }

        private static bool Decide(int prefab)
        {
            GameObject go = ZNetScene.instance != null ? ZNetScene.instance.GetPrefab(prefab) : null;
            if (go == null || go.GetComponent<Player>() != null || go.GetComponent<Fish>() != null)
            {
                return false;
            }
            return go.GetComponent<BaseAI>() != null || go.GetComponent<SpawnSystem>() != null
                || go.GetComponent<CreatureSpawner>() != null || go.GetComponent<SpawnArea>() != null
                || go.GetComponent<TriggerSpawner>() != null || go.GetComponent<OfferingBowl>() != null;
        }
    }
}
