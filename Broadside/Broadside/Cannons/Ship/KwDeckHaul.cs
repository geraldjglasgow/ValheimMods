using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// A ship cannon hauled about a launched Broadside ship's deck, where it rides the ship (the ship's child, its own body
    /// gone: <see cref="KwShipRider"/>), on its hauler's game, which owns its ZDO while he hauls it. Each frame it is drawn
    /// after him in the ship's own frame as a cart is (<see cref="KwDeckMath.Follow"/>: its middle rolls only along its way,
    /// its rear at the handle's reach, no faster than the carriage's top speed nor round than its turn), stood on the deck
    /// under its four trucks and tilted with it, and moved there only when every truck has deck within a step of where it
    /// stands and nothing of the ship's is in its way (<see cref="KwDeckFloor"/>); else it tries the turn alone and the
    /// roll alone, and failing both stays put, its hauler held back from walking on away from its rear (<see cref="Leash"/>).
    /// Its pose is the piece's own under the ship, set here and written to its rider record (<see cref="KwShipRider"/>'s
    /// <see cref="KwShipRider.PosKey"/> and <see cref="KwShipRider.RotKey"/>) ten times a second while it moves and as he
    /// lets go, so every other peer eases it after him (<see cref="KwShipRiderMove"/>) and it stays where it was left,
    /// saved too. Everything is in the ship's frame: the ship rolling, heeling or
    /// sailing moves nothing.
    /// </summary>
    public sealed class KwDeckHaul
    {
        // How often the pose is written while it moves; the most it moves or turns a frame (a slow frame moves it less);
        // how far from its rear's handle the hauler may walk before he is held back.
        private const float Every = 0.1f, MostStep = 0.05f, MostTurn = 3f * Mathf.Deg2Rad, Slack = 0.25f;

        private readonly KwArmTow tow;
        private readonly KwShipSpec spec;
        private readonly Vector3[] feet = new Vector3[4];
        private readonly float[] heights = new float[4], tried = new float[4];
        private KwDeckFloor? floor;
        private KwDeckPose pose;
        private float writtenAt = -1f;
        private bool moved;

        public KwDeckHaul(KwArmTow tow, KwShipSpec spec) => (this.tow, this.spec) = (tow, spec);

        private Transform Self => tow.Arm.transform;

        private Transform Ship => Self.parent;

        /// <summary>As he takes it up: where it stands now, the deck under its trucks, and what it stands in already.</summary>
        public void Begin()
        {
            KwDeckFloor made = floor = new KwDeckFloor(Ship, Self, spec, tow.Arm.Def);
            pose = KwDeckMath.From(Self.localPosition, Self.localRotation * Vector3.forward);
            KwDeckMath.Feet(pose, spec, feet);
            for (int i = 0; i < feet.Length; i++)
                heights[i] = made.Height(feet[i], pose.Ahead, pose.At.y, out float height) ? height : pose.At.y;
            made.Settle(Self.localPosition, Self.localRotation);
            moved = false;
        }

        /// <summary>One frame on the hauler's game while he holds it: drawn after him where it can go, and written when due.</summary>
        public void Step(Player hauler, float dt)
        {
            if (floor == null)
                return;
            Vector3 at = Ship.InverseTransformPoint(hauler.transform.position);
            float reach = -KwMesh.ToUnity(spec.Handle).z, most = Mathf.Min(spec.Top * dt, MostStep), turn = Mathf.Min(spec.Spin * Mathf.Deg2Rad * dt, MostTurn);
            KwDeckPose want = KwDeckMath.Follow(pose, at, reach, most, turn);
            if (!KwDeckMath.Same(want, pose))
                moved |= Try(want) || Try(new KwDeckPose { At = pose.At, Ahead = want.Ahead }) || Try(new KwDeckPose { At = want.At, Ahead = pose.Ahead });
            Write(false);
        }

        /// <summary>As he lets go: its pose written where it was left.</summary>
        public void End()
        {
            Write(true);
            floor = null;
        }

        /// <summary>
        /// The hauler's walk (his game's move direction) on the deck: steered as he pushes it, as a man swings a pushed cart's
        /// end (<see cref="KwCartMath.Pushed"/>), and held back from going on away from its rear's handle once he is more
        /// than <see cref="Slack"/> from it (it could not follow him there, or not as fast).
        /// </summary>
        public Vector3 Leash(Player hauler, Vector3 walk)
        {
            if (floor == null)
                return walk;
            Vector3 local = KwCartMath.Pushed(Ship.InverseTransformDirection(walk), pose.Ahead);
            Vector3 off = Ship.InverseTransformPoint(hauler.transform.position) - KwDeckMath.Handle(pose, spec);
            off.y = 0f;
            float into = off.magnitude > Slack ? Vector3.Dot(local, off.normalized) : 0f;
            if (into > 0f)
                local -= off.normalized * into;
            return Ship.TransformDirection(local);
        }

        // A pose tried: deck under every truck within a step of where it stands, no steeper than it may tilt, nothing of the
        // ship's in its way; taken (the piece put there) when it is fine.
        private bool Try(in KwDeckPose next)
        {
            if (KwDeckMath.Same(next, pose))
                return false;
            KwDeckMath.Feet(next, spec, feet);
            for (int i = 0; i < feet.Length; i++)
            {
                if (!floor!.Height(feet[i], next.Ahead, heights[i], out tried[i]))
                    return false;
            }
            if (!KwDeckMath.Rest(next, tried, spec, out Vector3 at, out Quaternion turn) || floor!.Blocked(at, turn))
                return false;
            pose = new KwDeckPose { At = at, Ahead = next.Ahead };
            System.Array.Copy(tried, heights, heights.Length);
            Self.SetLocalPositionAndRotation(at, turn);
            floor.Settle(at, turn);
            return true;
        }

        // The pose written to the rider record once it has moved: no more often than every tenth of a second while it moves,
        // and as he lets go; only by its owner.
        private void Write(bool now)
        {
            if (!moved || !now && Time.time - writtenAt < Every || !tow.Link.Valid || !tow.Link.View.IsOwner())
                return;
            Record(tow.Link.Zdo, Self.localPosition, Self.localRotation);
            (writtenAt, moved) = (Time.time, false);
        }

        // Its new pose in the ship's frame written to its rider record, on its owner; every other peer eases it there.
        private static void Record(ZDO zdo, Vector3 at, Quaternion turn)
        {
            zdo.Set(KwShipRider.PosHash, at);
            zdo.Set(KwShipRider.RotHash, turn);
        }
    }
}
