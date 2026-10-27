using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// A ship cannon's breeching, on every peer that draws (<see cref="KwShipRecoil"/>) and once on the bench: on each side
    /// a rope from the ringbolt ahead of the bracket (fixed on the piece) back to the eyebolt on the bracket's step
    /// (on the carriage, which rolls back at a shot), and from there up to the ring on the breech's side (on the gun). Its
    /// length from the ringbolt to the eyebolt is what it measures with the carriage rolled right back
    /// (<see cref="KwShipSpec.Recoil"/>): there it is drawn taut and straight; run out, the slack sags down onto the deck and
    /// a little out from the carriage, in <see cref="Pieces"/> lengths of rope, so it lies there as the slack of a real
    /// breeching does. Each length is a unit line of the rope (along its node's +z) laid and stretched between two points,
    /// as the weapons' cords are, all under the piece itself. Hidden with its ringbolts while the cannon is hauled
    /// (<see cref="Show"/>).
    /// </summary>
    public sealed class KwBreeching
    {
        public const string Anchor = "br_anchor_", Eye = "br_eye_", Ring = "br_ring_", Rope = "br_rope_";

        /// <summary>The lengths the rope from the ringbolt to the eyebolt is drawn in (one more from the eyebolt to the breech).</summary>
        public const int Pieces = 10;

        private static readonly float[] Sides = { 1f, -1f };

        private sealed class Side
        {
            public Transform From = null!, Through = null!, To = null!;
            public Transform[] Ropes = null!;
            public float Length, Out;
        }

        private readonly Transform root;
        private readonly Side[] sides;
        private readonly float lift;
        private readonly Vector3[] curve = new Vector3[Pieces + 1];
        private Renderer[]? drawn;
        private bool hidden;

        private KwBreeching(Transform root, Side[] sides, float lift) => (this.root, this.sides, this.lift) = (root, sides, lift);

        /// <summary>A side's rope name: left (+1) "l", right (-1) "r".</summary>
        public static string Named(string part, float side) => part + (side > 0f ? "l" : "r");

        /// <summary>A cannon's breeching, found by name under it; null when a part of it is missing.</summary>
        public static KwBreeching? Of(Transform root, KwShipSpec ship)
        {
            Side[] found = new Side[Sides.Length];
            for (int i = 0; i < Sides.Length; i++)
            {
                Side? side = Find(root, ship, Sides[i]);
                if (side == null)
                    return null;
                found[i] = side;
            }
            return new KwBreeching(root, found, ship.Rope + 0.004f);
        }

        /// <summary>Lays both ropes between their points as they are now.</summary>
        public void Lay()
        {
            foreach (Side side in sides)
            {
                Vector3 from = Local(side.From), through = Local(side.Through);
                Vector3 sag = new Vector3(-side.Out * 0.7f, -1f, 0f).normalized;
                Curve(from, through, side.Length, sag, lift, curve);
                for (int i = 0; i < Pieces; i++)
                    Piece(side.Ropes[i], curve[i], curve[i + 1]);
                Piece(side.Ropes[Pieces], through, Local(side.To));
            }
        }

        /// <summary>
        /// Both ropes and the ringbolts they are made fast to (the piece's model) shown, or hidden: while the cannon is
        /// hauled, and where it was left with nothing to make them fast to (<see cref="KwBreechingTie"/>).
        /// </summary>
        public void Show(bool shown)
        {
            if (hidden != shown)
                return;
            hidden = !shown;
            foreach (Renderer part in drawn ??= Parts())
            {
                if (part != null)
                    part.enabled = shown;
            }
        }

        // The ropes' lengths and the piece's model (the ringbolts on their deck plates).
        private Renderer[] Parts()
        {
            List<Renderer> parts = new List<Renderer>();
            foreach (Side side in sides)
            {
                foreach (Transform rope in side.Ropes)
                    parts.AddRange(rope.GetComponentsInChildren<Renderer>(true));
            }
            Transform? model = root.Find("model");
            if (model != null)
                parts.AddRange(model.GetComponents<Renderer>());
            return parts.ToArray();
        }

        /// <summary>
        /// Points along a rope of a length from a to b (the piece's own axes, Unity's: y up from the deck at 0): straight
        /// when it is taut (or stretched), else sagging along <paramref name="sag"/> by as much as the slack needs (a
        /// parabola's depth for its length), no lower than <paramref name="lift"/> off the deck.
        /// </summary>
        public static void Curve(Vector3 a, Vector3 b, float length, Vector3 sag, float lift, Vector3[] into)
        {
            float span = Vector3.Distance(a, b), slack = length - span;
            float depth = slack > 0f ? Mathf.Sqrt(3f * span * slack / 8f) : 0f;
            for (int i = 0; i < into.Length; i++)
            {
                float t = (float)i / (into.Length - 1);
                Vector3 p = Vector3.Lerp(a, b, t) + sag * (depth * 4f * t * (1f - t));
                p.y = Mathf.Max(p.y, lift);
                into[i] = p;
            }
        }

        /// <summary>A rope's length from the ringbolt to the eyebolt: as far apart as they are with the carriage rolled right back.</summary>
        public static float Taut(KwShipSpec ship, float side) => Vector3.Distance(ship.Anchor(side), ship.Eye(side) - new Vector3(ship.Recoil, 0f, 0f));

        private static Side? Find(Transform root, KwShipSpec ship, float side)
        {
            Transform? from = root.Find(Named(Anchor, side)), yaw = root.Find(KwArm.YawName), gun = KwShipRig.Gun(root);
            Transform? through = yaw != null ? yaw.Find(Named(Eye, side)) : null, to = gun != null ? gun.Find(Named(Ring, side)) : null;
            Transform[] ropes = new Transform[Pieces + 1];
            for (int i = 0; i < ropes.Length; i++)
            {
                Transform? rope = root.Find(Named(Rope, side) + i);
                if (rope == null)
                    return null;
                ropes[i] = rope;
            }
            if (from == null || through == null || to == null)
                return null;
            return new Side { From = from, Through = through, To = to, Ropes = ropes, Length = Taut(ship, side), Out = side };
        }

        // A length of rope (a unit line along its node's +z) from one point to the other, in the piece's own axes.
        private static void Piece(Transform rope, Vector3 a, Vector3 b)
        {
            Vector3 run = b - a;
            rope.localPosition = a;
            rope.localRotation = run.sqrMagnitude > 1e-8f ? Quaternion.LookRotation(run) : Quaternion.identity;
            rope.localScale = new Vector3(1f, 1f, Mathf.Max(0.001f, run.magnitude));
        }

        private Vector3 Local(Transform t) => root.InverseTransformPoint(t.position);
    }
}
