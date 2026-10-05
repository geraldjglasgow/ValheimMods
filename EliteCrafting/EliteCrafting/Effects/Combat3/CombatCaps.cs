using System;
using EliteCrafting.Rules;

namespace EliteCrafting.Effects.Combat3
{
    /// <summary>
    /// The most another peer accepts from a hit or a player's ZDO for the item-local combat values it applies (the
    /// target's owner: penetration, paralysis length), from the running, server-synced rules: the largest unconditional
    /// cap plus the largest health-critical cap among the channels of the effect, in the applied unit. 0 when the rules
    /// define no inscription on it, so a value the server does not allow is never applied. Refreshed once per rules
    /// generation.
    /// </summary>
    internal static class CombatCaps
    {
        private static int _generation = -1;
        private static float _penetration;
        private static float _paralyzeSeconds;

        /// <summary>Share of a resistance a hit may bypass (never more than all of it).</summary>
        public static float Penetration
        {
            get
            {
                Refresh();
                return _penetration;
            }
        }

        public static float ParalyzeSeconds
        {
            get
            {
                Refresh();
                return _paralyzeSeconds;
            }
        }

        private static void Refresh()
        {
            ChannelPlan plan = ChannelPlan.Current;
            if (plan.Generation == _generation)
            {
                return;
            }
            _penetration = Math.Min(1f, CapOf(plan, EffectKind.Penetration));
            _paralyzeSeconds = CapOf(plan, EffectKind.Paralyze);
            _generation = plan.Generation;
        }

        private static float CapOf(ChannelPlan plan, EffectKind kind)
        {
            float normal = 0f, critical = 0f;
            for (int c = 0; c < plan.Count; c++)
            {
                if (plan.Kinds[c] != kind)
                {
                    continue;
                }
                ChannelDef channel = plan.Channels[c];
                float cap = AggregateBuilder.Scale(channel, channel.Cap);
                if (channel.Condition == AffixCondition.HealthCritical)
                {
                    critical = Math.Max(critical, cap);
                }
                else
                {
                    normal = Math.Max(normal, cap);
                }
            }
            return normal + critical;
        }
    }
}
