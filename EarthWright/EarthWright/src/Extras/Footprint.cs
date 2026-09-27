using EarthWright.Actions;
using EarthWright.Brush;
using EarthWright.Terrain;
using UnityEngine;

namespace EarthWright.Extras
{
    /// <summary>
    /// The ground area an object-affecting entry (uproot) works on, in the brush's own terms: centre, shape, the two
    /// radii and the rotation, tested in world metres on the horizontal plane. Taken from the brush while it is on this
    /// entry; otherwise a circle of the entry's own radius around the ghost.
    /// </summary>
    public struct Footprint
    {
        public Vector3 Center;
        public BrushShape Shape;
        public float Radius;
        public float Radius2;
        public float Rotation;

        public static Footprint Of(ToolAction action, Vector3 ghostPosition)
        {
            bool brush = BrushState.Active && BrushState.Action != null && BrushState.Action.Id == action.Id;
            if (!brush)
                return new Footprint { Center = ghostPosition, Shape = BrushShape.Circle, Radius = action.BaseRadius };
            return new Footprint
            {
                Center = BrushState.Center,
                Shape = BrushState.Shape,
                Radius = action.Resizable ? BrushState.Radius : action.BaseRadius,
                Radius2 = BrushState.Radius2,
                Rotation = BrushState.Rotation,
            };
        }

        /// <summary>A distance from the centre that contains the whole footprint.</summary>
        public float Reach
        {
            get
            {
                float size = Mathf.Max(Radius, Radius2);
                bool cornered = Shape == BrushShape.Square || Shape == BrushShape.Rectangle || Shape == BrushShape.Frame;
                return cornered ? size * 1.415f : size;
            }
        }

        /// <summary>The point lies inside the footprint (horizontal position only).</summary>
        public bool Contains(Vector3 point)
        {
            Vector3 local = Quaternion.Inverse(Quaternion.Euler(0f, Rotation, 0f)) * (point - Center);
            float x = Mathf.Abs(local.x);
            float z = Mathf.Abs(local.z);
            float d2 = local.x * local.x + local.z * local.z;
            switch (Shape)
            {
                case BrushShape.Square: return x <= Radius && z <= Radius;
                case BrushShape.Rectangle: return x <= Radius && z <= Radius2;
                case BrushShape.Ring: return d2 <= Radius * Radius && d2 >= Radius2 * Radius2;
                case BrushShape.Frame:
                    float edge = Mathf.Max(x, z);
                    return edge <= Radius && edge >= Radius - Radius2;
                default: return d2 <= Radius * Radius;
            }
        }
    }
}
