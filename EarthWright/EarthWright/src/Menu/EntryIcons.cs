using System.Collections.Generic;
using PlateColumn;
using UnityEngine;
using UnityEngine.Rendering;

namespace EarthWright.Menu
{
    /// <summary>
    /// Icons of the menu entries. EarthWright's own are 128 px PNGs embedded in the DLL
    /// (<c>EarthWright.assets.menu_&lt;name&gt;.png</c>), decoded once per game run; a custom entry may instead borrow the
    /// icon of any piece or item by prefab name. A missing icon returns null and the entry keeps its template's icon.
    /// </summary>
    public static class EntryIcons
    {
        /// <summary>The names of the embedded icons, for custom entries and the YAML file's comments.</summary>
        public static readonly string[] Names =
        {
            "lower", "smooth", "paint", "reset", "ramp", "road", "clear", "groundbreaker", "terraform", "till", "uproot", "custom",
        };

        private static readonly Dictionary<string, Sprite> loaded = new Dictionary<string, Sprite>();

        /// <summary>
        /// An embedded icon by name ("lower"), or null when it is missing or will not decode (logged once), and always
        /// null on a machine without graphics (a dedicated server shows no menus).
        /// </summary>
        public static Sprite Own(string name)
        {
            if (string.IsNullOrEmpty(name) || SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null)
                return null;
            if (loaded.TryGetValue(name, out Sprite sprite))
                return sprite;
            string resource = "EarthWright.assets.menu_" + name + ".png";
            sprite = EmbeddedSprite.Load(typeof(EntryIcons).Assembly, resource, "ew_menu_" + name);
            if (sprite == null)
                Plugin.Log.LogWarning($"EarthWright: could not read the embedded icon {resource}; the entry keeps the game's icon.");
            loaded[name] = sprite;
            return sprite;
        }

        /// <summary>
        /// A custom entry's icon: one of EarthWright's by icon name or entry id ("reset", "ew_reset", "menu_reset"),
        /// or the icon of a piece (the tools' entries or any networked piece) or an item, by prefab name.
        /// </summary>
        public static Sprite Resolve(string reference)
        {
            if (string.IsNullOrEmpty(reference))
                return null;
            string bare = reference.Trim();
            foreach (string prefix in new[] { "menu_", "ew_" })
            {
                if (bare.StartsWith(prefix) && System.Array.IndexOf(Names, bare.Substring(prefix.Length)) >= 0)
                    return Own(bare.Substring(prefix.Length));
            }
            if (System.Array.IndexOf(Names, bare) >= 0)
                return Own(bare);
            return PieceIcon(bare) ?? ItemIcon(bare);
        }

        private static Sprite PieceIcon(string name)
        {
            GameObject prefab = EntryRegistry.Prefab(name);
            if (prefab == null && ZNetScene.instance != null)
                prefab = ZNetScene.instance.GetPrefab(name);
            Piece piece = prefab != null ? prefab.GetComponent<Piece>() : null;
            return piece != null ? piece.m_icon : null;
        }

        private static Sprite ItemIcon(string name)
        {
            GameObject prefab = ObjectDB.instance != null ? ObjectDB.instance.GetItemPrefab(name) : null;
            ItemDrop item = prefab != null ? prefab.GetComponent<ItemDrop>() : null;
            if (item == null || item.m_itemData?.m_shared?.m_icons == null || item.m_itemData.m_shared.m_icons.Length == 0)
                return null;
            return item.m_itemData.m_shared.m_icons[0];
        }
    }
}
