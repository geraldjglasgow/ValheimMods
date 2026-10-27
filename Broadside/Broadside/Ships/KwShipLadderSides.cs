using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// How far out of a ship's centre line its side is at a place along and a height, on one side (the ship's kit frame: x
    /// forward, y to port, z up), as the boarding ladder stands off it (<see cref="KwShipLadderFind"/>): a planking part's
    /// outside, its laps included, from its keel line to its sheer; a cabin's raised side (the hull carried straight up from
    /// the sheer to the roof) on a side at the hull over its length. 0 where the part does not reach.
    /// </summary>
    public static class KwShipLadderSides
    {
        public static float Planking(KwAnchorHull hull, float x, float side, float z)
        {
            Vector3 k = hull.Pose.Local(new Vector3(x, 0f, z));
            if (k.x < hull.From || k.x > hull.To || k.z > hull.Sheer(k.x).y + 0.02f)
                return 0f;
            float own = Own(hull.Pose, x, side, z, k);
            return Mathf.Abs(hull.Pose.Out(new Vector3(k.x, own * hull.Outside(k.x, k.z), k.z)).y);
        }

        public static float Raised(KwRigCabin cabin, float x, float side, float z)
        {
            KwCabinSpec spec = cabin.Spec;
            Vector3 k = cabin.Pose.Local(new Vector3(x, 0f, z));
            float own = Own(cabin.Pose, x, side, z, k);
            if (k.x < spec.X0 || k.x > spec.X1 || k.z > spec.Top || !(own > 0f ? spec.Left : spec.Right).AtHull)
                return 0f;
            float across = KwShapes.WidthAt(KwCabinOutline.Up(spec, k.x).Half, k.z) + KwPlanks.Lap;
            return Mathf.Abs(cabin.Pose.Out(new Vector3(k.x, own * across, k.z)).y);
        }

        // Which of a part's own sides (+1 its left) is the ship's side given.
        private static float Own(KwAnchorPose pose, float x, float side, float z, Vector3 middle) =>
            pose.Local(new Vector3(x, side, z)).y >= middle.y ? 1f : -1f;
    }
}
