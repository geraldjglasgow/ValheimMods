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
    /// only touches the boss's own table and never sees a boss's own trophies, which <see cref="BossTrophies"/> holds out
    /// and pays as one per star plus one. A boss
    /// that carries several aspects (Bountiful and its extras) pays every one of them, multiplied together.
    /// </summary>
    internal static class AspectLoot
    {
        public static float Factor(EliteController controller)
        {
            CreatureTraits traits = controller.Traits;
            return FactorOf(new BossAspects(traits.Aspect, traits.ExtraAspects), RuleState.Active.Boss.Aspects);
        }

        /// <summary>What a fight pays: the headline's multiplier (`none`'s for the plain fight) times each extra's own.</summary>
        public static float FactorOf(BossAspects aspects, AspectRules rules)
        {
            float factor = rules.LootOf(aspects.Headline);
            foreach (Aspect extra in aspects.ExtrasInOrder())
            {
                factor *= rules.LootOf(extra);
            }
            return factor;
        }

        /// <summary>Vanilla mode's whole treatment: scale the boss's own rows by its aspects, and nothing more.</summary>
        public static void ApplyAlone(CharacterDrop drop, EliteController controller, List<KeyValuePair<GameObject, int>> result)
        {
            CreatureTraits traits = controller.Traits;
            if (traits.Aspect != Aspect.None || traits.ExtraAspects != 0)
            {
                DropRoller.ScaleOwnRows(drop, result, Factor(controller), RuleState.Active.Loot.MultiplyTrophies);
            }
        }
    }
}
