using System;
using EliteCrafting.Rules;

namespace EliteCrafting.Effects
{
    /// <summary>
    /// The player-global totals another peer needs, published on the player's own ZDO (effects-runtime.md section 7,
    /// the pattern of the loot-find keys): the creature's owner reads Dazing Blows and Beast Whisperer, the ship's
    /// owner Fair Winds, the resource's owner Deep Vein, Heartwood and Harvester, and every client Hearthlight and
    /// Mistbane. Written by the player's own client (it owns its player ZDO) at the end of a rebuild, only when a
    /// value changed, so normal play sends nothing. The unconditional totals only: a health-critical copy would depend
    /// on health the reader cannot see. A reader clamps what it reads to the running rules' cap (a client cannot
    /// publish more than the server's rules allow), and a player without the key reads 0.
    /// </summary>
    internal static class PlayerStats
    {
        public const int Daze = 0, Light = 1, Demist = 2, Taming = 3, Sail = 4, YieldMining = 5, YieldLumber = 6, Harvest = 7;

        private static readonly EffectKind[] Kinds =
        {
            EffectKind.StaggerDurationDealt, EffectKind.LightAura, EffectKind.DemistRadius, EffectKind.TamingSpeed,
            EffectKind.SailSpeed, EffectKind.YieldMining, EffectKind.YieldLumber, EffectKind.YieldPickable,
        };

        private static readonly int[] Hashes =
        {
            "ecf_daze".GetStableHashCode(), "ecf_light".GetStableHashCode(), "ecf_demist".GetStableHashCode(),
            "ecf_taming".GetStableHashCode(), "ecf_sail".GetStableHashCode(), "ecf_yield_mining".GetStableHashCode(),
            "ecf_yield_lumber".GetStableHashCode(), "ecf_harvest".GetStableHashCode(),
        };

        private static readonly float[] Caps = new float[Kinds.Length];
        private static int _capsGeneration = -1;

        /// <summary>End of a rebuild on the local player's client.</summary>
        public static void Publish(Player player)
        {
            ZDO? zdo = player.m_nview != null && player.m_nview.IsValid() && player.m_nview.IsOwner() ? player.m_nview.GetZDO() : null;
            if (zdo == null)
            {
                return;
            }
            AggregateValues v = AggregateBuilder.Normal;
            for (int i = 0; i < Kinds.Length; i++)
            {
                float value = ItemEffects.Enabled ? v[Kinds[i]] : 0f;
                if (zdo.GetFloat(Hashes[i], 0f) != value)
                {
                    zdo.Set(Hashes[i], value);
                }
            }
        }

        /// <summary>A player's published total, clamped to the running rules.</summary>
        public static float Of(Player? player, int stat)
        {
            ZDO? zdo = player != null && player.m_nview != null ? player.m_nview.GetZDO() : null;
            return zdo == null ? 0f : Clamp(stat, zdo.GetFloat(Hashes[stat], 0f));
        }

        /// <summary>The published total of the hit's attacker (0 for a hit without one or from a non-player).</summary>
        public static float OfAttacker(HitData hit, int stat)
        {
            if (hit.m_attacker.IsNone() || ZDOMan.instance == null)
            {
                return 0f;
            }
            ZDO? zdo = ZDOMan.instance.GetZDO(hit.m_attacker);
            return zdo == null ? 0f : Clamp(stat, zdo.GetFloat(Hashes[stat], 0f));
        }

        private static float Clamp(int stat, float value)
        {
            RefreshCaps();
            return float.IsNaN(value) ? 0f : Math.Max(0f, Math.Min(value, Caps[stat]));
        }

        // Per rules generation: the largest cap (in the published unit) among the channels feeding each stat; 0 when the
        // running rules have no affix on it, so a stat the server does not define reads as 0 whatever a ZDO says.
        private static void RefreshCaps()
        {
            ChannelPlan plan = ChannelPlan.Current;
            if (plan.Generation == _capsGeneration)
            {
                return;
            }
            Array.Clear(Caps, 0, Caps.Length);
            for (int c = 0; c < plan.Count; c++)
            {
                int stat = Array.IndexOf(Kinds, plan.Kinds[c]);
                ChannelDef channel = plan.Channels[c];
                if (stat >= 0 && channel.Condition == AffixCondition.None)
                {
                    Caps[stat] = Math.Max(Caps[stat], AggregateBuilder.Scale(channel, channel.Cap));
                }
            }
            _capsGeneration = plan.Generation;
        }
    }
}
