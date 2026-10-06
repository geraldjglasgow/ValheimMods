using EarthWright.Actions;
using EarthWright.Brush;
using EarthWright.Core;
using EarthWright.Terrain;
using UnityEngine;

namespace EarthWright.Paths
{
    /// <summary>
    /// What a ramp or road plan is made from besides its points (which mark the session dirty themselves): the entry,
    /// the cursor, the player's feet (the quick ramp starts there), the brush size and target height, the paint choice
    /// and the held modifiers. The session plans again only when these changed, to the centimetre, or the plan is old.
    /// </summary>
    internal struct PlanInputs
    {
        private const float Tolerance = 0.01f;

        private ToolAction action;
        private bool hasCursor;
        private Vector3 cursor;
        private Vector3 feet;
        private Vector3 brush;
        private PaintOp paint;
        private int flags;

        public static PlanInputs Now(ToolAction action)
        {
            PlanInputs now = new PlanInputs { action = action, paint = BrushState.PaintOverride };
            now.hasCursor = PathSelection.TryCursor(out now.cursor);
            Player player = Player.m_localPlayer;
            now.feet = player != null ? player.transform.position : Vector3.zero;
            now.brush = new Vector3(BrushState.Radius, BrushState.TargetHeight, (float)BrushState.TargetSource);
            now.flags = (Keys.Held(PathSettings.BlendEndsModifier) ? 1 : 0) | (Keys.Held(PathSettings.OneSideModifier) ? 2 : 0)
                | (BrushState.KeepPaint ? 4 : 0);
            return now;
        }

        public bool Same(PlanInputs other)
        {
            return action == other.action && hasCursor == other.hasCursor && paint == other.paint && flags == other.flags
                && Near(cursor, other.cursor) && Near(feet, other.feet) && Near(brush, other.brush);
        }

        private static bool Near(Vector3 a, Vector3 b) => (a - b).sqrMagnitude < Tolerance * Tolerance;
    }
}
