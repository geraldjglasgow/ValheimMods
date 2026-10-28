using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// A rig piece shot down (the user, 2026-10-26: "the ropes attaching the sail make a noise and split completely, and the
    /// sail falls over and then just disappears, for now"; then "it needs to take a lot longer to fall over, as it normally
    /// would: three to four and a half seconds, with the ropes snapping at different times, a little out of order"), on
    /// every screen that sees its fall mark come (<see cref="KwRigDamage"/>), never for one already down when the ship loads:
    /// the wood cracks at the break and what is past it (its look's high part, <see cref="KwRigLook.High"/>, and what of its
    /// fitting goes with it, <see cref="KwMastFallSplit"/>: a mast's pennant and the sails hung from above the break, a
    /// bowsprit's jibs; the sails below stay) begins to go, slowly at first and faster as it goes, about the break, a mast or
    /// cap away from the shot, a bowsprit down, over <see cref="Shortest"/> to <see cref="Longest"/> s (doubled from 3 to 4.5
    /// s, the user, 2026-10-27). Each rope tied on above the break
    /// stays fast at its far end, drawn taut as the piece leans, until its own moment in the first part of the fall (in
    /// no order), when it snaps with the game's own sound (<see cref="KwRigSound"/>) and whips for <see cref="Whip"/> s before
    /// it is gone (<see cref="KwFallRope"/>). The length and the snaps' moments come from the fall mark, so every screen sees
    /// the same fall. As it goes its wood creaks and splinters, more as it leans (<see cref="KwMastCreak"/>); at the end it
    /// lands with a crash of timber and is hidden. Only drawn; nothing is sent.
    /// </summary>
    public sealed class KwMastFall : MonoBehaviour
    {
        public const float Shortest = 6f, Longest = 9f, Whip = 0.35f, SnapsWithin = 0.6f;
        private readonly List<(Transform Part, Vector3 At, Quaternion Turn)> parts = new List<(Transform, Vector3, Quaternion)>();
        private readonly List<KwFallRope> ropes = new List<KwFallRope>();
        private KwRigLook look = null!;
        private Vector3 pivot, axis, top;
        private float most, time, falling;
        private KwRigMark mark;
        private KwMastCreak? creak;

        /// <summary>A piece's fall begun, from its fall mark (where it broke, the heading it falls to).</summary>
        public static void Begin(KwRigLook look, KwRigMark fall)
        {
            Stop(look);
            KwMastFall made = look.gameObject.AddComponent<KwMastFall>();
            (made.look, made.mark) = (look, fall);
            made.Aim(look.Piece, fall);
            made.falling = Mathf.Lerp(Shortest, Longest, Chance(fall, 0));
            made.creak = new KwMastCreak(fall, made.falling);
            made.parts.Add((look.High, Vector3.zero, Quaternion.identity));
            Transform? goes = KwMastFallSplit.Gather(look, fall);
            if (goes != null)
                made.parts.Add((goes, look.Piece.Piece.Position, look.Piece.Piece.Rotation));
            made.Ropes(KwMastFallSplit.Ropes(look, fall), fall);
            made.Sounds();
        }

        // A share from 0 to 1, the same on every screen for this fall and this number (the fall's length, each rope's moment).
        internal static float Chance(KwRigMark fall, int i) =>
            Mathf.Repeat(Mathf.Sin(fall.A * 12.9898f + fall.B * 78.233f + i * 37.719f) * 43758.5453f, 1f);

        // About what it turns and how far: about the break, square to its spar and to where it falls.
        private void Aim(KwRigPiece piece, KwRigMark fall)
        {
            KwRigSpar main = piece.Main;
            pivot = piece.ToShip(main.At(Mathf.Clamp(fall.A, 0f, main.Length)));
            top = piece.ToShip(main.C);
            Vector3 along = piece.Piece.Rotation * KwMesh.ToUnity(main.Axis);
            float heading = fall.B * Mathf.Deg2Rad;
            Vector3 toward = piece.Kind == KwRigPiece.SpritKind ? Vector3.down : new Vector3(Mathf.Sin(heading), 0f, Mathf.Cos(heading));
            axis = Vector3.Cross(along, toward);
            if (axis.sqrMagnitude < 1e-4f)
                axis = Vector3.Cross(along, Vector3.forward);
            axis.Normalize();
            most = piece.Kind == KwRigPiece.SpritKind ? 60f : 95f;
        }

        // Each rope that parts with the fall (KwMastFallSplit.Ropes), held at its far end until it parts.
        private void Ropes(List<LineRenderer> lines, KwRigMark fall)
        {
            Transform ship = look.transform.parent;
            foreach (LineRenderer line in lines)
            {
                float at = Chance(fall, ropes.Count + 1) * SnapsWithin * falling;
                ropes.Add(new KwFallRope(line, ship, at));
            }
        }

        // The wood cracking at the break, the piece beginning to go (each rope snaps at its own moment).
        private void Sounds()
        {
            Transform ship = look.transform.parent;
            KwRigSound.Play(KwRigSound.Crack, ship.TransformPoint(pivot));
        }

        private void Update()
        {
            time += Time.deltaTime;
            foreach (KwFallRope rope in ropes)
                rope.Show(time, look.transform.parent);
            creak?.Tick(time, look.transform.parent.TransformPoint(pivot));
            float angle = most * Mathf.Pow(Mathf.Clamp01(time / falling), 2.4f);
            Quaternion turn = Quaternion.AngleAxis(angle, axis);
            foreach ((Transform part, Vector3 at, Quaternion rest) in parts)
            {
                if (part != null)
                    part.SetLocalPositionAndRotation(pivot + turn * (at - pivot), turn * rest);
            }
            if (time < falling)
                return;
            KwRigSound.Play(KwRigSound.Land, look.transform.parent.TransformPoint(pivot + turn * (top - pivot)), 0.8f, 0.9f);
            Gone(look, mark);
            Destroy(this);
        }

        /// <summary>A piece's falling stopped where it is (put back, or another fall begun).</summary>
        public static void Stop(KwRigLook look)
        {
            KwMastFall? falling = look.GetComponent<KwMastFall>();
            if (falling == null)
                return;
            foreach (KwFallRope rope in falling.ropes)
                rope.Restore();
            DestroyImmediate(falling);
        }

        /// <summary>
        /// A piece shown down after a fall (its high part and what went with it hidden, the ropes tied on above the break
        /// gone) or standing, fall null (everything back in its place, its ropes drawn).
        /// </summary>
        public static void Gone(KwRigLook look, KwRigMark? fall)
        {
            look.High.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            look.High.gameObject.SetActive(fall == null);
            if (look.Fitting == null)
                return;
            look.Fitting.SetLocalPositionAndRotation(look.Piece.Piece.Position, look.Piece.Piece.Rotation);
            look.Fitting.gameObject.SetActive(true);
            if (fall == null)
            {
                KwMastFallSplit.Scatter(look);
                foreach (LineRenderer line in look.Fitting.GetComponentsInChildren<LineRenderer>(true))
                    line.enabled = true;
                return;
            }
            Transform? goes = KwMastFallSplit.Gather(look, fall.Value);
            goes?.SetLocalPositionAndRotation(look.Piece.Piece.Position, look.Piece.Piece.Rotation);
            goes?.gameObject.SetActive(false);
            foreach (LineRenderer line in KwMastFallSplit.Ropes(look, fall.Value))
                line.enabled = false;
        }
    }

    /// <summary>
    /// One rope of a falling rig piece (<see cref="KwMastFall"/>): fast at its far end (where it was tied, kept in the ship's
    /// frame) and drawn taut as the piece leans until its moment, then it snaps with its sound, whips and is gone; its
    /// points are put back as they were for when the piece is put back.
    /// </summary>
    internal sealed class KwFallRope
    {
        private readonly LineRenderer line;
        private readonly Vector3[] points, shown;
        private readonly Vector3 across, pin;
        private readonly float snapAt;
        private bool snapped, gone;

        public KwFallRope(LineRenderer line, Transform ship, float snapAt)
        {
            this.line = line;
            this.snapAt = snapAt;
            points = new Vector3[line.positionCount];
            line.GetPositions(points);
            shown = (Vector3[])points.Clone();
            Vector3 run = points[points.Length - 1] - points[0], side = Vector3.Cross(run, Vector3.up);
            across = side.sqrMagnitude > 1e-6f ? side.normalized : Vector3.right;
            pin = ship.InverseTransformPoint(line.transform.TransformPoint(points[points.Length - 1]));
        }

        /// <summary>The rope at this moment of the fall: held, whipping, or gone.</summary>
        public void Show(float time, Transform ship)
        {
            if (gone || line == null)
                return;
            if (time < snapAt)
                Hold(ship);
            else if (time < snapAt + KwMastFall.Whip)
                Lash(time - snapAt);
            else
                Part();
        }

        // Its far end kept where it was tied, the rest of it moved along in proportion so it reads as drawn taut.
        private void Hold(Transform ship)
        {
            Vector3 end = line.transform.InverseTransformPoint(ship.TransformPoint(pin)), shift = end - points[points.Length - 1];
            for (int k = 0; k < points.Length; k++)
                shown[k] = points[k] + shift * (k / (points.Length - 1f));
            line.SetPositions(shown);
        }

        // Parted: its snap heard once where it broke, then a fading lash across its run.
        private void Lash(float since)
        {
            if (!snapped)
            {
                snapped = true;
                KwRigSound.Play(KwRigSound.Snap, line.transform.TransformPoint(points[points.Length - 1]));
            }
            float fade = 1f - since / KwMastFall.Whip, swing = 0.3f * Mathf.Sin(since * 2f * Mathf.PI * 5f) * Mathf.Max(0f, fade);
            for (int k = 0; k < points.Length; k++)
                shown[k] = points[k] + across * (swing * Mathf.Sin(Mathf.PI * k / (points.Length - 1f)));
            line.SetPositions(shown);
        }

        private void Part()
        {
            gone = true;
            line.SetPositions(points);
            line.enabled = false;
        }

        /// <summary>Its points put back as they were drawn.</summary>
        public void Restore()
        {
            if (line != null)
                line.SetPositions(points);
        }
    }
}
