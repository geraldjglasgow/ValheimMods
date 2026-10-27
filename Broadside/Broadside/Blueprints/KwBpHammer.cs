using System;
using System.Collections.Generic;
using HarmonyLib;
using PatchGuard;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// The saved ships in the hammer's Misc tab, after Broadside's own pieces (<see cref="KwHammer"/>): put in when the
    /// local player takes up the hammer and its entries are not as they should be (a ship saved, changed or gone, or the
    /// kit's pieces added after them), and at once when the table saves one or its window changes the ships
    /// (<see cref="Refresh"/>). The entries are known to the player without the game's "new piece" message. Clients only:
    /// a dedicated server never builds them.
    /// </summary>
    [HarmonyPatch(typeof(Player), "SetPlaceMode")]
    public static class KwBpHammer
    {
        private static PieceTable? putIn;
        private static int putVersion = -1, putCount = -1;

        [HarmonyPrefix]
        private static void Prefix(Player __instance, PieceTable buildPieces) => Guard.Run("Broadside blueprint hammer", Ensure, __instance, buildPieces);

        /// <summary>The entries again now; an open build menu shows them at once.</summary>
        public static void Refresh()
        {
            PieceTable? table = Table();
            Player player = Player.m_localPlayer;
            if (table == null || !KwShipBody.Drawn)
                return;
            Put(table, player);
            if (player != null && player.InPlaceMode())
                player.UpdateAvailablePiecesList();
        }

        private static void Ensure(Player player, PieceTable? table)
        {
            if (player != Player.m_localPlayer || table == null || table != Table())
                return;
            KwBpLibrary.Names();
            if (!Intact(table))
                Put(table, player);
        }

        // The same table and blueprints as last put, all entries there, and no kit piece after them.
        private static bool Intact(PieceTable table)
        {
            if (table != putIn || putVersion != KwBpLibrary.Version)
                return false;
            int mine = 0;
            foreach (GameObject prefab in table.m_pieces)
            {
                if (KwBpEntries.IsEntry(prefab))
                    mine++;
                else if (mine > 0 && prefab != null && prefab.name.StartsWith("KW_", StringComparison.Ordinal))
                    return false;
            }
            return mine == putCount;
        }

        private static void Put(PieceTable table, Player? player)
        {
            List<GameObject> entries = KwBpEntries.Current();
            table.m_pieces.RemoveAll(p => p != null && p.name.StartsWith(KwBlueprintStation.EntryPrefix, StringComparison.Ordinal));
            table.m_pieces.AddRange(entries);
            putIn = table;
            putVersion = KwBpLibrary.Version;
            putCount = entries.Count;
            if (player == null)
                return;
            foreach (GameObject entry in entries)
                player.m_knownRecipes.Add(entry.GetComponent<Piece>().m_name);
        }

        private static PieceTable? Table()
        {
            ObjectDB database = ObjectDB.instance;
            GameObject? hammer = database != null ? database.GetItemPrefab("Hammer") : null;
            ItemDrop? drop = hammer != null ? hammer.GetComponent<ItemDrop>() : null;
            return drop != null ? drop.m_itemData.m_shared.m_buildPieces : null;
        }
    }
}
