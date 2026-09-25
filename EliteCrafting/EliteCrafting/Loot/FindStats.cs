using System;
using System.Collections.Generic;
using EliteCrafting.Rules;

namespace EliteCrafting.Loot
{
    /// <summary>The four loot-find stats (drops.md section 10, effects-runtime.md section 7), in key order.</summary>
    public enum FindStat
    {
        /// <summary>Norns' Favour, effect <c>find_rarity</c>: rarity weights above the lowest magic rarity.</summary>
        Rarity,

        /// <summary>Fateweaver, effect <c>find_stones</c>: the stone chance of a kill.</summary>
        Stones,

        /// <summary>Trophy Taker, effect <c>find_trophy</c>: the creature's own trophy drop chance.</summary>
        Trophy,

        /// <summary>Hoardfinder, effect <c>find_coins</c>: the creature's own coin and valuable drop chances.</summary>
        Coins,
    }

    /// <summary>
    /// The player ZDO keys of the loot-find stats and the effect ids that feed them. A killer's client writes its own
    /// totals (<see cref="FindPublisher"/>); the dying creature's owner reads them (<see cref="KillerStats"/>).
    /// </summary>
    public static class FindKeys
    {
        public const int Count = 4;

        /// <summary>Effect ids, index = <see cref="FindStat"/>.</summary>
        public static readonly string[] Effects = { "find_rarity", "find_stones", "find_trophy", "find_coins" };

        /// <summary>Float keys on the player's own ZDO, index = <see cref="FindStat"/>. Absent reads as 0.</summary>
        public static readonly string[] Keys = { "ecf_find_rarity", "ecf_find_stones", "ecf_find_trophy", "ecf_find_coins" };

        public static readonly int[] Hashes =
        {
            Keys[0].GetStableHashCode(), Keys[1].GetStableHashCode(), Keys[2].GetStableHashCode(), Keys[3].GetStableHashCode(),
        };

        /// <summary>The stat an effect id feeds, or -1.</summary>
        public static int StatOf(string effectId) => Array.IndexOf(Effects, effectId);
    }

    /// <summary>
    /// The affix channels of one rules generation that feed a find stat, resolved once per generation so neither the
    /// publisher nor the death roll compares effect ids as strings. Only unconditional channels count: a
    /// health-critical find affix would depend on the killer's health at the moment of the kill, which the creature's
    /// owner cannot see (judgement call, DECISIONS IMP-73). Main thread only.
    /// </summary>
    internal sealed class FindChannels
    {
        private static FindChannels _current = new FindChannels(RuleSet.Empty);

        private FindChannels(RuleSet rules)
        {
            Generation = rules.Generation;
            IReadOnlyList<ChannelDef> channels = rules.Affixes.Channels;
            StatOf = new int[channels.Count];
            Cap = new float[FindKeys.Count];
            for (int i = 0; i < channels.Count; i++)
            {
                ChannelDef channel = channels[i];
                int stat = channel.Condition == AffixCondition.None ? FindKeys.StatOf(channel.Effect.Id) : -1;
                StatOf[i] = stat;
                if (stat >= 0)
                {
                    Cap[stat] = Math.Max(Cap[stat], channel.Cap);
                }
            }
        }

        public int Generation { get; }

        /// <summary>Channel index → <see cref="FindStat"/> index, or -1 for every other channel.</summary>
        public int[] StatOf { get; }

        /// <summary>
        /// Per stat, the largest cap among its channels (infinity when uncapped); 0 when no affix in the running rules
        /// feeds it, so a stat the server's rules do not define reads as 0 whatever a player's ZDO says.
        /// </summary>
        public float[] Cap { get; }

        public static FindChannels Current
        {
            get
            {
                RuleSet rules = ActiveRules.Current;
                if (_current.Generation != rules.Generation)
                {
                    _current = new FindChannels(rules);
                }
                return _current;
            }
        }

        /// <summary>A published value made safe for the roll: not negative, not above what the rules allow.</summary>
        public float Clamp(int stat, float value) => float.IsNaN(value) ? 0f : Math.Max(0f, Math.Min(value, Cap[stat]));
    }
}
