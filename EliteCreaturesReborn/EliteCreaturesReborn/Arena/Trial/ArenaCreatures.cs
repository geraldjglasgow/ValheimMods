using System.Collections.Generic;
using EliteCreaturesReborn.Util;
using UnityEngine;

namespace EliteCreaturesReborn.Arena.Trial
{
    /// <summary>
    /// The creatures each biome of the trial sends (features/arena.md section 3, "Creatures"): its hostile land creatures
    /// as the game's own spawn lists name them (<see cref="ArenaSpawnLists"/>), so another mod's creatures come too, or
    /// the fixed list (<see cref="ArenaFallback"/>) when the game's lists give a biome none. A kind as tough as the middle
    /// one of its biome or weaker comes as often as any; a tougher one less often, by the square root of the health ratio,
    /// so a Troll comes now and then among the Greydwarves and a Morgen among the Charred. Built once a session, on the
    /// first wave planned; every peer
    /// would build the same, and the queue names kinds by prefab hash, so a new owner needs none of it to carry on.
    /// </summary>
    internal static class ArenaCreatures
    {
        /// <summary>The trial's biomes in its order, wave 1 to 80.</summary>
        private static readonly Heightmap.Biome[] Order =
        {
            Heightmap.Biome.Meadows, Heightmap.Biome.BlackForest, Heightmap.Biome.Swamp, Heightmap.Biome.Mountain,
            Heightmap.Biome.Plains, Heightmap.Biome.Mistlands, Heightmap.Biome.AshLands, Heightmap.Biome.DeepNorth,
        };

        private static List<Kind>[]? _lists;
        private static ZNetScene? _builtFor;

        /// <summary>One kind a biome sends, and how often beside the others.</summary>
        public readonly struct Kind
        {
            public Kind(int prefab, float weight)
            {
                Prefab = prefab;
                Weight = weight;
            }

            public int Prefab { get; }

            public float Weight { get; }
        }

        /// <summary>The trial's biomes in order.</summary>
        public static IReadOnlyList<Heightmap.Biome> Biomes => Order;

        /// <summary>The biome at a place of the trial (0 Meadows to 7 Deep North).</summary>
        public static Heightmap.Biome Biome(int index) => Order[Mathf.Clamp(index, 0, Order.Length - 1)];

        /// <summary>The kinds the biome at <paramref name="index"/> sends; empty when it has none this game knows.</summary>
        public static List<Kind> For(int index)
        {
            Ensure();
            return _lists![Mathf.Clamp(index, 0, Order.Length - 1)];
        }

        /// <summary>A kind drawn by weight; 0 from an empty list.</summary>
        public static int Draw(List<Kind> kinds)
        {
            float total = 0f;
            foreach (Kind kind in kinds)
            {
                total += kind.Weight;
            }
            float roll = Random.Range(0f, total);
            foreach (Kind kind in kinds)
            {
                if ((roll -= kind.Weight) <= 0f)
                {
                    return kind.Prefab;
                }
            }
            return kinds.Count > 0 ? kinds[kinds.Count - 1].Prefab : 0;
        }

        private static void Ensure()
        {
            if (_lists != null && ReferenceEquals(_builtFor, ZNetScene.instance))
            {
                return;
            }
            _builtFor = ZNetScene.instance;
            Dictionary<string, int> homes = ArenaSpawnLists.Homes(Order);
            _lists = new List<Kind>[Order.Length];
            for (int i = 0; i < Order.Length; i++)
            {
                List<string> names = Names(homes, i);
                _lists[i] = Weigh(names);
                Log.Info($"Hel's Trial: {Order[i]} sends {string.Join(", ", names)}");
            }
        }

        // The names whose home is this biome, or the fixed list when the game's spawn lists give it none.
        private static List<string> Names(Dictionary<string, int> homes, int index)
        {
            List<string> names = new List<string>();
            foreach (KeyValuePair<string, int> home in homes)
            {
                if (home.Value == index)
                {
                    names.Add(home.Key);
                }
            }
            return names.Count > 0 ? names : ArenaFallback.For(index);
        }

        private static List<Kind> Weigh(List<string> names)
        {
            List<Character> bodies = new List<Character>();
            List<float> healths = new List<float>();
            foreach (string name in names)
            {
                Character? body = ArenaSpawnLists.Creature(name);
                if (body != null)
                {
                    bodies.Add(body);
                    healths.Add(Mathf.Max(1f, body.m_health));
                }
            }
            healths.Sort();
            float middle = healths.Count > 0 ? healths[(healths.Count - 1) / 2] : 1f;
            List<Kind> kinds = new List<Kind>(bodies.Count);
            foreach (Character body in bodies)
            {
                float weight = Mathf.Min(1f, Mathf.Sqrt(middle / Mathf.Max(1f, body.m_health)));
                kinds.Add(new Kind(body.gameObject.name.GetStableHashCode(), weight));
            }
            return kinds;
        }
    }
}
