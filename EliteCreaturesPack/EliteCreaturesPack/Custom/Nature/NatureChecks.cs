using System.Linq;
using EliteCreaturesPack.Custom.Build;

namespace EliteCreaturesPack.Custom.Nature
{
    /// <summary>
    /// What can only be judged on the finished creature, so at the last pass of its chain: lines that are valid alone but
    /// can never do anything together with the rest. Each is a warning; the creature is built as written. Only lines a
    /// definition of the chain wrote are judged, never what the base brings.
    /// </summary>
    internal static class NatureChecks
    {
        public static void Leaf(CreatureBuild build, Character character)
        {
            if (!build.IsLeaf)
            {
                return;
            }
            BossFight.CheckLeaf(build, character);
            CheckFeeding(build);
        }

        /// <summary>The game tames by feeding and breeds only the fed, and only a monster's mind eats.</summary>
        private static void CheckFeeding(CreatureBuild build)
        {
            if (!NeedsFood(build))
            {
                return;
            }
            MonsterAI ai = build.Shell.GetComponent<MonsterAI>();
            if (ai == null)
            {
                build.Report.Warn("only a monster's mind (MonsterAI) eats, and its base has none: it can never be fed, so never tamed, bred or healed by eating", "taming");
            }
            else if (ai.m_consumeItems == null || ai.m_consumeItems.Count == 0)
            {
                build.Report.Warn("it eats nothing (behaviour: eats), so it can never be fed: taming, breeding and eat heal need it", "behaviour.eats");
            }
        }

        /// <summary>Whether a definition asked for something only eating brings: taming (unless it is born tame),
        /// breeding (only a fed tame creature breeds) or healing by eating.</summary>
        private static bool NeedsFood(CreatureBuild build)
        {
            Tameable tameable = build.Shell.GetComponent<Tameable>();
            bool bornTame = tameable != null && tameable.m_startsTamed;
            bool tamesByFood = !bornTame && tameable != null && build.Chain.Any(d => d.Taming?.Tameable == true);
            bool breeds = build.Shell.GetComponent<Procreation>() != null && build.Chain.Any(d => d.Taming?.Breeds == true);
            return tamesByFood || breeds || build.Chain.Any(d => d.Behaviour?.EatHeal > 0f);
        }
    }
}
