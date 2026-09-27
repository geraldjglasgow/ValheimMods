using EarthWright.Actions;
using EarthWright.Brush;
using EarthWright.Core;
using EarthWright.Gear;
using EarthWright.Menu;
using EarthWright.Terrain;
using UnityEngine;

namespace EarthWright.Costs
{
    /// <summary>
    /// One terrain use of the local player as the costs see it: the entry's action and piece, the held tool, the brush
    /// radius the amounts grow with, the paint the use lays and the entry's YAML overrides. A click context is only
    /// built for the local player while EarthWright is on for them and a terrain tool is in the right hand, and only
    /// for a piece the action catalog knows as terrain work, so the hammer and every other piece keep the game's costs.
    /// The tool is only a terrain tool (a console command run with an axe in hand wears nothing), and the tool family
    /// for the station switches is the tool in hand (the shovel's menu also lists hoe entries such as Level ground).
    /// </summary>
    internal sealed class CostContext
    {
        public Player Player;
        public ToolAction Action;
        public Piece Piece;
        public ItemDrop.ItemData Tool;
        public ToolFamily Family;
        public float Radius;
        public PaintOp Paint;
        public EntryOverride Override;

        /// <summary>The brush radius over the entry's normal radius; 1 for entries the brush does not resize.</summary>
        public float RadiusRatio => Action.BaseRadius > 0f ? Radius / Action.BaseRadius : 1f;

        /// <summary>(radius ratio)^exponent: how a cost grows with the brush. 1 for exponent 0.</summary>
        public float Scale(float exponent)
        {
            return exponent <= 0f ? 1f : Mathf.Pow(Mathf.Max(RadiusRatio, 0.01f), exponent);
        }

        /// <summary>The use of this piece by the local player's terrain tool, or null when the game's own costs apply.</summary>
        public static CostContext ForPiece(Player player, Piece piece)
        {
            if (piece == null || !IsLocalTerrainUser(player))
                return null;
            ToolAction action = ActionCatalog.For(piece);
            return action != null ? Build(player, action, piece) : null;
        }

        /// <summary>The click the local player would make now with the selected piece, or null.</summary>
        public static CostContext ForSelected(Player player)
        {
            return player != null ? ForPiece(player, player.GetSelectedPiece()) : null;
        }

        /// <summary>Work of an action by the local player (special entries through CostApi), whatever is in hand.</summary>
        public static CostContext ForAction(Player player, ToolAction action)
        {
            if (player == null || player != Player.m_localPlayer || action == null)
                return null;
            return Build(player, action, PieceOf(player, action));
        }

        private static bool IsLocalTerrainUser(Player player)
        {
            return player != null && player == Player.m_localPlayer && GeneralSettings.Active && LocalTool.IsToolName(LocalTool.RightItemName);
        }

        private static CostContext Build(Player player, ToolAction action, Piece piece)
        {
            ItemDrop.ItemData tool = TerrainTool(player);
            return new CostContext
            {
                Player = player,
                Action = action,
                Piece = piece,
                Tool = tool,
                Family = FamilyOf(tool, action),
                Radius = action.Resizable && BrushState.Active ? BrushState.Radius : action.BaseRadius,
                Paint = PaintOf(action),
                Override = CostOverrides.For(action.Id),
            };
        }

        /// <summary>The item in the right hand when it is a terrain tool, else null.</summary>
        private static ItemDrop.ItemData TerrainTool(Player player)
        {
            ItemDrop.ItemData item = player.GetRightItem();
            string name = item != null && item.m_dropPrefab != null ? item.m_dropPrefab.name : null;
            return LocalTool.IsToolName(name) ? item : null;
        }

        /// <summary>The held tool's family; other mods' entries stay "modded", and without a known tool the entry's own.</summary>
        private static ToolFamily FamilyOf(ItemDrop.ItemData tool, ToolAction action)
        {
            string name = tool != null && tool.m_dropPrefab != null ? tool.m_dropPrefab.name : null;
            if (action.Family == ToolFamily.Modded || name == null)
                return action.Family;
            if (name == ToolNames.Shovel)
                return ToolFamily.Shovel;
            return name == ToolNames.Cultivator ? ToolFamily.Cultivator : name == ToolNames.Hoe ? ToolFamily.Hoe : action.Family;
        }

        /// <summary>The paint a click lays: the player's paint choice, none with "keep paint" on a height entry, else the entry's own.</summary>
        private static PaintOp PaintOf(ToolAction action)
        {
            if (!BrushState.Active)
                return action.Paint;
            if (BrushState.PaintOverride != PaintOp.None)
                return BrushState.PaintOverride;
            return BrushState.KeepPaint && action.Height != HeightOp.None ? PaintOp.None : action.Paint;
        }

        /// <summary>The entry's piece: the selected one when it is this entry, else the registered entry prefab.</summary>
        private static Piece PieceOf(Player player, ToolAction action)
        {
            Piece selected = player.GetSelectedPiece();
            if (selected != null && Utils.GetPrefabName(selected.gameObject) == action.Id)
                return selected;
            GameObject prefab = EntryRegistry.Prefab(action.Id);
            if (prefab == null && ZNetScene.instance != null)
                prefab = ZNetScene.instance.GetPrefab(action.Id);
            return prefab != null ? prefab.GetComponent<Piece>() : null;
        }
    }
}
