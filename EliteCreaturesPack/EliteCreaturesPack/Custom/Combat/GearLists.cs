using System;
using System.Collections.Generic;
using System.Linq;
using EliteCreaturesPack.Custom.Build;
using EliteCreaturesPack.Custom.Definitions;
using UnityEngine;

namespace EliteCreaturesPack.Custom.Combat
{
    /// <summary>
    /// A definition's <c>gear:</c> on the creature's Humanoid lists (<see cref="CarriedItems"/>), handed out by the game
    /// as each creature starts, alike on every peer from its seed.
    /// <list type="bullet">
    /// <item><c>replace: true</c> empties the base's lists first; otherwise the gear comes on top of the base's. A human's
    /// stand-in kit (a club and rags, so a human given nothing still fights) is not this step's: the human step takes it
    /// off in the chain's first pass when any definition of the chain gives gear (<c>HumanStep</c>), so it goes exactly
    /// once and never takes anything else with it.</item>
    /// <item><c>always</c> joins <c>m_defaultItems</c>, after the base's.</item>
    /// <item><c>pick one from</c> (one item from each list) and <c>one set from</c> (one set) are rolled through the game's
    /// own random sets (<c>m_randomSets</c>, of which the game gives one): every combination becomes a set, so the one
    /// roll picks an item from each list and one set, independently and evenly. With gear added, the base's own sets join
    /// the combinations, so the base still gets one of its sets as before. The base's random weapon, armour and shield
    /// lists and chance items are left as they are. More than <see cref="MostSets"/> combinations fail the creature.</item>
    /// <item>An item must be an item of the game or a mod (<see cref="PrefabLookup.Item"/>, a creature's attack item
    /// included). One the item database does not know (some creatures' attack items) is given as the creature's own
    /// copy, which goes into the database (<see cref="PartItems"/>), so it can be held and drawn.</item>
    /// </list>
    /// </summary>
    internal static class GearLists
    {
        public const int MostSets = 10000;

        public static void Apply(CreatureBuild build, Humanoid humanoid, GearBlock gear)
        {
            GameObject[]? always = Items(build, gear.Always, "gear.always");
            List<GameObject[][]>? choices = Choices(build, gear);
            if (always == null || choices == null)
            {
                return;
            }
            if (gear.Replace)
            {
                CarriedItems.Clear(humanoid);
            }
            CarriedItems.Always(humanoid, always);
            if (choices.Count > 0)
            {
                Combine(build, humanoid, choices);
            }
        }

        /// <summary>Each pick list as a choice of single items, then the sets as one choice of sets; null after a fail.</summary>
        private static List<GameObject[][]>? Choices(CreatureBuild build, GearBlock gear)
        {
            List<GameObject[]>? picks = Lists(build, gear.PickOneFrom, "gear.pick one from");
            List<GameObject[]>? sets = Lists(build, gear.OneSetFrom, "gear.one set from");
            if (picks == null || sets == null)
            {
                return null;
            }
            List<GameObject[][]> choices = picks.Select(list => list.Select(item => new[] { item }).ToArray()).ToList();
            if (sets.Count > 0)
            {
                choices.Add(sets.ToArray());
            }
            return choices;
        }

        /// <summary>Each list of names as its items; null after a fail.</summary>
        private static List<GameObject[]>? Lists(CreatureBuild build, List<List<string>> lists, string field)
        {
            List<GameObject[]> found = new List<GameObject[]>(lists.Count);
            for (int i = 0; i < lists.Count; i++)
            {
                GameObject[]? items = Items(build, lists[i], $"{field}[{i}]");
                if (items == null)
                {
                    return null;
                }
                found.Add(items);
            }
            return found;
        }

        /// <summary>Every combination of the base's sets (if any) and the choices, as the creature's random sets.</summary>
        private static void Combine(CreatureBuild build, Humanoid humanoid, List<GameObject[][]> choices)
        {
            GameObject[][] own = (humanoid.m_randomSets ?? new Humanoid.ItemSet[0]).Select(set => set.m_items ?? new GameObject[0]).ToArray();
            if (own.Length > 0)
            {
                choices.Insert(0, own);
            }
            // counted up to one past the most, so many long lists never overflow the count
            long count = choices.Aggregate(1L, (total, choice) => Math.Min(total * choice.Length, MostSets + 1L));
            if (count > MostSets)
            {
                build.Report.Fail($"its gear makes more than {MostSets:#,0} combinations of picks and sets: use fewer or shorter lists", "gear");
                return;
            }
            IEnumerable<GameObject[]> combos = new[] { new GameObject[0] };
            foreach (GameObject[][] choice in choices)
            {
                combos = combos.SelectMany(combo => choice.Select(option => combo.Concat(option).Where(item => item != null).ToArray())).ToList();
            }
            humanoid.m_randomSets = combos.Select((items, i) => new Humanoid.ItemSet { m_name = $"{build.Creature.Name} gear {i + 1}", m_items = items }).ToArray();
        }

        /// <summary>The named items, each found (or its own copy when the database lacks it); null after a fail.</summary>
        private static GameObject[]? Items(CreatureBuild build, IReadOnlyList<string> names, string field)
        {
            List<GameObject> items = new List<GameObject>(names.Count);
            foreach (string name in names)
            {
                GameObject? item = build.Find.Item(name);
                if (item == null)
                {
                    build.Report.Fail($"unknown item '{name}'", field);
                    return null;
                }
                items.Add(InDatabase(item) ? item : OwnItems.Make(build, item));
            }
            return items.ToArray();
        }

        private static bool InDatabase(GameObject item) =>
            ObjectDB.instance != null && ObjectDB.instance.GetItemPrefab(item.name) == item;
    }
}
