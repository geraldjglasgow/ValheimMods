using UnityEngine;

namespace StatusTemplates
{
    /// <summary>
    /// Icons borrowed from the game's own items and status effects, for a status effect of a mod's own. Each returns the
    /// first one found in the live object database, in the order given, and null while there is no database: a
    /// template's icon function (<see cref="StatusTemplate{T}"/>) asks again until it gets one. Pass a static array when
    /// called often.
    /// </summary>
    public static class GameIcons
    {
        /// <summary>The first icon of these item prefabs, by prefab name.</summary>
        public static Sprite? Item(params string[] prefabNames)
        {
            ObjectDB db = ObjectDB.instance;
            if (db == null)
            {
                return null;
            }
            foreach (string name in prefabNames)
            {
                GameObject? prefab = db.GetItemPrefab(name);
                ItemDrop? item = prefab != null ? prefab.GetComponent<ItemDrop>() : null;
                Sprite[]? icons = item != null ? item.m_itemData?.m_shared?.m_icons : null;
                if (icons != null && icons.Length > 0 && icons[0] != null)
                {
                    return icons[0];
                }
            }
            return null;
        }

        /// <summary>The first icon of these status effects, by name hash (such as <c>SEMan.s_statusEffectFrost</c>).</summary>
        public static Sprite? Effect(params int[] nameHashes)
        {
            ObjectDB db = ObjectDB.instance;
            if (db == null)
            {
                return null;
            }
            foreach (int hash in nameHashes)
            {
                StatusEffect? effect = db.GetStatusEffect(hash);
                if (effect != null && effect.m_icon != null)
                {
                    return effect.m_icon;
                }
            }
            return null;
        }

        /// <summary>The first icon of these status effects, by object name (such as <c>Wet</c>).</summary>
        public static Sprite? Effect(params string[] names)
        {
            foreach (string name in names)
            {
                Sprite? icon = Effect(name.GetStableHashCode());
                if (icon != null)
                {
                    return icon;
                }
            }
            return null;
        }
    }
}
