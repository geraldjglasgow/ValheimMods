using System;
using System.Collections.Generic;
using HarmonyLib;
using PatchGuard;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// Broadside's own sublist in the build menu's Categories tab: picking Transportation opens a "Broadside" entry under
    /// it that holds every Broadside piece in the hammer (the dock pieces and tools, the fittings, the blueprint table,
    /// the ship cannons, the storage crates, ammunition piles, the hammock, cargo crane and crate, the dockside decorations and
    /// the saved ships), and Transportation itself lists the rest: the game's own transport and other
    /// mods'. The entry shows while Transportation or itself is picked, and only when the hammer has a Broadside piece.
    /// The game's tag ids on that tab are indexes into its usage flags; Broadside's is one clear of them all
    /// (<see cref="TagId"/>), handled in <see cref="KwBuildMenuTags"/>, <see cref="KwBuildMenuName"/>,
    /// <see cref="KwBuildMenuPieces"/> and <see cref="KwBuildMenuOpen"/>. Only drawn: nothing is saved or sent.
    /// </summary>
    public static class KwBuildMenu
    {
        /// <summary>The entry's tag id: clear of the game's usage indexes, its All (-1) and its new favourite (-10).</summary>
        public const int TagId = 0x4B57;

        /// <summary>The entry's name, set in under Transportation's (the menu's text reads rich text).</summary>
        public const string Label = "<indent=1em>Broadside";

        /// <summary>Transportation's tag id: its usage flag's index, numbered as the game numbers them.</summary>
        public static readonly int Transport = Array.IndexOf((Piece.UsageTagFlags[])Enum.GetValues(typeof(Piece.UsageTagFlags)), Piece.UsageTagFlags.Transport);

        private static readonly HashSet<GameObject> kit = new HashSet<GameObject>();
        private static int kitCount = -1;

        /// <summary>Whether picking the tag opens the entry: Transportation or the entry itself.</summary>
        public static bool Opens(int tagId) => tagId == TagId || tagId == Transport;

        /// <summary>Whether the piece is Broadside's: a dock piece, tool, fitting or table, a ship cannon, a crate, pile, hammock or the crane, a dockside decoration, the Auto-decorate tool, the Dock tool or a saved ship.</summary>
        public static bool Owns(Piece piece) => piece != null && (KwBpEntries.IsEntry(piece.gameObject) || Kit().Contains(piece.gameObject));

        // Broadside's made prefabs, gathered again when more have been made (each list is made once).
        private static HashSet<GameObject> Kit()
        {
            GameObject? crane = KwCargoPrefabs.Crane, crate = KwCargoPrefabs.Crate, decorate = KwDecorTool.Made;
            int count = KwPrefabs.All.Count + KwCannonPrefabs.All.Count + KwDockside.All.Count + KwCrates.All.Count
                + KwPiles.All.Count + KwHammocks.All.Count + KwTrawl.All.Count + KwPierPrefabs.All.Count + (crane != null ? 1 : 0) + (crate != null ? 1 : 0) + (decorate != null ? 1 : 0);
            if (count == kitCount)
                return kit;
            kit.Clear();
            kit.UnionWith(KwPrefabs.All);
            kit.UnionWith(KwCannonPrefabs.All);
            kit.UnionWith(KwDockside.All);
            kit.UnionWith(KwCrates.All);
            kit.UnionWith(KwPiles.All);
            kit.UnionWith(KwHammocks.All);
            kit.UnionWith(KwTrawl.All);
            kit.UnionWith(KwPierPrefabs.All);
            if (crane != null)
                kit.Add(crane);
            if (crate != null)
                kit.Add(crate);
            if (decorate != null)
                kit.Add(decorate);
            kitCount = count;
            return kit;
        }
    }

    /// <summary>ByUsagePieceList.UpdateAvailableTags postfix: the Broadside entry right after Transportation, when the hammer has a Broadside piece.</summary>
    [HarmonyPatch(typeof(ByUsagePieceList), nameof(ByUsagePieceList.UpdateAvailableTags))]
    public static class KwBuildMenuTags
    {
        [HarmonyPostfix]
        private static void Postfix(ByUsagePieceList __instance, PieceTable pieceTable) => Guard.Run("Broadside build menu entry", Insert, __instance, pieceTable);

        private static void Insert(ByUsagePieceList list, PieceTable table)
        {
            int at = list.m_availableTags.IndexOf(KwBuildMenu.Transport);
            if (at < 0 || list.m_availableTags.Contains(KwBuildMenu.TagId))
                return;
            foreach (Piece piece in table.m_availablePieces)
            {
                if (!KwBuildMenu.Owns(piece))
                    continue;
                list.m_availableTags.Insert(at + 1, KwBuildMenu.TagId);
                return;
            }
        }
    }

    /// <summary>ByUsagePieceList.GetTagDisplayName prefix: the Broadside entry's name (the game's own lookup would read past its flags).</summary>
    [HarmonyPatch(typeof(ByUsagePieceList), nameof(ByUsagePieceList.GetTagDisplayName))]
    public static class KwBuildMenuName
    {
        [HarmonyPrefix]
        private static bool Prefix(ByUsagePieceList __instance, int index, ref string __result)
        {
            List<int> tags = __instance.m_availableTags;
            if (index < 0 || index >= tags.Count || tags[index] != KwBuildMenu.TagId)
                return true;
            __result = KwBuildMenu.Label;
            return false;
        }
    }

    /// <summary>
    /// ByUsagePieceList.GetAvailablePiecesWithTag prefix and postfix: the Broadside entry lists Broadside's pieces (and the
    /// hammer's repair and remove, as every tag does) in place of the game's own lookup, which would read past its flags;
    /// Transportation lists the rest of its pieces. Other mods' postfixes still run on both.
    /// </summary>
    [HarmonyPatch(typeof(ByUsagePieceList), nameof(ByUsagePieceList.GetAvailablePiecesWithTag))]
    public static class KwBuildMenuPieces
    {
        [HarmonyPrefix]
        private static bool Prefix(int tagId, PieceTable pieceTable, IList<Piece> resultOut)
        {
            if (tagId != KwBuildMenu.TagId)
                return true;
            Guard.Run("Broadside build menu pieces", Fill, pieceTable, resultOut);
            return false;
        }

        [HarmonyPostfix]
        private static void Postfix(int tagId, IList<Piece> resultOut)
        {
            if (tagId == KwBuildMenu.Transport)
                Guard.Run("Broadside build menu transport", Drop, resultOut);
        }

        private static void Fill(PieceTable table, IList<Piece> result)
        {
            foreach (Piece piece in table.m_availablePieces)
            {
                if (piece.m_repairPiece || piece.m_removePiece || KwBuildMenu.Owns(piece))
                    result.Add(piece);
            }
        }

        private static void Drop(IList<Piece> result)
        {
            for (int i = result.Count - 1; i >= 0; i--)
            {
                if (KwBuildMenu.Owns(result[i]))
                    result.RemoveAt(i);
            }
        }
    }

    /// <summary>BuildUi.SetCurrentTag prefix (private): the Broadside entry shows only while Transportation or itself is picked.</summary>
    [HarmonyPatch(typeof(BuildUi), nameof(BuildUi.SetCurrentTag))]
    public static class KwBuildMenuOpen
    {
        [HarmonyPrefix]
        private static void Prefix(BuildUi __instance, int tagId) => Guard.Run("Broadside build menu sublist", Show, __instance, tagId);

        private static void Show(BuildUi menu, int tagId)
        {
            bool open = KwBuildMenu.Opens(tagId);
            foreach (BuildUiTagButton button in menu.m_tagButtons)
            {
                if (button.m_tagId == KwBuildMenu.TagId && button.gameObject.activeSelf != open)
                    button.gameObject.SetActive(open);
            }
        }
    }
}
