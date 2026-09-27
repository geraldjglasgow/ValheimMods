using System.Collections.Generic;
using EarthWright.Menu;
using UnityEngine;

namespace EarthWright.Gear
{
    /// <summary>
    /// The shovel's build table: a PieceTable of its own (a new object with the hoe table's settings) whose entries are
    /// the pieces named in "Shovel Entries", found through the Menu module's <see cref="EntryRegistry"/> (EarthWright's
    /// entries and the game's hoe and cultivator pieces). The entry toggles of section 12 apply as they do everywhere:
    /// the Menu module disables a switched-off piece and the game leaves it out of the menu. Pieces other modules add to
    /// the table (the Menu's custom shovel entries, at object database order 300) are kept when the list is rebuilt.
    /// Names that match nothing are skipped with one warning each.
    /// </summary>
    public static class ShovelTable
    {
        private static readonly HashSet<string> warned = new HashSet<string>();
        private static readonly HashSet<GameObject> placed = new HashSet<GameObject>();

        public static PieceTable Table { get; private set; }

        /// <summary>Creates the table under the gear holder, with the hoe table's menu settings and no entries yet.</summary>
        public static PieceTable Create(PieceTable hoeTable, Transform parent)
        {
            GameObject tableObject = new GameObject("EW_ShovelPieceTable");
            tableObject.transform.SetParent(parent, false);
            Table = tableObject.AddComponent<PieceTable>();
            if (hoeTable != null)
            {
                Table.m_categories = new List<Piece.PieceCategory>(hoeTable.m_categories);
                Table.m_categoryLabels = new List<string>(hoeTable.m_categoryLabels);
                Table.m_canRemovePieces = hoeTable.m_canRemovePieces;
                Table.m_canRemoveFeasts = hoeTable.m_canRemoveFeasts;
                Table.m_skill = hoeTable.m_skill;
                Table.m_hideAdvancedMenu = hoeTable.m_hideAdvancedMenu;
            }
            return Table;
        }

        /// <summary>Fills the table from the setting, keeps what others added, and refreshes the menu of a player holding it.</summary>
        public static void Refresh()
        {
            if (Table == null)
                return;
            List<GameObject> configured = Configured();
            List<GameObject> pieces = new List<GameObject>(configured);
            foreach (GameObject other in Table.m_pieces)
            {
                if (other != null && !placed.Contains(other) && !pieces.Contains(other))
                    pieces.Add(other);
            }
            placed.Clear();
            placed.UnionWith(configured);
            Table.m_pieces = pieces;
            RefreshHolder();
        }

        /// <summary>The pieces the setting names, in its order, each once.</summary>
        private static List<GameObject> Configured()
        {
            List<GameObject> pieces = new List<GameObject>();
            foreach (string id in SettingLists.Names(GearSettings.ShovelEntries.Value))
            {
                GameObject prefab = EntryRegistry.Prefab(id);
                if (prefab == null || prefab.GetComponent<Piece>() == null)
                    WarnOnce(id);
                else if (!pieces.Contains(prefab))
                    pieces.Add(prefab);
            }
            return pieces;
        }

        /// <summary>The local player holding the shovel learns new entries and keeps the selected one (Menu's refresh).</summary>
        private static void RefreshHolder()
        {
            Player player = Player.m_localPlayer;
            if (player == null || player.m_buildPieces != Table)
                return;
            string selected = PlayerRefresh.SelectedName();
            PlayerRefresh.Refresh(selected);
        }

        private static void WarnOnce(string id)
        {
            if (warned.Add(id))
                Plugin.Log.LogWarning($"EarthWright: the shovel entry '{id}' matches no build piece; it is left out of the shovel's menu.");
        }
    }
}
