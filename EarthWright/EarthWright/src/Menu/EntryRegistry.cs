using UnityEngine;

namespace EarthWright.Menu
{
    /// <summary>
    /// Finds build-menu entry prefabs by piece prefab name: EarthWright's own entries (made by the Menu module), the
    /// custom entries of EarthWright.Entries.yml ("ew_custom_&lt;id&gt;") and the game's hoe and cultivator pieces (also
    /// those an entry toggle took out of a table). Valid from GameReady.OnObjectDb order 100 on. The Gear module builds
    /// the shovel's menu from it; an entry switched off in section 12 is a disabled piece the game leaves out of any menu.
    /// </summary>
    public static class EntryRegistry
    {
        /// <summary>The entry prefab with this piece prefab name, or null.</summary>
        public static GameObject Prefab(string pieceName)
        {
            return EntryFactory.Get(pieceName) ?? CustomPrefabs.Get(pieceName) ?? GamePieces.Get(pieceName);
        }

        /// <summary>The Piece of <see cref="Prefab"/>, or null.</summary>
        public static Piece PieceOf(string pieceName)
        {
            GameObject prefab = Prefab(pieceName);
            return prefab != null ? prefab.GetComponent<Piece>() : null;
        }

        /// <summary>The name is one of EarthWright's entries or a custom entry.</summary>
        public static bool IsOurs(string pieceName) => EntryDefs.Get(pieceName) != null || CustomPrefabs.IsCustomName(pieceName);

        /// <summary>Whether the entry is listed for this player now (toggles, master switch, admin rights).</summary>
        public static bool IsListed(string pieceName) => EntryVisibility.Visible(pieceName);
    }
}
