using System;
using UnityEngine;

namespace GrindstoneSkills
{
    /// <summary>
    /// One of GrindstoneSkills' own skills, one the game's Skills.SkillType enum does not define (Sailing, Defense).
    /// Its type number is the stable hash of its identity, masked positive: far from the game's numbers and from small
    /// numbers another mod picks, and the same in every version, since a saved level is stored under it. Levels live
    /// with the character like every skill, on the player's own client; that client also publishes the level to the
    /// player's own ZDO (<see cref="CustomSkillLevels"/>), so other machines read it with <see cref="Of"/>.
    /// Every instance is handed to <see cref="CustomSkills.Initialize"/> in the plugin's Awake.
    /// </summary>
    public sealed class CustomSkill
    {
        public const float MaxLevel = 100f;

        private readonly Func<Sprite> icon;

        /// <param name="identity">The name whose stable hash is the type number. Never change it: saved levels use it.</param>
        /// <param name="name">The name shown in the skills panel, and the word raiseskill and resetskill take.</param>
        /// <param name="levelKey">The float key on the player's own ZDO the level is published under.</param>
        /// <param name="icon">Returns the skill's icon; called when ZNetScene wakes, until it returns one.</param>
        public CustomSkill(string identity, string name, string description, string levelKey, Func<Sprite> icon, float increaseStep = 1f)
        {
            Name = name;
            Type = (Skills.SkillType)(identity.GetStableHashCode() & 0x7FFFFFFF);
            LocalizationKey = "skill_" + Type.ToString().ToLowerInvariant();
            LevelKey = levelKey;
            this.icon = icon;
            Definition = new Skills.SkillDef { m_skill = Type, m_description = description, m_increseStep = increaseStep };
        }

        public string Name { get; }
        public Skills.SkillType Type { get; }

        /// <summary>The word the game looks up for the skill's name: "skill_" + the type's name in lower case, a number here.</summary>
        public string LocalizationKey { get; }

        public string LevelKey { get; }

        /// <summary>The definition every Skills component gets. Its icon is filled in once the game's prefabs are loaded.</summary>
        public Skills.SkillDef Definition { get; }

        /// <summary>The local player's level, 0 when there is no local player.</summary>
        public float Local()
        {
            Player player = Player.m_localPlayer;
            return player == null ? 0f : player.GetSkillLevel(Type);
        }

        /// <summary>Any player's level: the local player's own skill, anyone else's as their client published it (0 until then).</summary>
        public float Of(Player player)
        {
            if (player == null)
                return 0f;
            if (player == Player.m_localPlayer)
                return player.GetSkillLevel(Type);
            ZNetView nview = player.m_nview;
            return nview != null && nview.IsValid() ? Mathf.Max(0f, nview.GetZDO().GetFloat(LevelKey)) : 0f;
        }

        /// <summary>Fills in the definition's icon while it has none.</summary>
        internal void FindIcon()
        {
            if (Definition.m_icon == null && icon != null)
                Definition.m_icon = icon();
        }
    }
}
