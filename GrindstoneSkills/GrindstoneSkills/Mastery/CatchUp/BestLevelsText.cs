using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace GrindstoneSkills
{
    /// <summary>
    /// The saved form of the best levels (<see cref="Keys.BestLevels"/> in the player's own custom data): "type:level"
    /// pairs joined by commas, each the skill type's number and a whole level in invariant culture ("102:45,1:30"). The
    /// number is the one the game saves skills under, so the mod's own skills and other mods' skills fit as well. Read
    /// once when <see cref="BestLevels"/> takes a character and written only when a best level changes, never per
    /// experience gain; a pair it cannot read is skipped. Player.m_customData lives on the player's own client and is
    /// saved with the character, so this runs there.
    /// </summary>
    internal static class BestLevelsText
    {
        private const char PairSeparator = ',';
        private const char LevelSeparator = ':';

        private static readonly StringBuilder text = new StringBuilder();

        /// <summary>Adds every readable pair of <paramref name="saved"/> to <paramref name="into"/>, levels 1..100.</summary>
        internal static void Parse(string saved, Dictionary<Skills.SkillType, int> into)
        {
            if (string.IsNullOrEmpty(saved))
                return;
            foreach (string pair in saved.Split(PairSeparator))
            {
                if (TryRead(pair, out Skills.SkillType type, out int level))
                    into[type] = level;
            }
        }

        /// <summary>The saved form of every best level above 0.</summary>
        internal static string Format(Dictionary<Skills.SkillType, int> levels)
        {
            text.Length = 0;
            foreach (KeyValuePair<Skills.SkillType, int> best in levels)
            {
                if (best.Value <= 0)
                    continue;
                if (text.Length > 0)
                    text.Append(PairSeparator);
                text.Append(((int)best.Key).ToString(CultureInfo.InvariantCulture));
                text.Append(LevelSeparator);
                text.Append(best.Value.ToString(CultureInfo.InvariantCulture));
            }
            return text.ToString();
        }

        private static bool TryRead(string pair, out Skills.SkillType type, out int level)
        {
            type = Skills.SkillType.None;
            level = 0;
            int colon = pair.IndexOf(LevelSeparator);
            if (colon <= 0)
                return false;
            if (!int.TryParse(pair.Substring(0, colon), NumberStyles.Integer, CultureInfo.InvariantCulture, out int number))
                return false;
            if (!int.TryParse(pair.Substring(colon + 1), NumberStyles.Integer, CultureInfo.InvariantCulture, out level))
                return false;
            type = (Skills.SkillType)number;
            level = BestLevels.Clamp(level);
            return level > 0;
        }
    }
}
