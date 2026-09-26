using System.Collections.Generic;
using EliteCreaturesReborn.Rules;
using EliteCreaturesReborn.Runtime;
using EliteCreaturesReborn.Traits;
using UnityEngine;

namespace EliteCreaturesReborn.Loot
{
    /// <summary>
    /// A boss aspect's loot multiplier. In every loot mode but Vanilla it is one more factor in the engine's final
    /// multiply; in Vanilla - where the loot rules are off and nothing else touches the drops - it is applied here on
    /// its own, because an aspect that pays must keep paying with the loot rules off (`configuration.md`). Either way it
    /// only touches the boss's own table and leaves trophies to the trophy switch, like every other multiplier.
    /// </summary>
    internal static class AspectLoot
    {
        public static float Factor(EliteController controller) =>
            RuleState.Active.Boss.Aspects.LootOf(controller.Traits.Aspect);

        /// <summary>Vanilla mode's whole treatment: scale the boss's own rows by its aspect, and nothing more.</summary>
        public static void ApplyAlone(CharacterDrop drop, EliteController controller, List<KeyValuePair<GameObject, int>> result)
        {
            float factor = Factor(controller);
            if (controller.Traits.Aspect == Aspect.None || Mathf.Approximately(factor, 1f))
            {
                return;
            }
            HashSet<GameObject> own = new HashSet<GameObject>();
            foreach (CharacterDrop.Drop row in drop.m_drops)
            {
                if (row.m_prefab != null)
                {
                    own.Add(row.m_prefab);
                }
            }
            bool trophies = RuleState.Active.Loot.MultiplyTrophies;
            for (int i = 0; i < result.Count; i++)
            {
                if (own.Contains(result[i].Key) && (trophies || !DropRoller.IsTrophy(result[i].Key)))
                {
                    result[i] = new KeyValuePair<GameObject, int>(result[i].Key, DropRoller.Scaled(result[i].Value, factor));
                }
            }
        }
    }
}
