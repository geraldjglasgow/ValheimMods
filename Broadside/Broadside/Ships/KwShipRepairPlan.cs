using System;
using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// One thing the hammer's repair mends on a launched ship in one click (<see cref="KwShipRepair"/>): its kind, what it
    /// is (<see cref="A"/>, <see cref="B"/>, <see cref="C"/> by kind: a hole's number; a railing's piece and part; a rig
    /// piece, its part and the mark type a mend takes, <see cref="KwRigDamage.Mends"/>; a fallen piece), and where it is in
    /// the ship's frame.
    /// </summary>
    public readonly struct KwRepairStep
    {
        public const int Patched = 0, Hole = 1, Rail = 2, Rig = 3, Fallen = 4;

        public KwRepairStep(int kind, int a, int b, int c, Vector3 at) => (Kind, A, B, C, At) = (kind, a, b, c, at);

        public int Kind { get; }
        public int A { get; }
        public int B { get; }
        public int C { get; }
        public Vector3 At { get; }

        /// <summary>The step as text, the same for the same thing to mend.</summary>
        public string Key => FormattableString.Invariant($"{Kind}:{A}:{B}:{C}");
    }

    /// <summary>
    /// Everything a launched ship's damage needs from the hammer, one click each, in the order the hammer works through it
    /// (the user, 2026-10-26: "when they start repairing it with the hammer, these wooden repair kits should just go away
    /// ... and that hull piece is actually repaired ... things like sails and anything that was shot and fell off the ship
    /// incrementally start generating back"): the hull first, then the rig. Patched holes (the patch comes off and the hole
    /// is mended), then holes with no patch, then broken railings, then the rig's marks (a sail's holes with its own yard's,
    /// boom's and gaff's notches; a spar's notches; a cap's chips; a rudder's lost chunks) on pieces that stand, then the
    /// fallen masts, bowsprits and caps, each put back for its cost. Within a kind, the nearest to where the hammer aims
    /// first. Plain: from the ship's damage as this peer reads it.
    /// </summary>
    public static class KwShipRepairPlan
    {
        public static List<KwRepairStep> Of(List<KwHullHole> holes, ICollection<int> patched, List<KwRailStretch> stretches,
            ICollection<(int Piece, int Part)> broken, KwRigParts? parts, List<KwRigMark> marks, Vector3 aim)
        {
            List<KwRepairStep> steps = new List<KwRepairStep>();
            foreach (KwHullHole hole in holes)
                steps.Add(new KwRepairStep(patched.Contains(hole.Id) ? KwRepairStep.Patched : KwRepairStep.Hole, hole.Id, 0, 0, hole.Place));
            foreach (KwRailStretch s in stretches)
            {
                if (broken.Contains((s.Piece, s.Part)))
                    steps.Add(new KwRepairStep(KwRepairStep.Rail, s.Piece, s.Part, 0, (s.A + s.B) / 2f + s.Up * (KwRailStretches.Height / 2f)));
            }
            if (parts != null)
                Rig(steps, parts, marks);
            steps.Sort((x, y) => x.Kind != y.Kind ? x.Kind.CompareTo(y.Kind) : (x.At - aim).sqrMagnitude.CompareTo((y.At - aim).sqrMagnitude));
            return steps;
        }

        // The rig's steps: each standing piece's marks a mend at a time, then each fallen piece.
        private static void Rig(List<KwRepairStep> steps, KwRigParts parts, List<KwRigMark> marks)
        {
            HashSet<int> fallen = new HashSet<int>(marks.FindAll(m => m.Type == KwRigMark.Fall).ConvertAll(m => m.Piece));
            HashSet<string> seen = new HashSet<string>();
            foreach (KwRigMark mark in marks)
            {
                KwRigPiece? piece = parts.Get(mark.Piece);
                if (piece == null || fallen.Contains(mark.Piece))
                    continue;
                (char type, int part) = Mend(mark, piece);
                KwRepairStep step = new KwRepairStep(KwRepairStep.Rig, piece.Index, part, type, Middle(piece));
                if (seen.Add(step.Key))
                    steps.Add(step);
            }
            foreach (KwRigPiece piece in parts.Pieces)
            {
                if (fallen.Contains(piece.Index))
                    steps.Add(new KwRepairStep(KwRepairStep.Fallen, piece.Index, 0, 0, piece.ToShip(piece.Main.A)));
            }
        }

        /// <summary>
        /// The mend that takes a mark (<see cref="KwRigDamage.Mends"/>, as the hammer aimed at the part asks it): a sail's
        /// hole by its sail; a cap's chip or notch by the cap's chips; a rudder's chunk by its chunks; any other notch by its spar.
        /// </summary>
        public static (char Type, int Part) Mend(KwRigMark mark, KwRigPiece piece)
        {
            if (mark.Type == KwRigMark.Hole)
                return (KwRigMark.Hole, mark.Part);
            if (piece.Kind == KwRigPiece.CapKind)
                return (KwRigMark.Chip, -1);
            if (mark.Type == KwRigMark.Chunk)
                return (KwRigMark.Chunk, -1);
            return (mark.Type, mark.Part);
        }

        // A rig piece's middle in the ship's frame: halfway up its main spar.
        private static Vector3 Middle(KwRigPiece piece) => (piece.ToShip(piece.Main.A) + piece.ToShip(piece.Main.C)) / 2f;

        /// <summary>What a step mends, in words for the player.</summary>
        public static string Words(KwRepairStep step, KwRigParts? parts)
        {
            KwRigPiece? piece = parts?.Get(step.A);
            return step.Kind switch
            {
                KwRepairStep.Patched => "The patch comes off and the hole is mended",
                KwRepairStep.Hole => "A hole in the hull is mended",
                KwRepairStep.Rail => "A railing is mended",
                KwRepairStep.Fallen => $"{piece?.Called ?? "The rig"} is put back",
                _ => step.C == KwRigMark.Hole ? "A sail is mended" : $"{piece?.Called ?? "The rig"} is mended",
            };
        }
    }
}
