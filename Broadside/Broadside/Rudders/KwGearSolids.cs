using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// A keel as the steering gear's shaft must pass it (<see cref="KwGearShip"/>): its timber along its line, from its
    /// underside to its top, and its post, in its own kit frame (the piece's origin <see cref="KwKeelBody.Depth"/> under
    /// its line's start, as <see cref="KwKeel"/> pivots it).
    /// </summary>
    internal sealed class KwGearKeel
    {
        private readonly KwGearPose pose;
        private readonly KwKeelSpec spec;

        public KwGearKeel(KwGearPose pose, KwKeelSpec spec) => (this.pose, this.spec) = (pose, spec);

        /// <summary>Whether a point of the rudder's frame lies in the keel or its post, or within a margin of them.</summary>
        public bool Holds(Vector3 kit, float margin)
        {
            Vector3 k = pose.Local(kit) - new Vector3(0f, 0f, spec.Body.Depth);
            if (k.x < -margin || k.x > spec.Length + margin)
                return false;
            float rise = spec.RiseAt(k.x);
            if (Mathf.Abs(k.y) <= spec.Body.Half + margin && k.z >= rise - spec.Body.Depth - margin && k.z <= rise + margin)
                return true;
            return spec.Post && Post(k, margin);
        }

        // The post at the keel's far end: a little wider than the keel, from its underside up to its top.
        private bool Post(Vector3 k, float margin)
        {
            float half = spec.Body.PostDepth / 2f, x = spec.Length + (spec.Flat ? half : -half);
            return Mathf.Abs(k.x - x) <= half + margin && Mathf.Abs(k.y) <= spec.Body.PostHalf + margin
                && k.z <= spec.RiseAt(x) + spec.PostAbove + margin && k.z >= spec.RiseAt(x) - spec.Body.Depth - margin;
        }
    }

    /// <summary>
    /// A cabin as the steering gear's shaft must pass it (<see cref="KwGearShip"/>): its room from its floor to its roof's
    /// top, as wide as the hull under it, in its own kit frame. A cabin on the deck the shaft hangs under is over it; one
    /// on a lower deck stands in its way.
    /// </summary>
    internal sealed class KwGearCabin
    {
        private readonly KwGearPose pose;
        private readonly KwCabinSpec spec;

        public KwGearCabin(KwGearPose pose, KwCabinSpec spec) => (this.pose, this.spec) = (pose, spec);

        /// <summary>Where the cabin stands in the rudder's frame, and what it saves (a wheel on its roof climbs through it, <see cref="KwGearRooms"/>).</summary>
        public KwGearPose Pose => pose;
        public KwCabinSpec Spec => spec;

        /// <summary>Whether a point of the rudder's frame lies in the cabin, or within a margin of it.</summary>
        public bool Holds(Vector3 kit, float margin)
        {
            Vector3 k = pose.Local(kit);
            if (k.x < spec.X0 - margin || k.x > spec.X1 + margin || k.z < spec.Floor - margin || k.z > spec.Top + margin)
                return false;
            return Mathf.Abs(k.y) <= KwShapes.InsideWidth(KwCabinOutline.Up(spec, k.x).Half, Mathf.Min(k.z, spec.Top)) + margin;
        }
    }
}
