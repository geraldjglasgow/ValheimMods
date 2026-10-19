using Translations;
using UnityEngine;

namespace GrindstoneSkills
{
    /// <summary>
    /// Rend, the Axes milestone at level 50, on the attacker's client (<see cref="MeleeMastery"/>): each axe hit on the
    /// same foe within <see cref="Window"/> seconds of the last deals <see cref="Step"/> more than the one before, up to
    /// <see cref="MaxSteps"/> steps (first hit +0%, second +5%, sixth and later +25%).
    /// <list type="bullet">
    /// <item>The chain is kept on the attacker's client, the only place that knows the attacker's own hits: a few slots
    /// of target, last hit time and steps, held by the target's Unity instance id (no reference kept). A slot whose
    /// last hit is older than the window starts over, and a new foe takes the slot hit longest ago, so the memory never
    /// grows. Every axe hit the client sends counts, blocked or not.</item>
    /// <item>The bonus is HitData.ApplyModifier before the hit leaves; the target's owner (Character.RPC_Damage) applies
    /// its resistances and armour after, and the stronger hit also adds more stagger, as any stronger hit does.</item>
    /// <item>Trees, logs and stumps never come here: they are not characters, and the game gives most axe hits on them
    /// the Woodcutting skill.</item>
    /// </list>
    /// </summary>
    public static class AxesRend
    {
        private const float Step = 0.05f;
        private const int MaxSteps = 5;
        private const float Window = 3f;
        private const int Slots = 8;

        private struct Chain
        {
            public int Target;
            public float LastHit;
            public int Steps;
        }

        private static readonly Chain[] chains = new Chain[Slots];

        public static void OnHit(Character target, HitData hit)
        {
            int steps = Next(target.GetInstanceID(), Time.time);
            if (steps > 0)
                hit.ApplyModifier(1f + Step * steps);
        }

        /// <summary>Records a hit on the target now and returns its steps: one more than its last hit's while the chain holds.</summary>
        private static int Next(int target, float now)
        {
            int slot = SlotOf(target);
            Chain chain = chains[slot];
            bool holds = chain.Target == target && now - chain.LastHit <= Window;
            int steps = holds ? Mathf.Min(chain.Steps + 1, MaxSteps) : 0;
            chains[slot] = new Chain { Target = target, LastHit = now, Steps = steps };
            return steps;
        }

        /// <summary>The target's slot, or else the slot hit longest ago (an unused slot first).</summary>
        private static int SlotOf(int target)
        {
            int oldest = 0;
            for (int i = 0; i < chains.Length; i++)
            {
                if (chains[i].Target == target)
                    return i;
                if (chains[i].LastHit < chains[oldest].LastHit)
                    oldest = i;
            }
            return oldest;
        }

        public static void Perk(SkillPage page) => page.Perk(Lang.Get("gs_perk_rend"), MeleeMastery.Level,
            Lang.Format("gs_mastery_rend_tip", SkillPage.Duration(Window), SkillPage.Percent(Step), SkillPage.Percent(Step * MaxSteps), MaxSteps));
    }
}
