using UnityEngine;

namespace Wayfare.SeaGates
{
    /// <summary>The rectangle a gate's surface fills: between the inner faces of its two pillars, from
    /// <see cref="BelowWater"/> under sea level up to the lower of the two pillar tops, facing along the gate's
    /// Forward. Measured once from the pillars' renderer bounds when the surface is built (pillars never move), so the
    /// surface fits whatever piece the pillar is a copy of.</summary>
    internal readonly struct SeaGateSurfaceFrame
    {
        internal const float BelowWater = 1f;
        private const float DefaultPillarHeight = 6f;  // when a pillar has no renderer to measure
        private const float DefaultInset = 0.5f;       // half a pillar's thickness, likewise
        private const float MaxInset = 2f;
        private const float MinTopAboveWater = 2f;
        private const float MinWidth = 1f;

        /// <summary>World centre of the rectangle.</summary>
        internal readonly Vector3 Center;
        /// <summary>The gate's frame: local x across the span, y up, z out of the front side.</summary>
        internal readonly Quaternion Rotation;
        internal readonly float Width;
        internal readonly float Height;

        internal SeaGateSurfaceFrame(LoadedGate gate)
        {
            GateGeometry geometry = gate.Geometry;
            float half = geometry.Width * 0.5f;
            bool hasA = TryBounds(gate.Anchor, out Bounds a);
            bool hasB = TryBounds(gate.Partner, out Bounds b);
            // The anchor sits at local x = -half, the partner at +half (Right runs from the anchor to the partner).
            float left = hasA ? Mathf.Clamp(Along(geometry, a) + Extent(geometry, a), -half, -half + MaxInset) : -half + DefaultInset;
            float right = hasB ? Mathf.Clamp(Along(geometry, b) - Extent(geometry, b), half - MaxInset, half) : half - DefaultInset;
            float top = Mathf.Min(Top(gate.Anchor, hasA, a), Top(gate.Partner, hasB, b)) - geometry.Mid.y;
            top = Mathf.Max(top, MinTopAboveWater);
            Width = Mathf.Max(right - left, MinWidth);
            Height = top + BelowWater;
            Center = geometry.ToWorld(new Vector3((left + right) * 0.5f, (top - BelowWater) * 0.5f, 0f));
            Rotation = geometry.Frame;
        }

        /// <summary>The world bounds of a pillar's visible meshes (particles and disabled parts left out).</summary>
        private static bool TryBounds(SeaGatePillar pillar, out Bounds bounds)
        {
            bounds = default;
            bool any = false;
            foreach (Renderer renderer in pillar.GetComponentsInChildren<Renderer>())
            {
                if (!renderer.enabled || !(renderer is MeshRenderer || renderer is SkinnedMeshRenderer))
                    continue;
                if (any)
                    bounds.Encapsulate(renderer.bounds);
                else
                    bounds = renderer.bounds;
                any = true;
            }
            return any;
        }

        private static float Top(SeaGatePillar pillar, bool measured, Bounds bounds) =>
            measured ? bounds.max.y : pillar.transform.position.y + DefaultPillarHeight;

        /// <summary>Where the bounds' centre lies across the span.</summary>
        private static float Along(GateGeometry geometry, Bounds bounds) => geometry.ToLocal(bounds.center).x;

        /// <summary>Half the bounds' size across the span.</summary>
        private static float Extent(GateGeometry geometry, Bounds bounds) =>
            Mathf.Abs(geometry.Right.x) * bounds.extents.x + Mathf.Abs(geometry.Right.z) * bounds.extents.z;
    }
}
