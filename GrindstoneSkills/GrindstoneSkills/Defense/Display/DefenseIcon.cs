using PlateColumn;
using UnityEngine;

namespace GrindstoneSkills
{
    /// <summary>
    /// The Defense icon: <c>assets/skill_defense.png</c> (a helmet and shield, 64x64 like the game's skill icons)
    /// embedded in the DLL, else the iron helmet's item icon. The skills panel (<see cref="DefenseSkill"/>) uses it. The
    /// embedded file is decoded once.
    /// </summary>
    public static class DefenseIcon
    {
        public const string Resource = "GrindstoneSkills.assets.skill_defense.png";

        private static readonly string[] FallbackItems = { "HelmetIron", "HelmetBronze", "ShieldWood" };

        private static Sprite embedded;
        private static bool triedEmbedded;

        public static Sprite Find()
        {
            if (!triedEmbedded)
            {
                triedEmbedded = true;
                embedded = EmbeddedSprite.Load(typeof(DefenseIcon).Assembly, Resource, "skill_defense");
            }
            return embedded != null ? embedded : ItemIcon();
        }

        private static Sprite ItemIcon()
        {
            ZNetScene scene = ZNetScene.instance;
            if (scene == null)
                return null;
            foreach (string name in FallbackItems)
            {
                GameObject prefab = scene.GetPrefab(name);
                Sprite[] icons = prefab != null ? prefab.GetComponent<ItemDrop>()?.m_itemData?.m_shared?.m_icons : null;
                if (icons != null && icons.Length > 0 && icons[0] != null)
                    return icons[0];
            }
            return null;
        }
    }
}
