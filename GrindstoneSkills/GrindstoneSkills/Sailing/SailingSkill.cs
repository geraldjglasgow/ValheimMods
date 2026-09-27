using UnityEngine;

namespace GrindstoneSkills
{
    /// <summary>
    /// Sailing, a skill of GrindstoneSkills' own: the game has none. Its SkillType is a number outside the game's enum,
    /// the stable hash of <see cref="Keys.SailingSkillName"/>, so it cannot meet a game skill or a small number another
    /// mod picked. Levels live with the character like every skill (client-side). A player's client also writes its
    /// level to the player's own ZDO (<see cref="SailingXp"/>), so a ship's owner can read the helmsman's level.
    /// </summary>
    public static class SailingSkill
    {
        public const string Name = "Sailing";
        public const float MaxLevel = 100f;

        public static readonly Skills.SkillType Type = (Skills.SkillType)(Keys.SailingSkillName.GetStableHashCode() & 0x7FFFFFFF);

        /// <summary>The word the game looks up for the skill's name: "$skill_" + the type's name, lower case (a number here).</summary>
        public static readonly string LocalizationKey = "skill_" + Type.ToString().ToLowerInvariant();

        /// <summary>The definition every Skills component gets. The icon is filled in once the game's prefabs are loaded.</summary>
        public static readonly Skills.SkillDef Definition = new Skills.SkillDef
        {
            m_skill = Type,
            m_description = "Health of the ships you build, speed of the ships you steer, and how far you explore at sea.",
            m_increseStep = 1f,
        };

        public static bool Active => SailingSettings.Enabled.Value;

        /// <summary>The local player's Sailing level, 0 when there is no local player.</summary>
        public static float Local()
        {
            Player player = Player.m_localPlayer;
            return player == null ? 0f : player.GetSkillLevel(Type);
        }

        /// <summary>Any player's Sailing level: the local player's own skill, anyone else's from their ZDO.</summary>
        public static float Of(Player player)
        {
            if (player == null)
                return 0f;
            if (player == Player.m_localPlayer)
                return player.GetSkillLevel(Type);
            ZNetView nview = player.m_nview;
            return nview != null && nview.IsValid() ? Mathf.Max(0f, nview.GetZDO().GetFloat(Keys.SailingLevel)) : 0f;
        }

        /// <summary>A perk's share at a level: <paramref name="percentAt100"/> percent at level 100, linear from 0.</summary>
        public static float Share(float percentAt100, float level) =>
            Mathf.Max(0f, percentAt100) / 100f * Mathf.Clamp01(level / MaxLevel);
    }
}
