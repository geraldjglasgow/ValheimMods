using System.Collections.Generic;
using UnityEngine;

namespace GrindstoneSkills
{
    /// <summary>
    /// Catch Up's memory (section 6): the best base level each of the local player's skills ever reached, the game's,
    /// the mod's own and any other mod's alike. A base level is the stored Skills.Skill.m_level, without food or mead
    /// bonuses, kept whole as the game shows it. Skills and Player.m_customData live on the player's own client and are
    /// saved with the character (Player.Save), so everything here runs there and follows the character between worlds.
    /// <para>The saved text (<see cref="BestLevelsText"/>) is read once, when a character is taken: the first time a
    /// player's skills ask for it, and at the latest when the game spawns the local player (Player.OnSpawned runs after
    /// its data is loaded). A character without it is seeded from its current levels; a level above its saved best (one
    /// earned without the mod) raises that best. After that the levels are kept in memory and written back only when one
    /// changes. A new Player is made for every spawn and every character picked in the main menu, so one character
    /// never sees another's best levels.</para>
    /// </summary>
    public static class BestLevels
    {
        private static readonly Dictionary<Skills.SkillType, int> best = new Dictionary<Skills.SkillType, int>();
        private static Player owner;

        /// <summary>The best base level the player's skill ever reached; 0 when none is known.</summary>
        public static int Of(Player player, Skills.SkillType type)
        {
            Take(player);
            return best.TryGetValue(type, out int level) ? level : 0;
        }

        /// <summary>A skill's base level, whole: the stored level without bonuses; 0 for a skill never trained.</summary>
        public static int BaseLevel(Skills skills, Skills.SkillType type) =>
            skills != null && skills.m_skillData.TryGetValue(type, out Skills.Skill skill) ? Clamp((int)skill.m_level) : 0;

        /// <summary>A whole level within the game's 0..100.</summary>
        internal static int Clamp(int level) => Mathf.Clamp(level, 0, (int)Skills.c_MaxSkillLevel);

        /// <summary>Reads the player's saved best levels unless they are the ones in memory, seeding them when missing.</summary>
        internal static void Take(Player player)
        {
            if (ReferenceEquals(player, owner) || player == null)
                return;
            owner = player;
            best.Clear();
            bool saved = player.m_customData.TryGetValue(Keys.BestLevels, out string text);
            if (saved)
                BestLevelsText.Parse(text, best);
            if (RaiseToCurrent(player.GetSkills()) || !saved)
                Save(player);
        }

        /// <summary>A skill reached a new level (the game's level-up): raises its best when the level is above it.</summary>
        internal static void Note(Player player, Skills.SkillType type, float level)
        {
            Take(player);
            if (Raise(type, Clamp((int)level)))
                Save(player);
        }

        /// <summary>Raises every best level to the skill's current level where that is higher (the console's raiseskill).</summary>
        internal static void CatchCurrent(Player player)
        {
            Take(player);
            if (RaiseToCurrent(player.GetSkills()))
                Save(player);
        }

        /// <summary>Forgets one skill's best level, so it starts again from its current level (the console's resetskill).</summary>
        internal static void Forget(Player player, Skills.SkillType type)
        {
            Take(player);
            if (best.Remove(type))
                Save(player);
        }

        /// <summary>Forgets every best level (the console's resetcharacter).</summary>
        internal static void ForgetAll(Player player)
        {
            Take(player);
            if (best.Count == 0)
                return;
            best.Clear();
            Save(player);
        }

        private static bool RaiseToCurrent(Skills skills)
        {
            if (skills == null)
                return false;
            bool changed = false;
            foreach (KeyValuePair<Skills.SkillType, Skills.Skill> skill in skills.m_skillData)
                changed |= Raise(skill.Key, Clamp((int)skill.Value.m_level));
            return changed;
        }

        private static bool Raise(Skills.SkillType type, int level)
        {
            if (level <= 0 || (best.TryGetValue(type, out int known) && known >= level))
                return false;
            best[type] = level;
            return true;
        }

        private static void Save(Player player) => player.m_customData[Keys.BestLevels] = BestLevelsText.Format(best);
    }
}
