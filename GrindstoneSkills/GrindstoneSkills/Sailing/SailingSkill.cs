using UnityEngine;

namespace GrindstoneSkills
{
    /// <summary>
    /// Sailing, a skill of GrindstoneSkills' own (<see cref="CustomSkill"/>): the game has none. Its type number is the
    /// stable hash of <see cref="Keys.SailingSkillName"/>. Levels live with the character like every skill
    /// (client-side); each player's client also publishes its level to the player's own ZDO under
    /// <see cref="Keys.SailingLevel"/>, so a ship's owner can read the helmsman's level. The icon is the Karve's.
    /// </summary>
    public static class SailingSkill
    {
        public const string Name = "Sailing";
        public const float MaxLevel = CustomSkill.MaxLevel;

        private static readonly string[] IconShips = { "Karve", "VikingShip", "Raft" };

        public static readonly CustomSkill Skill = new CustomSkill(Keys.SailingSkillName, Name,
            "Health of the ships you build, speed of the ships you steer, and how far you explore at sea.",
            Keys.SailingLevel, ShipIcon);

        public static Skills.SkillType Type => Skill.Type;

        public static bool Active => SailingSettings.Enabled.Value;

        /// <summary>The local player's Sailing level, 0 when there is no local player.</summary>
        public static float Local() => Skill.Local();

        /// <summary>Any player's Sailing level: the local player's own skill, anyone else's from their ZDO.</summary>
        public static float Of(Player player) => Skill.Of(player);

        /// <summary>A perk's share at a level: <paramref name="percentAt100"/> percent at level 100, linear from 0.</summary>
        public static float Share(float percentAt100, float level) =>
            Mathf.Max(0f, percentAt100) / 100f * Mathf.Clamp01(level / MaxLevel);

        private static Sprite ShipIcon()
        {
            ZNetScene scene = ZNetScene.instance;
            if (scene == null)
                return null;
            foreach (string name in IconShips)
            {
                GameObject prefab = scene.GetPrefab(name);
                Piece piece = prefab != null ? prefab.GetComponent<Piece>() : null;
                if (piece != null && piece.m_icon != null)
                    return piece.m_icon;
            }
            return null;
        }
    }
}
