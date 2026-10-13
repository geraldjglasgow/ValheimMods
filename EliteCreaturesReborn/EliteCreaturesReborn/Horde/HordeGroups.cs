using System.Collections.Generic;
using EliteCreaturesReborn.Util;
using UnityEngine;

namespace EliteCreaturesReborn.Horde
{
    /// <summary>
    /// Makes the world's spawn groups larger for one pass of the game's spawn list, then puts them back. The game's
    /// <c>SpawnData</c> entries are shared by every spawn system, so they are grown in place only for the length of one
    /// <c>SpawnSystem.UpdateSpawnList</c> call (<see cref="HordeSpawnPatch"/>) and restored by its finalizer, whatever
    /// happens in between: the group size range and the most of the kind allowed around the players, times the spawn
    /// multiplier, each rounded up or down at random so a fraction averages right. No allocation once warm.
    /// </summary>
    internal static class HordeGroups
    {
        private struct Saved
        {
            public int Max;
            public int GroupMin;
            public int GroupMax;
        }

        private static readonly List<Saved> Originals = new List<Saved>();
        private static List<SpawnSystem.SpawnData>? _grown;

        public static void Grow(List<SpawnSystem.SpawnData> spawners, float factor)
        {
            Originals.Clear();
            _grown = spawners; // first, so a failure part way still restores what was grown
            foreach (SpawnSystem.SpawnData data in spawners)
            {
                Originals.Add(new Saved
                {
                    Max = data.m_maxSpawned, GroupMin = data.m_groupSizeMin, GroupMax = data.m_groupSizeMax,
                });
                GrowOne(data, factor);
            }
        }

        // Backwards, so an entry listed twice ends with its first, true values.
        public static void Restore()
        {
            List<SpawnSystem.SpawnData>? grown = _grown;
            _grown = null;
            if (grown == null)
            {
                return;
            }
            for (int i = Mathf.Min(grown.Count, Originals.Count) - 1; i >= 0; i--)
            {
                Saved saved = Originals[i];
                grown[i].m_maxSpawned = saved.Max;
                grown[i].m_groupSizeMin = saved.GroupMin;
                grown[i].m_groupSizeMax = saved.GroupMax;
            }
            Originals.Clear();
        }

        // A most-allowed of 0 means no limit and stays so.
        private static void GrowOne(SpawnSystem.SpawnData data, float factor)
        {
            if (data.m_maxSpawned > 0)
            {
                data.m_maxSpawned = Mathf.Max(1, Dice.Round(data.m_maxSpawned * factor));
            }
            data.m_groupSizeMin = Dice.Round(data.m_groupSizeMin * factor);
            data.m_groupSizeMax = Mathf.Max(data.m_groupSizeMin, Dice.Round(data.m_groupSizeMax * factor));
        }
    }
}
