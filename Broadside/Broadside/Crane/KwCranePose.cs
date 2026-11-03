using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// A crane's pose: the turret's turn (degrees, 0 as built), the boom's slide out from rest (0 to
    /// <see cref="KwCargo.Slide"/> m) and the head's drop under the boom's tip (<see cref="KwCargo.MinDrop"/> to
    /// <see cref="KwCargo.MaxDrop"/> m). The operator's game moves its own; the crane's ZDO holds the owner's; every other
    /// screen follows the ZDO's smoothly (<see cref="Follow"/>).
    /// </summary>
    public sealed class KwCranePose
    {
        private const float Smooth = 8f, SnapTurn = 90f, SnapLength = 3f;

        public float Turn, Slide, Drop;

        public KwCranePose(float drop) => Drop = drop;

        /// <summary>The pose the ZDO holds; a crane never worked hangs its head as the model was made.</summary>
        public void Read(ZDO zdo, float restDrop)
        {
            Turn = zdo.GetFloat(KwCargo.TurnKey);
            Slide = zdo.GetFloat(KwCargo.SlideKey);
            Drop = zdo.GetFloat(KwCargo.DropKey, restDrop);
        }

        public void Write(ZDO zdo)
        {
            zdo.Set(KwCargo.TurnKey, Turn);
            zdo.Set(KwCargo.SlideKey, Slide);
            zdo.Set(KwCargo.DropKey, Drop);
        }

        public void CopyFrom(KwCranePose other) => (Turn, Slide, Drop) = (other.Turn, other.Slide, other.Drop);

        public bool Same(KwCranePose other) =>
            Mathf.Abs(Mathf.DeltaAngle(Turn, other.Turn)) < 0.05f && Mathf.Abs(Slide - other.Slide) < 0.002f && Mathf.Abs(Drop - other.Drop) < 0.002f;

        /// <summary>Eases toward a pose another game sent (it arrives a few times a second); a big jump is taken at once.</summary>
        public void Follow(KwCranePose target, float dt)
        {
            float t = 1f - Mathf.Exp(-Smooth * dt);
            bool far = Mathf.Abs(Mathf.DeltaAngle(Turn, target.Turn)) > SnapTurn || Mathf.Abs(Slide - target.Slide) > SnapLength
                || Mathf.Abs(Drop - target.Drop) > SnapLength;
            Turn = far ? target.Turn : Mathf.LerpAngle(Turn, target.Turn, t);
            Slide = far ? target.Slide : Mathf.Lerp(Slide, target.Slide, t);
            Drop = far ? target.Drop : Mathf.Lerp(Drop, target.Drop, t);
        }

        /// <summary>
        /// The operator's step: turn (x), slide (z) and hoist (up raises) at the crane's speeds, Run twice as fast; the drop
        /// is kept between its limits and above <paramref name="lowest"/> (the ground or a crate under the head).
        /// </summary>
        public void Step(Vector3 move, float hoist, bool run, float lowest, float dt)
        {
            float speed = run ? KwCargo.RunFactor * dt : dt;
            Turn = Mathf.Repeat(Turn + move.x * KwCargo.TurnSpeed * speed, 360f);
            Slide = Mathf.Clamp(Slide + move.z * KwCargo.SlideSpeed * speed, 0f, KwCargo.Slide);
            float most = Mathf.Clamp(lowest, KwCargo.MinDrop, KwCargo.MaxDrop);
            Drop = Mathf.Clamp(Drop - hoist * KwCargo.HoistSpeed * speed, KwCargo.MinDrop, Mathf.Max(KwCargo.MinDrop, most));
        }
    }
}
