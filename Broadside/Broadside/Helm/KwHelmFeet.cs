using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// The helmsman's feet at a tiller or a captain's wheel, on his own game: nothing holds him, the game's own movement
    /// walks him to his stand beside the grip or behind the wheel (<see cref="IKwHelmGrip.Stand"/>), at a jog while walking
    /// up to the helm, then at a walk. Once there his feet stay planted while his hand follows the peg;
    /// he steps again only once the stand has moved a stride from where he stopped, or the peg has come in too close to
    /// him, and gives up on a spot he cannot reach after a moment without moving. He faces the way he walks while walking
    /// up, then the bow, so a step across the deck plays as the game's side step. All measured in the ship's own space,
    /// so the moving deck, which the game's own physics carries him on, changes nothing.
    /// </summary>
    public sealed class KwHelmFeet
    {
        private const float Arrive = 0.06f, Restart = 0.28f, Slow = 0.3f, Stride = 0.4f, GiveUp = 0.6f, Still = 0.1f;
        private const float TurnRate = 540f, Walking = 0.5f;

        private bool arrived, settled;
        private float stuck;
        private Vector3 restSpot, restFeet, lastFeet;

        public void Start(Transform ship, Player me)
        {
            (arrived, settled, stuck) = (false, false, 0f);
            lastFeet = ship.InverseTransformPoint(me.transform.position);
        }

        /// <summary>
        /// Each physics step, after the game has worked out his controls (none: the helm takes his movement keys): where his
        /// feet go, toward his stand in the world, or nowhere.
        /// </summary>
        public void Steer(Transform ship, Vector3 spot, bool crowded, Player me)
        {
            Vector3 to = spot - me.transform.position;
            to.y = 0f;
            float distance = to.magnitude, pace = arrived ? Stride : 1f;
            bool stays = Stays(ship, spot, me, distance, crowded);
            me.m_moveDir = stays || distance < 1e-4f ? Vector3.zero : to / distance * Mathf.Min(pace, distance / Slow);
        }

        // Whether the feet stay put this step (settled with the stand still near, arrived, or blocked), in the ship's space.
        private bool Stays(Transform ship, Vector3 at, Player me, float distance, bool crowded)
        {
            Vector3 spot = ship.InverseTransformPoint(at), feet = ship.InverseTransformPoint(me.transform.position);
            float speed = (feet - lastFeet).magnitude / Mathf.Max(1e-4f, Time.fixedDeltaTime);
            lastFeet = feet;
            if (settled && !crowded && (spot - restSpot).magnitude < Restart && (feet - restFeet).magnitude < Restart)
                return true;
            stuck = speed < Still ? stuck + Time.fixedDeltaTime : 0f;
            if (distance < Arrive || stuck > GiveUp)
            {
                (settled, arrived, stuck, restSpot, restFeet) = (true, true, 0f, spot, feet);
                return true;
            }
            settled = false;
            return false;
        }

        /// <summary>
        /// His facing, in place of the game's: the way he walks while still walking up to the helm, then the way the helm
        /// faces (the bow at a tiller, the wheel's own forward at a captain's wheel), turned into fast; the turn for the
        /// game's turning animation (radians a second).
        /// </summary>
        public float Face(Vector3 bow, Player me, float dt)
        {
            Vector3 travel = me.m_moveDir, wanted = !arrived && travel.magnitude > Walking ? travel : bow;
            wanted.y = 0f;
            if (wanted.sqrMagnitude < 1e-6f)
                return 0f;
            Quaternion facing = Quaternion.LookRotation(wanted);
            float delta = Utils.GetYawDeltaAngle(me.transform.rotation, facing);
            me.transform.rotation = Quaternion.RotateTowards(me.transform.rotation, facing, TurnRate * dt);
            return Mathf.Abs(delta) > 1f ? TurnRate * Mathf.Sign(delta) * Mathf.Deg2Rad : 0f;
        }
    }
}
