using MannedWeapons;
using UnityEngine;

namespace EliteCreaturesPack.Ballista
{
    /// <summary>
    /// Where the holder's hands go on a ballista, from the ZDO and the timeline, so every screen shows the same (the
    /// library reaches them there by two-bone IK and eases them on and off over the last stride up): both fists on the
    /// handles while aiming; for the pull, a lean in, both hands taking the string either side of the groove and drawing it
    /// back to the latch; for the load, the right hand taking a missile from the hip, carrying it over the groove and
    /// laying it down in it, then back to its handle.
    /// </summary>
    public static class BallistaHands
    {
        private const float Grab = 0.075f, Lift = 0.14f, OnSeat = 0.03f;

        /// <summary>The lean and both fists' targets now; `centre` is the string's centre this frame.</summary>
        public static OperatorPose Pose(BallistaParts parts, in WeaponState s, Vector3 centre, OperatorRig rig, Transform body)
        {
            float onString = BallistaTimeline.OnString(s);
            Vector3 across = parts.Pitch.right * Grab;
            Vector3 left = Vector3.Lerp(parts.GripLeft.position, centre - across, onString);
            Vector3 right = Vector3.Lerp(RightHand(parts, s, rig, body), centre + across, onString);
            return new OperatorPose(BallistaTimeline.Lean(s), left, right);
        }

        /// <summary>The right fist's target: its handle, or along the load's path (hip, over the groove, in it).</summary>
        private static Vector3 RightHand(BallistaParts parts, in WeaponState s, OperatorRig rig, Transform body)
        {
            (HandSpot from, HandSpot to, float along) = BallistaTimeline.RightHand(s);
            return Vector3.Lerp(Spot(parts, from, rig, body), Spot(parts, to, rig, body), along);
        }

        private static Vector3 Spot(BallistaParts parts, HandSpot spot, OperatorRig rig, Transform body) => spot switch
        {
            HandSpot.Hip => rig.Hips.position + body.right * 0.24f - body.forward * 0.06f + Vector3.down * 0.05f,
            HandSpot.Above => parts.Seat.position + parts.Pitch.up * Lift,
            HandSpot.Seat => parts.Seat.position + parts.Pitch.up * OnSeat,
            _ => parts.GripRight.position,
        };

        /// <summary>The missile in the right fist while it is carried: hanging along the thigh at the hip, along the groove over it.</summary>
        public static void Missile(BallistaParts parts, in WeaponState s, OperatorRig rig, Transform body)
        {
            bool show = BallistaTimeline.MissileInHand(s);
            parts.Held?.SetActive(show);
            if (!show || parts.Held == null)
            {
                return;
            }
            (HandSpot from, HandSpot _, float along) = BallistaTimeline.RightHand(s);
            Quaternion hanging = Quaternion.LookRotation(Vector3.down, body.forward), level = parts.Pitch.rotation;
            float turn = from == HandSpot.Grip ? 0f : from == HandSpot.Hip ? along : 1f;
            parts.Held.transform.SetPositionAndRotation(rig.Right.Grip, Quaternion.Slerp(hanging, level, turn));
        }
    }
}
