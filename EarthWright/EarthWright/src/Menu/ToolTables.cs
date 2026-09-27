using System.Collections.Generic;
using System.Linq;
using EarthWright.Actions;
using EarthWright.Core;
using UnityEngine;

namespace EarthWright.Menu
{
    /// <summary>
    /// The build tables of the terrain tools. The hoe's and the cultivator's are EarthWright's to arrange: the game's
    /// terrain entries that are switched on, then EarthWright's entries, then the custom ones, before the game's other
    /// pieces (stones, saplings).
    /// The tables are prefab assets that outlive the object database, so every pass starts from what is there.
    /// </summary>
    public static class ToolTables
    {
        private static readonly Dictionary<PieceTable, int> anchors = new Dictionary<PieceTable, int>();

        public static PieceTable Hoe { get; private set; }

        public static PieceTable Cultivator { get; private set; }

        public static void Find(ObjectDB db)
        {
            Hoe = TableOf(db.GetItemPrefab("Hoe"));
            Cultivator = TableOf(db.GetItemPrefab("Cultivator"));
        }

        /// <summary>Lays out both tables; true when any list changed. Cheap and repeatable.</summary>
        public static bool ApplyAll()
        {
            bool changed = ApplyOwn(Hoe, ToolFamily.Hoe);
            changed |= ApplyOwn(Cultivator, ToolFamily.Cultivator);
            return changed;
        }

        private static bool ApplyOwn(PieceTable table, ToolFamily tool)
        {
            if (table == null)
                return false;
            int fallback = anchors.TryGetValue(table, out int anchor) ? anchor : 0;
            return Relaid(table, () => anchors[table] = TableLayout.Rebuild(table.m_pieces, IsManaged, Block(tool), Custom(tool), fallback));
        }

        /// <summary>Runs a layout and tells whether the table's list differs afterwards.</summary>
        private static bool Relaid(PieceTable table, System.Action layout)
        {
            List<GameObject> before = new List<GameObject>(table.m_pieces);
            layout();
            return !before.SequenceEqual(table.m_pieces);
        }

        /// <summary>The visible game terrain entries of the tool in the game's order, then EarthWright's.</summary>
        private static List<GameObject> Block(ToolFamily tool)
        {
            IEnumerable<string> ids = GameEntries.For(tool).Select(e => e.Id).Concat(EntryDefs.For(tool).Select(d => d.Id));
            return ids.Where(EntryVisibility.Visible).Select(EntryRegistry.Prefab).Where(p => p != null).ToList();
        }

        private static List<(GameObject Prefab, int? Position)> Custom(ToolFamily tool)
        {
            return CustomPrefabs.For(tool).Where(c => EntryVisibility.Visible(c.Prefab.name)).ToList();
        }

        private static bool IsManaged(string name) => GameEntries.Is(name) || EntryRegistry.IsOurs(name);

        private static PieceTable TableOf(GameObject item)
        {
            ItemDrop drop = item != null ? item.GetComponent<ItemDrop>() : null;
            return drop?.m_itemData?.m_shared?.m_buildPieces;
        }
    }
}
