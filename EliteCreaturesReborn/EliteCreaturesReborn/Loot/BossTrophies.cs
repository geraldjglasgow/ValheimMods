using System.Collections.Generic;
using EliteCreaturesReborn.Rules;
using EliteCreaturesReborn.Runtime;
using EliteCreaturesReborn.Traits;
using UnityEngine;

namespace EliteCreaturesReborn.Loot
{
    /// <summary>
    /// A boss's own trophies, the one drop no loot rule touches: a boss with N stars drops exactly N+1 of each
    /// trophy in its own drop table - one head for the plain fight, five for a four-star one - in every loot mode,
    /// Vanilla included. A head per star is a count the group can read off the nameplate before the fight, not a
    /// quantity to roll or multiply, so it is the whole rule: no `drops` line, extra roll, global or boss
    /// multiplier, other aspect's factor or trophy switch changes it, and like an aspect's pay it stays when the loot
    /// rules are off. Bountiful alone scales it, since it pays more of everything: N+1 times its own `loot` (x2 by
    /// default, so a four-star Bountiful boss drops ten heads), rounded. The table decides, not the game's roll: a
    /// trophy row in the boss's table pays N+1 even if its
    /// chance (1 on every vanilla boss) came up empty. The engine holds these rows out of the list before any rule
    /// runs and pays them back last, so nothing in between can scale or reroll them; a row the rule file adds for
    /// the same item still adds on top. The exception is a trophy the boss's `creatures:` entry names under `drop
    /// overrides`: the server's own words, left to the engine as before. Every boss that dies in its own right pays
    /// its own - each Twin - while a Phantom copy, which is no boss kill and drops nothing, never does.
    /// </summary>
    internal sealed class BossTrophies
    {
        /// <summary>Nothing held: not a boss, or no trophy in its table left to this rule.</summary>
        public static readonly BossTrophies None = new BossTrophies(new List<GameObject>(), 0);

        private readonly List<GameObject> _heads;
        private readonly int _count;

        private BossTrophies(List<GameObject> heads, int count)
        {
            _heads = heads;
            _count = count;
        }

        /// <summary>
        /// Takes a boss's own trophies out of the drop list so the loot rules run without them, and remembers what
        /// to pay back. <paramref name="overridden"/> names the rows the boss's `creatures:` entry overrides (null
        /// when the rules are off); a trophy among them stays in the list for the engine.
        /// </summary>
        public static BossTrophies Hold(CharacterDrop drop, EliteController controller,
            List<KeyValuePair<GameObject, int>> result, HashSet<string>? overridden)
        {
            if (!Pays(controller))
            {
                return None;
            }
            List<GameObject> heads = TableTrophies(drop, overridden);
            if (heads.Count == 0)
            {
                return None;
            }
            result.RemoveAll(pair => heads.Contains(pair.Key));
            return new BossTrophies(heads, Count(controller.Traits));
        }

        /// <summary>N+1 heads, times Bountiful's own loot multiplier when the boss carries it.</summary>
        private static int Count(CreatureTraits traits)
        {
            float heads = traits.Stars + 1;
            if (traits.HasAspect(Aspect.Bountiful))
            {
                heads *= RuleState.Active.Boss.Aspects.LootOf(Aspect.Bountiful);
            }
            return Mathf.Clamp(Mathf.RoundToInt(heads), 1, DropRoller.AmountCap);
        }

        /// <summary>True for a trophy held here, which the engine's rerolls pass by.</summary>
        public bool Holds(GameObject prefab) => _heads.Contains(prefab);

        /// <summary>Last of all, after every rule and multiplier: N+1 of each held trophy, scaled by nothing.</summary>
        public void Pay(List<KeyValuePair<GameObject, int>> result)
        {
            foreach (GameObject head in _heads)
            {
                DropRoller.Add(result, head, _count);
            }
        }

        /// <summary>A boss that died in its own right; never a Phantom copy, whose drops are off anyway.</summary>
        private static bool Pays(EliteController controller) =>
            controller.Creature != null && controller.Creature.IsBoss() && !controller.Traits.PhantomCopy;

        /// <summary>Each distinct trophy in the boss's own table that no `drop overrides` row names.</summary>
        private static List<GameObject> TableTrophies(CharacterDrop drop, HashSet<string>? overridden)
        {
            List<GameObject> heads = new List<GameObject>();
            foreach (CharacterDrop.Drop row in drop.m_drops)
            {
                if (row.m_prefab != null && DropRoller.IsTrophy(row.m_prefab) && !heads.Contains(row.m_prefab)
                    && (overridden == null || !overridden.Contains(row.m_prefab.name)))
                {
                    heads.Add(row.m_prefab);
                }
            }
            return heads;
        }
    }
}
