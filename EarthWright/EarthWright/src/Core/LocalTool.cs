using System.Collections.Generic;

namespace EarthWright.Core
{
    /// <summary>
    /// What the local player holds. A terrain tool is the hoe or the cultivator. A terrain piece is any build piece with
    /// a <c>TerrainOp</c>, or one registered as a special action (ramp, road, clearing, custom entries).
    /// </summary>
    public static class LocalTool
    {
        private static readonly HashSet<string> tools = new HashSet<string> { "Hoe", "Cultivator" };

        public static bool IsToolName(string itemPrefabName) => itemPrefabName != null && tools.Contains(itemPrefabName);

        public static Player Player => Player.m_localPlayer;

        /// <summary>The prefab name of the item in the local player's right hand, or null.</summary>
        public static string RightItemName
        {
            get
            {
                ItemDrop.ItemData item = Player != null ? Player.GetRightItem() : null;
                return item?.m_dropPrefab != null ? item.m_dropPrefab.name : null;
            }
        }

        /// <summary>The local player holds a terrain tool and is in build mode with its menu closed.</summary>
        public static bool InTerrainTool
        {
            get
            {
                Player player = Player;
                if (player == null || player.IsDead() || !player.InPlaceMode() || Hud.IsPieceSelectionVisible())
                    return false;
                return IsToolName(RightItemName);
            }
        }

        /// <summary>The selected piece of the build table, or null.</summary>
        public static Piece SelectedPiece => Player != null && Player.InPlaceMode() ? Player.GetSelectedPiece() : null;

        /// <summary>The prefab name of the selected piece, or null.</summary>
        public static string SelectedPieceName
        {
            get
            {
                Piece piece = SelectedPiece;
                return piece != null ? Utils.GetPrefabName(piece.gameObject) : null;
            }
        }

        /// <summary>The placement ghost of the local player, or null when there is none or it is hidden.</summary>
        public static UnityEngine.GameObject Ghost
        {
            get
            {
                UnityEngine.GameObject ghost = Player != null ? Player.m_placementGhost : null;
                return ghost != null && ghost.activeSelf ? ghost : null;
            }
        }
    }
}
