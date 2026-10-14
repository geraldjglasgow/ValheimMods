using System.Collections.Generic;
using EliteCreaturesReborn.Traits;
using EliteCreaturesReborn.Util;
using UnityEngine;

namespace EliteCreaturesReborn.Arena.Trial
{
    /// <summary>
    /// The creatures each biome of the trial sends (features/arena.md section 3, "Creatures"): its hostile land creatures
    /// as the game's own spawn lists name them (<see cref="ArenaSpawnLists"/>), so another mod's creatures come too, or
    /// the fixed list (<see cref="ArenaFallback"/>) when the game's lists give a biome none. A kind as tough as the middle
    /// one of its biome or weaker comes as often as any; a tougher one less often, by the square root of the health ratio,
    /// so a Troll comes now and then among the Greydwarves and a Morgen among the Charred. A kind is an elite when ECR's
    /// own measure finds its body large (<see cref="BodySize.IsLarge"/>: trolls, bears, lox, golems, a modded giant) or
    /// its base health is over <see cref="ArenaTable.EliteHealthRatio"/> times the biome's middle one (a Wraith); a wave
    /// holds only so many (<see cref="ArenaTable.EliteCap"/>). Built once a session, on the first wave planned; every peer
    /// would build the same, and the queue names kinds by prefab hash, so a new owner needs none of it to carry on.
    /// </summary>
    internal static class ArenaCreatures
    {
        /// <summary>The trial's biomes in their order, five waves each from the trial's first biome on.</summary>
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
            public Kind(int prefab, float weight, bool elite)
            {
                Prefab = prefab;
                Weight = weight;
                Elite = elite;
            }

            public int Prefab { get; }

            public float Weight { get; }

            /// <summary>Large, or far tougher than its biome's middle kind: few to a wave.</summary>
            public bool Elite { get; }
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

        /// <summary>A kind drawn by weight, an elite only when <paramref name="elites"/> (or when the biome has nothing
        /// else); 0 from an empty list. <paramref name="elite"/> says whether the kind drawn is one.</summary>
        public static int Draw(List<Kind> kinds, bool elites, out bool elite)
        {
            elites |= !kinds.Exists(k => !k.Elite);
            float total = 0f;
            foreach (Kind kind in kinds)
            {
                total += elites || !kind.Elite ? kind.Weight : 0f;
            }
            float roll = Random.Range(0f, total);
            Kind drawn = default;
            foreach (Kind kind in kinds)
            {
                if (!elites && kind.Elite)
                {
                    continue;
                }
                drawn = kind; // the last one allowed, should the roll's rounding outlast them all
                if ((roll -= kind.Weight) <= 0f)
                {
                    break;
                }
            }
            elite = drawn.Elite;
            return drawn.Prefab;
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
                List<string> shown = new List<string>();
                _lists[i] = Weigh(Names(homes, i), shown);
                Log.Info($"Hel's Trial: {Order[i]} sends {string.Join(", ", shown)}");
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

        // The kinds of a biome, weighed and marked; each name as the log shows it ("Troll (elite)") into shown.
        private static List<Kind> Weigh(List<string> names, List<string> shown)
        {
            List<Character> bodies = Bodies(names);
            float middle = Middle(bodies);
            List<Kind> kinds = new List<Kind>(bodies.Count);
            foreach (Character body in bodies)
            {
                float health = Mathf.Max(1f, body.m_health);
                float weight = Mathf.Min(1f, Mathf.Sqrt(middle / health));
                bool elite = BodySize.IsLarge(body) || health > ArenaTable.EliteHealthRatio * middle;
                kinds.Add(new Kind(body.gameObject.name.GetStableHashCode(), weight, elite));
                shown.Add(elite ? body.gameObject.name + " (elite)" : body.gameObject.name);
            }
            return kinds;
        }

        private static List<Character> Bodies(List<string> names)
        {
            List<Character> bodies = new List<Character>();
            foreach (string name in names)
            {
                Character? body = ArenaSpawnLists.Creature(name);
                if (body != null)
                {
                    bodies.Add(body);
                }
            }
            return bodies;
        }

        // The middle base health of the biome's kinds (the lower of two middles).
        private static float Middle(List<Character> bodies)
        {
            List<float> healths = bodies.ConvertAll(b => Mathf.Max(1f, b.m_health));
            healths.Sort();
            return healths.Count > 0 ? healths[(healths.Count - 1) / 2] : 1f;
        }
    }
}
