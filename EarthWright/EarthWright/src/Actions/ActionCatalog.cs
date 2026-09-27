using System.Collections.Generic;
using EarthWright.Core;
using EarthWright.Terrain;

namespace EarthWright.Actions
{
    /// <summary>
    /// Every known terrain action by piece prefab name: the game's own hoe and cultivator entries
    /// (<see cref="VanillaActions"/>), EarthWright's own entries (registered by the Menu module), and terrain pieces of
    /// other mods, whose action is derived from their TerrainOp the first time they are seen.
    /// </summary>
    public static class ActionCatalog
    {
        private static readonly Dictionary<string, ToolAction> actions = new Dictionary<string, ToolAction>();

        public static void Register(ToolAction action) => actions[action.Id] = action;

        public static IEnumerable<ToolAction> All => actions.Values;

        /// <summary>The action of a piece, or null when the piece is not a terrain piece.</summary>
        public static ToolAction For(Piece piece)
        {
            if (piece == null)
                return null;
            string name = Utils.GetPrefabName(piece.gameObject);
            if (actions.TryGetValue(name, out ToolAction known))
                return known;
            TerrainOp op = piece.GetComponent<TerrainOp>();
            if (op == null)
                return null;
            ToolAction derived = Derive(name, op.m_settings);
            actions[name] = derived;
            return derived;
        }

        public static ToolAction ById(string id) => id != null && actions.TryGetValue(id, out ToolAction action) ? action : null;

        /// <summary>The action of the local player's selected piece while a terrain tool is in use, or null.</summary>
        public static ToolAction Current => LocalTool.InTerrainTool ? For(LocalTool.SelectedPiece) : null;

        /// <summary>A modded terrain piece: level/smooth become Level, raise becomes Raise or Lower, paint is kept.</summary>
        public static ToolAction Derive(string name, TerrainOp.Settings s)
        {
            ToolAction action = new ToolAction { Id = name, Family = ToolFamily.Modded, BaseRadius = s.GetRadius() };
            if (s.m_level || s.m_smooth)
            {
                action.Height = HeightOp.Level;
                action.UsesTarget = true;
                action.Style = s.m_level ? LevelStyle.Instant : LevelStyle.Ease;
            }
            else if (s.m_raise)
            {
                action.Height = s.m_raiseDelta >= 0f ? HeightOp.Raise : HeightOp.Lower;
                action.Amount = UnityEngine.Mathf.Abs(s.m_raiseDelta);
                action.UsesAmount = true;
            }
            if (s.m_paintCleared)
                action.Paint = VanillaActions.PaintOf(s.m_paintType);
            action.PaintRatio = action.BaseRadius > 0f ? s.m_paintRadius / action.BaseRadius : 1f;
            action.PaintHeightCheck = s.m_paintHeightCheck;
            return action;
        }
    }
}
