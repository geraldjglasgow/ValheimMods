using PlateColumn;
using UnityEngine;

namespace GrindstoneSkills
{
    /// <summary>
    /// The Foraging skill's icon: <c>assets/skill_foraging.png</c> embedded in the DLL when the file is there (64x64,
    /// like the game's skill icons), otherwise the Raspberry item's icon. Called when ZNetScene wakes, until it returns
    /// an icon; the embedded file is decoded once.
    /// </summary>
    public static class ForagingIcon
    {
        public const string Resource = "GrindstoneSkills.assets.skill_foraging.png";
        private const string FallbackItem = "Raspberry";

        private static Sprite embedded;
        private static bool triedEmbedded;

        public static Sprite Find()
        {
            if (!triedEmbedded)
            {
                triedEmbedded = true;
                embedded = EmbeddedSprite.Load(typeof(ForagingIcon).Assembly, Resource, "skill_foraging");
            }
            return embedded != null ? embedded : ItemIcon(FallbackItem);
        }

        private static Sprite ItemIcon(string prefabName)
        {
            GameObject prefab = ZNetScene.instance != null ? ZNetScene.instance.GetPrefab(prefabName) : null;
            ItemDrop drop = prefab != null ? prefab.GetComponent<ItemDrop>() : null;
            Sprite[] icons = drop?.m_itemData?.m_shared?.m_icons;
            return icons != null && icons.Length > 0 ? icons[0] : null;
        }
    }
}
