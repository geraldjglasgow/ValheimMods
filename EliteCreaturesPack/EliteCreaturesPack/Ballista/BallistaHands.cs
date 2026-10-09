using UnityEngine;

namespace EliteCreaturesPack.Ballista
{
    /// <summary>
    /// The holder's upper body on a ballista, on every peer that draws, after the animator has posed them (their legs and
    /// feet are the game's own walk, <see cref="BallistaOperator.Steer"/>): both fists on the handles while aiming; for
    /// the pull, a lean in, both hands taking the string either side of the groove and drawing it back to the latch;
    /// for the load, the right hand taking a missile from the hip, carrying it over the groove and laying it down in it,
    /// then back to its handle. All from the ZDO and the timeline, so every screen shows the same. The hands reach out
    /// over the last stride up to the ballista (in about an eighth of a second) and let go as quickly.
    /// </summary>
    public sealed class BallistaHands
    {
        private const float Grab = 0.075f, Lift = 0.14f, OnSeat = 0.03f, Ease = 8f, Reached = 0.2f, Reaching = 0.6f;

        private Player? player;
        private BallistaRig? rig;
        private float weight;

        public void Pose(BallistaParts parts, in BallistaState s, Vector3 centre, float dt)
        {
            if (!Follow(parts, s, dt) || rig == null || player == null)
            {
                parts.Held?.SetActive(false);
                return;
            }
            Transform body = player.transform;
            rig.Lean(BallistaTimeline.Lean(s) * weight, body);
            float onString = BallistaTimeline.OnString(s);
            Vector3 across = parts.Pitch.right * Grab;
            rig.Left.Reach(Vector3.Lerp(parts.GripLeft.position, centre - across, onString), weight, body);
            rig.Right.Reach(Vector3.Lerp(RightHand(parts, s, rig, body), centre + across, onString), weight, body);
            Missile(parts, s, rig, body);
        }

        /// <summary>
        /// The holder (kept while the hands let go after they did) and how fully the hands are on the ballista; false
        /// when nobody's are.
        /// </summary>
        private bool Follow(BallistaParts parts, in BallistaState s, float dt)
        {
            Player? holder = s.User != 0L ? Player.GetPlayer(s.User) : null;
            if (holder != null && holder != player)
            {
                (player, rig, weight) = (holder, BallistaRig.Of(holder), 0f);
            }
            weight = Mathf.MoveTowards(weight, holder != null ? AtBallista(parts, holder, s) : 0f, Ease * dt);
            if (weight > 0.001f)
            {
                return true;
            }
            if (holder == null)
            {
                (player, rig) = (null, null);
            }
            return false;
        }

        /// <summary>1 once the holder's feet are at the spot the timeline asks for, 0 while still walking up to it.</summary>
        private static float AtBallista(BallistaParts parts, Player holder, in BallistaState s)
        {
            Vector3 spot = Vector3.Lerp(parts.StandAim.position, parts.StandLoad.position, BallistaTimeline.Close(s));
            Vector3 off = holder.transform.position - spot;
            off.y = 0f;
            return 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(Reached, Reaching, off.magnitude));
        }

        /// <summary>The right fist's target: its handle, or along the load's path (hip, over the groove, in it).</summary>
        private static Vector3 RightHand(BallistaParts parts, in BallistaState s, BallistaRig rig, Transform body)
        {
            (HandSpot from, HandSpot to, float along) = BallistaTimeline.RightHand(s);
            return Vector3.Lerp(Spot(parts, from, rig, body), Spot(parts, to, rig, body), along);
        }

        private static Vector3 Spot(BallistaParts parts, HandSpot spot, BallistaRig rig, Transform body) => spot switch
        {
            HandSpot.Hip => rig.Hips.position + body.right * 0.24f - body.forward * 0.06f + Vector3.down * 0.05f,
            HandSpot.Above => parts.Seat.position + parts.Pitch.up * Lift,
            HandSpot.Seat => parts.Seat.position + parts.Pitch.up * OnSeat,
            _ => parts.GripRight.position,
        };

        /// <summary>The missile in the right fist while it is carried: hanging along the thigh at the hip, along the groove over it.</summary>
        private static void Missile(BallistaParts parts, in BallistaState s, BallistaRig rig, Transform body)
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
