using System.Linq;
using UnityEngine;

namespace EliteCreaturesPack.Arsenal
{
    /// <summary>
    /// Where arsenal skeletons come from: wherever and whenever the game spawns one of the skeletons they are made from
    /// (<see cref="ArsenalKind"/>: the Black Forest's Skeleton only), one spawn in four is an arsenal skeleton instead,
    /// like any skeleton unit: the wild spawns (at night once Bonemass is dead), the fixed spawners of burial chambers
    /// and the like, and bone piles. Its weapon is drawn evenly; the bow
    /// only in place of a skeleton that could have been an archer. A weapon switched off in the settings leaves that
    /// spawn a plain skeleton, so the others come no more often. Decided by whoever runs the spawner, the only machine
    /// that spawns anything. The spawn keeps its levels.
    /// </summary>
    public static class ArsenalSpawns
    {
        private const float Share = 0.25f;

        /// <summary>The spawn data for an arsenal skeleton in place of this wild spawn, or null to let it be.</summary>
        public static SpawnSystem.SpawnData? Wild(SpawnSystem.SpawnData critter)
        {
            GameObject? instead = Instead(critter.m_prefab);
            if (instead == null)
            {
                return null;
            }
            SpawnSystem.SpawnData data = critter.Clone();
            data.m_prefab = instead;
            return data;
        }

        /// <summary>The bone pile's pick, or an arsenal skeleton with the same levels in its place.</summary>
        public static SpawnArea.SpawnData BonePile(SpawnArea.SpawnData picked)
        {
            GameObject? instead = picked == null ? null : Instead(picked.m_prefab);
            if (instead == null)
            {
                return picked!;
            }
            return new SpawnArea.SpawnData
            {
                m_prefab = instead, m_weight = picked!.m_weight, m_minLevel = picked.m_minLevel, m_maxLevel = picked.m_maxLevel,
            };
        }

        /// <summary>The arsenal skeleton to spawn in place of this skeleton, one time in four, or null to let it be.</summary>
        public static GameObject? Instead(GameObject? prefab)
        {
            ArsenalKind? kind = ArsenalKind.OfSource(prefab, out bool archer);
            if (kind == null || Random.value >= Share)
            {
                return null;
            }
            ArsenalWeapon[] drawn = ArsenalWeapon.All.Where(w => archer || !w.IsBow).ToArray();
            ArsenalWeapon weapon = drawn[Random.Range(0, drawn.Length)];
            return ArsenalSettings.Spawns(weapon) && kind.Creatures.TryGetValue(weapon, out GameObject creature) ? creature : null;
        }
    }
}
