using EarthWright.Actions;
using EarthWright.Brush;
using EarthWright.Core;
using EarthWright.Terrain;
using UnityEngine;

namespace EarthWright.Preview
{
    /// <summary>How the brush visuals are coloured this frame.</summary>
    internal enum PreviewTone
    {
        Normal,
        /// <summary>The click would be refused: a module reported a reason (<see cref="PreviewStatus"/>) or the game's own placement check fails.</summary>
        Blocked,
        /// <summary>The crosshair points at ground farther from the eye than the player can place.</summary>
        OutOfReach,
    }

    /// <summary>
    /// What the preview shows this frame, worked out once before the visuals so the outline, points, volume and
    /// highlight all agree: the edit a click would send now (built by the same <see cref="EditFactory"/> the click
    /// uses), the engine's own footprints of it (<see cref="StrokeParams"/>), and the tone. Within reach the brush's
    /// aim decides where it stands; beyond reach (where the brush has no aim) the footprint is drawn where the
    /// crosshair meets the ground, in the out-of-reach colour. Special entries (ramps, roads) draw their own preview.
    /// </summary>
    internal static class PreviewFrame
    {
        private const float FarRay = 100f;

        private static int terrainMask;

        /// <summary>A brush entry (not a special one) is selected with a terrain tool out and a place to draw.</summary>
        public static bool BrushVisible { get; private set; }

        public static ToolAction Action { get; private set; }

        /// <summary>The edit a click would send now (its centre moved to the crosshair when out of reach).</summary>
        public static TerrainEdit Edit { get; private set; }

        public static BrushStroke Stroke => Edit?.Stroke;

        /// <summary>The engine's view of the stroke: clamped values and both footprints.</summary>
        public static StrokeParams Params { get; private set; }

        public static FootprintSpec HeightSpec { get; private set; }

        public static FootprintSpec PaintSpec { get; private set; }

        public static PreviewTone Tone { get; private set; }

        /// <summary>The player's placing reach and the distance from the eye to the aimed ground, metres.</summary>
        public static float Reach { get; private set; }
        public static float Distance { get; private set; }

        public static void Update()
        {
            BrushVisible = false;
            Edit = null;
            Action = BrushState.Active && LocalTool.InTerrainTool && GeneralSettings.Active ? BrushState.Action : null;
            if (!TryShow())
                PlacementReason.Clear();
        }

        private static bool TryShow()
        {
            Player player = LocalTool.Player;
            if (Action == null || Action.IsPathTool || player == null || !Aim(player, out Vector3 aim, out bool beyond))
                return false;
            TerrainEdit edit = Safe.Call("EarthWright preview edit", () => EditFactory.Build(Action), null);
            if (edit?.Stroke == null)
                return false;
            // The building hooks only add flags (admin routing, terraform limits); the click will carry them too.
            EditEvents.RaiseBuilding(edit);
            if (beyond)
                edit.Stroke.Center = new Vector3(aim.x, TargetHeight.Used(Action) ? BrushState.TargetHeight : aim.y, aim.z);
            StrokeParams p = StrokeParams.From(edit);
            if (!p.Valid)
                return false;
            Show(edit, p, player, beyond);
            return true;
        }

        private static void Show(TerrainEdit edit, StrokeParams p, Player player, bool beyond)
        {
            Edit = edit;
            Params = p;
            HeightSpec = FootprintSpec.Height(p);
            PaintSpec = FootprintSpec.Paint(p);
            BrushVisible = true;
            if (beyond)
                PlacementReason.Clear();
            bool refused = !beyond && PlacementReason.Update(player);
            Tone = beyond ? PreviewTone.OutOfReach : refused || PreviewStatus.Blocked ? PreviewTone.Blocked : PreviewTone.Normal;
        }

        /// <summary>The brush's aim within reach, else the ground under the crosshair when it lies beyond reach.</summary>
        private static bool Aim(Player player, out Vector3 aim, out bool beyond)
        {
            Reach = GhostAim.Reach(player);
            Vector3 eye = player.m_eye != null ? player.m_eye.position : player.transform.position;
            beyond = false;
            aim = BrushState.AimPoint;
            if (!BrushState.HasAim && !FarGround(out aim))
                return false;
            Distance = Vector3.Distance(eye, aim);
            beyond = !BrushState.HasAim;
            return BrushState.HasAim || Distance > Reach;
        }

        private static bool FarGround(out Vector3 point)
        {
            point = Vector3.zero;
            if (GameCamera.instance == null)
                return false;
            if (terrainMask == 0)
                terrainMask = LayerMask.GetMask("terrain");
            Transform camera = GameCamera.instance.transform;
            if (!Physics.Raycast(camera.position, camera.forward, out RaycastHit hit, FarRay, terrainMask))
                return false;
            point = hit.point;
            return true;
        }

        /// <summary>The outline colour for the current tone.</summary>
        public static Color ToneColour(Color normal)
        {
            switch (Tone)
            {
                case PreviewTone.Blocked: return PreviewSettings.BlockedColour.Value;
                case PreviewTone.OutOfReach: return PreviewSettings.OutOfReachColour.Value;
                default: return normal;
            }
        }
    }
}
