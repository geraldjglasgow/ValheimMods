using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// How a rig piece's look is cut for its damage (<see cref="KwRigLook"/>), plain data from the piece and the rig's
    /// condition, in the piece's kit axes: which of its drawn spars go (<see cref="Cut"/>: each one notched, broken through,
    /// or the column of a fallen piece; every triangle wholly inside one is left out, <see cref="Keeps"/>), which broken
    /// hoops go (a sail along the ship's rings up the mast, <see cref="KwSailHurt.Hoops"/>), the spars drawn again in their
    /// place (<see cref="Spars"/>: notched, a broken yard only to its break, a fallen piece's column as its stump and the part
    /// that falls, both splintered), and for a fallen piece what falls (<see cref="High"/>: everything past the break along
    /// its column; all of a cap).
    /// </summary>
    public sealed class KwRigLookPlan
    {
        private const float Inside = 0.004f, HoopTube = 0.036f;

        public List<KwRigSpar> Cut { get; } = new List<KwRigSpar>();

        /// <summary>Broken hoops: their height on the column's axis (kit z) and their ring's radius.</summary>
        public List<(float Z, float Radius)> Hoops { get; } = new List<(float Z, float Radius)>();

        public List<KwRigSparDraw> Spars { get; } = new List<KwRigSparDraw>();

        /// <summary>Where along the piece's column it broke and fell; null while it stands.</summary>
        public float? Fall { get; private set; }

        public KwRigPiece Piece { get; }

        /// <summary>Whether nothing is to be cut: the piece's whole look drawn as it is.</summary>
        public bool Whole => Cut.Count == 0 && Hoops.Count == 0 && !Fall.HasValue;

        private KwRigLookPlan(KwRigPiece piece) => Piece = piece;

        public static KwRigLookPlan Of(KwRigPiece piece, KwRigCondition condition)
        {
            KwRigLookPlan plan = new KwRigLookPlan(piece);
            plan.Fall = condition.FallOf(piece.Index)?.A;
            if (piece.Kind == KwRigPiece.CapKind)
                return plan;
            plan.Main(condition.Notches(piece.Index, KwRigSpar.Column));
            for (int i = 0; i < piece.Sails.Count; i++)
            {
                if (piece.Sails[i].Square)
                    plan.Yard(condition, i);
                else
                    plan.Ringed(piece.Sails[i], condition.Sail(piece.Index, i));
            }
            return plan;
        }

        // The column (a bowsprit's spar): notched, or broken into its stump and the part that falls.
        private void Main(List<KwRigMark> notches)
        {
            KwRigSpar main = Piece.Main;
            if (notches.Count == 0 && !Fall.HasValue)
                return;
            Cut.Add(main);
            if (!Fall.HasValue)
            {
                Spars.Add(new KwRigSparDraw(main, notches, 0f, main.Length, false, false, false));
                return;
            }
            float at = Mathf.Clamp(Fall.Value, 0.05f, main.Length - 0.05f);
            Spars.Add(new KwRigSparDraw(main, notches, 0f, at, false, true, false));
            Spars.Add(new KwRigSparDraw(main, notches, at, main.Length, true, false, true));
        }

        // A square sail's yard: notched, or drawn only to where it broke through, from the end still held.
        private void Yard(KwRigCondition condition, int i)
        {
            List<KwRigMark> notches = condition.Notches(Piece.Index, i);
            if (notches.Count == 0)
                return;
            KwRigSpar yard = KwRigPiece.Yard(Piece.Sails[i], i);
            KwSailHurt hurt = condition.Sail(Piece.Index, i);
            Cut.Add(yard);
            bool high = Fall.HasValue && Rises(yard.At(yard.Length / 2f));
            if (hurt.YardBreak < 0f)
                Spars.Add(new KwRigSparDraw(yard, notches, 0f, yard.Length, false, false, high));
            else if (hurt.YardSide > 0)
                Spars.Add(new KwRigSparDraw(yard, notches, 0f, hurt.YardBreak, false, true, high));
            else
                Spars.Add(new KwRigSparDraw(yard, notches, hurt.YardBreak, yard.Length, true, false, high));
        }

        // A sail along the ship's broken hoops.
        private void Ringed(KwSailLayout s, KwSailHurt hurt)
        {
            List<float> at = KwRigPiece.Hoops(s);
            for (int k = 0; k < at.Count && k < hurt.Hoops.Length; k++)
            {
                if (hurt.Hoops[k])
                    Hoops.Add((at[k] - 0.1f, s.Radius + 0.05f));
            }
        }

        // Whether a kit point is past the break along the column: on the part that falls.
        private bool Rises(Vector3 kit) => Fall.HasValue && Piece.Main.Along(kit) > Fall.Value;

        /// <summary>Whether a drawn triangle stays: not wholly inside a spar that goes, nor a broken hoop.</summary>
        public bool Keeps(Vector3 a, Vector3 b, Vector3 c)
        {
            foreach (KwRigSpar spar in Cut)
            {
                if (spar.Holds(a, Inside) && spar.Holds(b, Inside) && spar.Holds(c, Inside))
                    return false;
            }
            foreach ((float z, float radius) in Hoops)
            {
                if (Hoop(a, z, radius) && Hoop(b, z, radius) && Hoop(c, z, radius))
                    return false;
            }
            return true;
        }

        // A point of a hoop's ring round the mast's axis at a height.
        private static bool Hoop(Vector3 p, float z, float radius) =>
            Mathf.Abs(p.z - z) < HoopTube && Mathf.Abs(new Vector2(p.x, p.y).magnitude - radius) < HoopTube;

        /// <summary>Whether a drawn triangle falls with the piece: past its break (its middle); all of a cap.</summary>
        public bool High(Vector3 a, Vector3 b, Vector3 c) => Fall.HasValue && (Piece.Kind == KwRigPiece.CapKind || Rises((a + b + c) / 3f));
    }

    /// <summary>A spar drawn again for its damage (<see cref="KwRigDamageSpar"/>): from and to along it, its notches, its ends splintered, and whether it falls.</summary>
    public readonly struct KwRigSparDraw
    {
        public KwRigSparDraw(KwRigSpar spar, List<KwRigMark> notches, float from, float to, bool raggedFrom, bool raggedTo, bool high)
        {
            (Spar, Notches, From, To, RaggedFrom, RaggedTo, High) = (spar, notches, from, to, raggedFrom, raggedTo, high);
        }

        public KwRigSpar Spar { get; }
        public List<KwRigMark> Notches { get; }
        public float From { get; }
        public float To { get; }
        public bool RaggedFrom { get; }
        public bool RaggedTo { get; }
        public bool High { get; }

        /// <summary>The seed its splinters are drawn by, the same on every peer.</summary>
        public int Seed => Spar.Part * 7919 + Mathf.RoundToInt((From + To) * 100f);
    }
}
