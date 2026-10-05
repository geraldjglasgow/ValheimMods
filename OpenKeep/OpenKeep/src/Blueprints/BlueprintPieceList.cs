using System.Collections.Generic;
using UnityEngine;

namespace OpenKeep.Blueprints
{
    /// <summary>
    /// The Blueprints tab's piece list for the game's build menu: the entries of <see cref="BlueprintMenu.View"/> in its
    /// order (tools, then the folder's blueprints), as far as the hammer's table has them available; any other
    /// build table gets nothing. It asks for the tag column (so the game shows it, with its search field) but has no
    /// tags: the column holds the folder panel instead (<see cref="Tab.FolderPanel"/>).
    /// </summary>
    public sealed class BlueprintPieceList : IPieceList
    {
        public string DisplayName => BlueprintWords.Tab;

        public bool ShowTags => true;

        public bool CanCustomizeTags => false;

        public int TagCount => 0;

        public int TagSeparatorIndex => -1;

        public string GetTagDisplayName(int index) => "";

        public int GetTagIdByIndex(int index) => -1;

        public void UpdateAvailableTags(PieceTable pieceTable)
        {
        }

        public void GetAvailablePiecesWithTag(int tagId, PieceTable pieceTable, IList<Piece> resultOut)
        {
            if (!HammerTable.Is(pieceTable))
                return;
            foreach (GameObject entry in BlueprintMenu.View)
            {
                Piece piece = entry != null ? entry.GetComponent<Piece>() : null;
                if (piece != null && pieceTable.m_availablePieces.Contains(piece))
                    resultOut.Add(piece);
            }
        }
    }
}
