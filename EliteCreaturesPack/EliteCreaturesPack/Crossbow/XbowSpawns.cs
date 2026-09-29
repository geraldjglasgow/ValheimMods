using UnityEngine;

namespace EliteCreaturesPack.Crossbow
{
    /// <summary>
    /// Where crossbowmen come from: wherever and whenever the game spawns its Black Forest archer skeleton
    /// (<see cref="XbowKind"/>), that spawn is a crossbowman instead at the settings' `Share`, like any skeleton unit:
    /// the wild spawns (at night once Bonemass is dead), the fixed spawners of burial chambers and the like, and bone
    /// piles. Decided by whoever runs the
    /// spawner, the only machine that spawns anything. The spawn keeps its levels; only which creature comes changes.
    /// </summary>
    public static class XbowSpawns
    {
        /// <summary>The spawn data for a crossbowman in place of this wild spawn, or null to let it be.</summary>
        public static SpawnSystem.SpawnData? Wild(SpawnSystem.SpawnData critter)
        {
            GameObject? crossbowman = Instead(critter.m_prefab);
            if (crossbowman == null)
            {
                return null;
            }
            SpawnSystem.SpawnData data = critter.Clone();
            data.m_prefab = crossbowman;
            return data;
        }

        /// <summary>The bone pile's pick, or a crossbowman with the same levels in its place.</summary>
        public static SpawnArea.SpawnData BonePile(SpawnArea.SpawnData picked)
        {
            GameObject? crossbowman = picked == null ? null : Instead(picked.m_prefab);
            if (crossbowman == null)
            {
                return picked!;
            }
            return new SpawnArea.SpawnData
            {
                m_prefab = crossbowman, m_weight = picked!.m_weight, m_minLevel = picked.m_minLevel, m_maxLevel = picked.m_maxLevel,
            };
        }

        /// <summary>The crossbowman to spawn in place of this skeleton, at the settings' share, or null to let it be.</summary>
        public static GameObject? Instead(GameObject? prefab)
        {
            XbowKind? kind = XbowKind.OfSkeleton(prefab);
            if (kind?.Prefab == null || !XbowSettings.On || XbowSettings.Share <= 0f)
            {
                return null;
            }
            return Random.value < XbowSettings.Share ? kind.Prefab : null;
        }
    }
}
