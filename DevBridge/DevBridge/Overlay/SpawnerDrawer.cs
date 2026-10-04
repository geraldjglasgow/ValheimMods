using UnityEngine;

namespace DevBridge.Overlay
{
    /// <summary>
    /// spawners: a CreatureSpawner (one creature at a fixed point, as in dungeons and camps) as a small red ring at that
    /// point, an orange ring at its trigger distance (it spawns only while a player is within it, and with a trigger noise
    /// only while that player is that loud) and a pink ring at its group radius when it shares a spawn group. A SpawnArea
    /// (a spawning nest) as a red ring where it spawns, a yellow ring at its near radius (its max_near count) and an
    /// orange ring at its trigger distance. The nest's far radius (its max_total count, 1000 m by default) is not drawn.
    /// </summary>
    internal static class SpawnerDrawer
    {
        private static readonly Color Spawn = new Color(1f, 0.2f, 0.2f, 0.95f);
        private static readonly Color Trigger = new Color(1f, 0.55f, 0.15f, 0.6f);
        private static readonly Color Near = new Color(1f, 0.9f, 0.2f, 0.85f);
        private static readonly Color Group = new Color(1f, 0.45f, 0.8f, 0.7f);

        internal static void Draw(OverlayArea area, Category into)
        {
            foreach (CreatureSpawner spawner in CreatureSpawner.m_creatureSpawners)
            {
                if (spawner && area.Holds(spawner.transform.position)) Creature(spawner, into);
            }
            foreach (ZNetView view in area.Views)
            {
                foreach (SpawnArea nest in PrefabParts.Of<SpawnArea>(view)) Nest(nest, into);
            }
        }

        private static void Creature(CreatureSpawner spawner, Category into)
        {
            into.Count("creature_spawners");
            if (spawner.m_triggerNoise > 0f) into.Count("noise_triggered");
            Vector3 at = spawner.transform.position + Vector3.up * 0.1f;
            into.Lines.Add(Shapes.Ring(at, 0.5f), Spawn, 0.06f);
            into.Lines.Add(Shapes.Ring(at, spawner.m_triggerDistance), Trigger, 0.04f);
            if (spawner.m_spawnGroupRadius > 0f && spawner.m_maxGroupSpawned >= 1) into.Lines.Add(Shapes.Ring(at, spawner.m_spawnGroupRadius), Group, 0.04f);
        }

        private static void Nest(SpawnArea nest, Category into)
        {
            into.Count("spawn_areas");
            Vector3 at = nest.transform.position + Vector3.up * 0.1f;
            into.Lines.Add(Shapes.Ring(at, Mathf.Max(0.3f, nest.m_spawnRadius)), Spawn, 0.06f);
            into.Lines.Add(Shapes.Ring(at, nest.m_nearRadius), Near, 0.05f);
            into.Lines.Add(Shapes.Ring(at, nest.m_triggerDistance), Trigger, 0.04f);
        }
    }
}
