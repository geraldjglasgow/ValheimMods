using System;
using System.Collections.Generic;
using System.Linq;
using EliteCreaturesPack.Custom.Build;

namespace EliteCreaturesPack.Custom.Nature
{
    /// <summary>
    /// `boss fight`: the name of one of the game's events, put in <c>Character.m_bossEvent</c>. Every two seconds each
    /// peer asks its enemy HUD for the boss on screen and forces that boss's event (<c>RandEventSystem.GetForcedEvent</c>),
    /// whose weather and music then play. A forced event is the active one, so an event with creatures of its own (a
    /// raid) spawns them too: the game's boss events (<c>boss_eikthyr</c>...) have none. Only a boss is ever on that HUD,
    /// so the fight plays only for a creature with <c>boss: true</c>. The name is checked against the game's events
    /// (<c>RandEventSystem.m_events</c>), case ignored, and stored as the game writes it, since the game matches it exactly;
    /// an unknown one leaves the creature out.
    /// </summary>
    internal static class BossFight
    {
        private const string Field = "character.boss fight";

        public static void Apply(CreatureBuild build, Character character, string fight)
        {
            List<RandomEvent>? events = RandEventSystem.instance != null ? RandEventSystem.instance.m_events : null;
            if (events == null)
            {
                build.Report.Warn($"the game's events are not loaded, so '{fight}' is not checked", Field);
                character.m_bossEvent = fight;
                return;
            }
            RandomEvent? known = events.FirstOrDefault(e => e != null && string.Equals(e.m_name, fight, StringComparison.OrdinalIgnoreCase));
            if (known == null)
            {
                build.Report.Fail($"'{fight}' is not one of the game's events: {Names(events)}", Field);
                return;
            }
            Caution(build, known);
            character.m_bossEvent = known.m_name;
        }

        /// <summary>At the last pass: a boss fight that can never play, because the finished creature is not a boss.</summary>
        public static void CheckLeaf(CreatureBuild build, Character character)
        {
            if (!character.m_boss && build.Chain.Any(definition => definition.Character?.BossFight != null))
            {
                build.Report.Warn("its boss fight plays only while a boss is on screen, and it is not a boss (boss: true)", Field);
            }
        }

        private static void Caution(CreatureBuild build, RandomEvent fight)
        {
            if (!fight.m_enabled)
            {
                build.Report.Warn($"the game's event '{fight.m_name}' is switched off, so its weather and music never play", Field);
            }
            if (fight.m_spawn != null && fight.m_spawn.Count > 0)
            {
                build.Report.Warn($"the event '{fight.m_name}' brings creatures of its own, which spawn while this boss is on screen", Field);
            }
        }

        private static string Names(List<RandomEvent> events) =>
            string.Join(", ", events.Where(e => e != null).Select(e => e.m_name).Distinct().OrderBy(name => name));
    }
}
