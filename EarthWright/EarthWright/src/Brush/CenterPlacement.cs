using EarthWright.Actions;
using EarthWright.Core;
using EarthWright.Terrain;
using UnityEngine;

namespace EarthWright.Brush
{
    /// <summary>
    /// Turns the aimed ground point into the footprint centre: aim-at-edge moves the centre forward (along the camera's
    /// view, flattened) by the distance from the centre to the footprint's edge in that direction, so the crosshair marks
    /// the near edge; grid mode and the snap key then put the centre on whole world metres, where the terrain vertices
    /// are. Ramps and roads never aim at the edge (their clicks are points), but still snap.
    /// </summary>
    public static class CenterPlacement
    {
        public static Vector3 From(Vector3 aim, ToolAction action)
        {
            Vector3 center = aim;
            if (BrushState.AimAtEdge && !EntryKinds.IsPath(action))
                center += EdgeOffset();
            if (BrushState.GridMode || SnapHeld())
            {
                center.x = Mathf.Round(center.x);
                center.z = Mathf.Round(center.z);
            }
            return center;
        }

        private static Vector3 EdgeOffset()
        {
            GameCamera camera = GameCamera.instance;
            if (camera == null)
                return Vector3.zero;
            Vector3 forward = camera.transform.forward;
            forward.y = 0f;
            if (forward.sqrMagnitude < 0.0001f)
                return Vector3.zero;
            forward.Normalize();
            return forward * EdgeDistance(forward);
        }

        /// <summary>Centre to edge along a flat direction, for the published shape, size and rotation.</summary>
        public static float EdgeDistance(Vector3 direction)
        {
            switch (BrushState.Shape)
            {
                case BrushShape.Square:
                case BrushShape.Frame: return BoxDistance(direction, BrushState.Radius, BrushState.Radius);
                case BrushShape.Rectangle: return BoxDistance(direction, BrushState.Radius, BrushState.Radius2);
                default: return BrushState.Radius;
            }
        }

        /// <summary>
        /// A box with half width along its local X and half depth along its local Z, turned by the rotation (degrees,
        /// clockwise seen from above, as <c>Quaternion.Euler(0, rotation, 0)</c>).
        /// </summary>
        private static float BoxDistance(Vector3 direction, float halfWidth, float halfDepth)
        {
            Vector3 local = Quaternion.Euler(0f, -BrushState.Rotation, 0f) * direction;
            float x = Mathf.Abs(local.x) > 0.0001f ? halfWidth / Mathf.Abs(local.x) : float.MaxValue;
            float z = Mathf.Abs(local.z) > 0.0001f ? halfDepth / Mathf.Abs(local.z) : float.MaxValue;
            return Mathf.Min(x, z);
        }

        /// <summary>The snap key held on its own: not while Ctrl is down, so pressing Ctrl+Z (undo) does not snap the brush.</summary>
        private static bool SnapHeld()
        {
            return Keys.Held(ControlSettings.SnapKey) && !Input.GetKey(KeyCode.LeftControl) && !Input.GetKey(KeyCode.RightControl);
        }
    }
}
