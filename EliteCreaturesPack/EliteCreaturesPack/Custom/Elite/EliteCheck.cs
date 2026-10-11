using System;
using System.Collections.Generic;
using EliteCreaturesLink;
using EliteCreaturesPack.Custom.Build;
using EliteCreaturesPack.Custom.Definitions;

namespace EliteCreaturesPack.Custom.Elite
{
    /// <summary>
    /// Checks a creature's merged elite lines on its own pass, with Elite Creatures Reborn there, against ECR's names and
    /// the finished creature (features/custom-creatures.md section 8). Every problem is a warning at the line's file and
    /// line, never a failure: the creature loads, and only what cannot work is left out - a name ECR does not know, the
    /// aspect line of a creature not marked as a boss, a portal attack it does not have or that is not thrown
    /// (<see cref="EliteAttacks"/>), a summon that is not a creature. An aspect whose abilities the creature lacks is kept
    /// and warned about, since it still rolls and simply does nothing: Portalbound without a portal attack, Echoing on a
    /// creature without attacks. Summoner without a list is fine: ECR falls back to the summons of its biome's boss.
    /// </summary>
    internal static class EliteCheck
    {
        public static EliteLines Checked(CreatureBuild build, EliteLines lines)
        {
            EliteLines result = new EliteLines
            {
                Mutations = Known(lines.Mutations, EliteTraits.MutationNames, "mutation"),
                Aspects = BossAspects(build, lines.Aspects),
                PortalAttacks = EliteAttacks.Resolve(build, lines.PortalAttacks),
                Summons = Creatures(build, lines.Summons),
            };
            WarnAbilities(build, result);
            return result;
        }

        /// <summary>The aspect line of a creature marked as a boss; any other creature ignores it (the spec), with a warning.</summary>
        private static EliteLine<string>? BossAspects(CreatureBuild build, EliteLine<string>? line)
        {
            if (line == null)
            {
                return null;
            }
            if (!IsBoss(build))
            {
                line.Warn("it is not marked as a boss, so it ignores its aspect line (`character: boss: true` makes it one)");
                return null;
            }
            return Known(line, EliteTraits.AspectNames, "aspect");
        }

        /// <summary>Marked as a boss by the last definition of its chain that says, else by its base (as its shell has it).</summary>
        private static bool IsBoss(CreatureBuild build)
        {
            for (int i = build.Chain.Count - 1; i >= 0; i--)
            {
                bool? boss = build.Chain[i].Character?.Boss;
                if (boss != null)
                {
                    return boss.Value;
                }
            }
            Character? body = build.Shell.GetComponent<Character>();
            return body != null && body.m_boss;
        }

        /// <summary>The names ECR knows, each once, in ECR's spelling; each unknown one warned about and left out. Unchecked
        /// when ECR answers no names at all (its endpoint failed): ECR then reports unknown names itself.</summary>
        private static EliteLine<string>? Known(EliteLine<string>? line, string[] names, string kind)
        {
            if (line == null || names.Length == 0)
            {
                return line;
            }
            List<string> kept = new List<string>();
            foreach (string name in line.Values)
            {
                string? known = Array.Find(names, candidate => string.Equals(candidate, name, StringComparison.OrdinalIgnoreCase));
                if (known == null)
                {
                    line.Warn($"'{name}' is not one of Elite Creatures Reborn's {kind}s, left out. They are: {string.Join(", ", names)}");
                }
                else if (!kept.Contains(known))
                {
                    kept.Add(known);
                }
            }
            return line.With(kept);
        }

        /// <summary>The summons that name a creature (custom ones included); any other name is warned about and left out.</summary>
        private static EliteLine<SummonEntry>? Creatures(CreatureBuild build, EliteLine<SummonEntry>? line)
        {
            if (line == null)
            {
                return null;
            }
            List<SummonEntry> kept = new List<SummonEntry>();
            for (int i = 0; i < line.Values.Count; i++)
            {
                SummonEntry entry = line.Values[i];
                if (build.Find.Creature(entry.Creature) != null)
                {
                    kept.Add(entry);
                }
                else
                {
                    line.Warn($"'{entry.Creature}' is not a creature, left out of its summons", $"{line.Field}[{i}]");
                }
            }
            return line.With(kept);
        }

        /// <summary>The aspects that need something of the creature and find nothing: kept, warned about.</summary>
        private static void WarnAbilities(CreatureBuild build, EliteLines lines)
        {
            EliteLine<string>? aspects = lines.Aspects;
            if (aspects == null)
            {
                return;
            }
            if (Names(aspects, "Portalbound") && lines.PortalAttacks == null)
            {
                aspects.Warn("Portalbound needs `elite: portal attacks` naming a projectile attack it has; without one the "
                    + "aspect never fires (the creature still spawns)");
            }
            if (Names(aspects, "Echoing") && !EliteAttacks.HasAny(build.Shell))
            {
                aspects.Warn("Echoing replays its attacks and it has none: its echo walks but never strikes");
            }
        }

        private static bool Names(EliteLine<string> line, string aspect)
        {
            foreach (string name in line.Values)
            {
                if (string.Equals(name, aspect, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
            return false;
        }
    }
}
