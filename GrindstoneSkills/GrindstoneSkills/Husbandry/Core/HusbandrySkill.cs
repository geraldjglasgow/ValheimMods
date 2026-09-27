using PlateColumn;
using UnityEngine;

namespace GrindstoneSkills
{
    /// <summary>
    /// Husbandry, a skill of GrindstoneSkills' own (<see cref="CustomSkill"/>): the game has none (Riding covers mounts'
    /// speed and stamina). Its type number is the stable hash of <see cref="Keys.HusbandrySkillName"/>. Levels live with
    /// the character like every skill (client-side); each player's client also publishes its level to the player's own
    /// ZDO under <see cref="Keys.HusbandryLevel"/>, so a creature's owner can read the level of every player near it
    /// (<see cref="Keeper"/>). The icon is the user's own art (animals behind a fence), with the boar trophy as fallback.
    /// </summary>
    public static class HusbandrySkill
    {
        public const string Name = "Husbandry";
        public const float MaxLevel = CustomSkill.MaxLevel;

        private const string IconResource = "GrindstoneSkills.assets.skill_husbandry.png";
        private static readonly string[] IconItems = { "TrophyBoar", "TrophyWolf", "TrophyLox" };
        private static Sprite embedded;
        private static bool triedEmbedded;

        public static readonly CustomSkill Skill = new CustomSkill(Keys.HusbandrySkillName, Name,
            "Taming, breeding and keeping animals: faster taming, calm creatures, better offspring and more from your herd.",
            Keys.HusbandryLevel, Icon);

        public static Skills.SkillType Type => Skill.Type;

        public static bool Active => HusbandrySettings.Enabled.Value;

        /// <summary>The local player's Husbandry level, 0 when there is no local player.</summary>
        public static float Local() => Skill.Local();

        /// <summary>Any player's Husbandry level: the local player's own skill, anyone else's from their ZDO.</summary>
        public static float Of(Player player) => Skill.Of(player);

        /// <summary>A perk's share at a level: <paramref name="percentAt100"/> percent at level 100, linear from 0.</summary>
        public static float Share(float percentAt100, float level) =>
            Mathf.Max(0f, percentAt100) / 100f * Mathf.Clamp01(level / MaxLevel);

        /// <summary>True when <paramref name="level"/> reaches a milestone setting; above 100 means never.</summary>
        public static bool Reaches(float level, float milestone) => milestone <= MaxLevel && level >= milestone;

        /// <summary>The user's art, <c>assets/skill_husbandry.png</c> embedded in the DLL (64x64), else a trophy's icon.</summary>
        private static Sprite Icon()
        {
            if (!triedEmbedded)
            {
                triedEmbedded = true;
                embedded = EmbeddedSprite.Load(typeof(HusbandrySkill).Assembly, IconResource, "skill_husbandry");
            }
            return embedded != null ? embedded : TrophyIcon();
        }

        private static Sprite TrophyIcon()
        {
            ZNetScene scene = ZNetScene.instance;
            if (scene == null)
                return null;
            foreach (string name in IconItems)
            {
                GameObject prefab = scene.GetPrefab(name);
                ItemDrop item = prefab != null ? prefab.GetComponent<ItemDrop>() : null;
                if (item != null && item.m_itemData.m_shared.m_icons.Length > 0)
                    return item.m_itemData.GetIcon();
            }
            return null;
        }
    }
}
