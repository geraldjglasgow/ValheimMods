using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>What holds a rope's end on the ship (<see cref="KwRigTie"/>).</summary>
    public enum KwTieKind
    {
        /// <summary>A shroud's: an iron chainplate down the side's outside, a pair of deadeyes on it and a lanyard between them.</summary>
        Deadeye,

        /// <summary>A brace's or sheet's on a railing or the gunwale: a wooden cleat.</summary>
        Cleat,

        /// <summary>A rope's on a deck or a roof with no railing: an eyebolt with a ring.</summary>
        Eyebolt,
    }

    /// <summary>
    /// Where a rope ends on its ship and the fitting it is made fast to there (<see cref="KwRigTies"/>), in the rigged piece's
    /// kit frame: a shroud's deadeyes over the gunwale or the railing, the lower on its chainplate, the upper on the rope's
    /// line a lanyard's length above it; a cleat on the railing's top rail (its inner face) or on the gunwale; an eyebolt on a
    /// deck or a roof. Drawn by <see cref="KwTieShapes"/>.
    /// </summary>
    public sealed class KwRigTie
    {
        /// <summary>A deadeye's radius; the lanyard's length between the deadeyes' middles; how high a chainplate stands over what it passes.</summary>
        public const float Block = 0.065f, Lanyard = 0.3f, Over = 0.1f;

        private KwRigTie(KwTieKind kind, Vector3 seat, Vector3 outward, Vector3 end, KwBiomeTier tier)
        {
            (Kind, Seat, Out, End, Tier) = (kind, seat, outward, end, tier);
        }

        public KwTieKind Kind { get; }

        /// <summary>Where the fitting sits: the lower deadeye's middle, a cleat's or an eyebolt's foot.</summary>
        public Vector3 Seat { get; }

        /// <summary>Level, out of the ship: the face a cleat or chainplate is fixed to faces it (a cleat on a rail's inner face, the other way).</summary>
        public Vector3 Out { get; }

        public Vector3 Way => new Vector3(-Out.y, Out.x, 0f);

        /// <summary>Where the rope ends: the upper deadeye's top, a cleat's horns, an eyebolt's ring.</summary>
        public Vector3 End { get; }

        public KwBiomeTier Tier { get; }

        /// <summary>A cleat on an upright face, facing against <see cref="Out"/>: a railing's top rail's inner face, a mast's side (else on the gunwale's top).</summary>
        public bool OnRail { get; private set; }

        /// <summary>A shroud's: the upper deadeye's middle, and the chainplate's points from the lower deadeye down.</summary>
        public Vector3 Upper { get; private set; }
        public List<Vector3> Strap { get; } = new List<Vector3>();

        /// <summary>A rope from a point made fast at an edge by a kind of fitting (a cleat on a roof with no railing is an eyebolt).</summary>
        public static KwRigTie At(KwRigEdge edge, KwTieKind kind, Vector3 from)
        {
            if (kind == KwTieKind.Deadeye)
                return Deadeye(edge, from);
            if (edge.Railed)
                return Railed(edge);
            if (edge.Roof != null)
                return Eyebolt(edge.At - edge.Out * 0.12f, edge.Out, edge.Tier);
            Vector3 seat = edge.At - edge.Out * ((KwPlanks.Thick - KwPlanks.Lap) / 2f);
            return new KwRigTie(KwTieKind.Cleat, seat, edge.Out, seat + new Vector3(0f, 0f, 0.05f), edge.Tier);
        }

        /// <summary>An eyebolt on a deck or a roof at a point, its ring the rope's end.</summary>
        public static KwRigTie Eyebolt(Vector3 seat, Vector3 outward, KwBiomeTier tier) =>
            new KwRigTie(KwTieKind.Eyebolt, seat, outward, seat + new Vector3(0f, 0f, 0.075f), tier);

        /// <summary>A cleat on a mast's side at a rope's end (a halyard's), its face out from the mast's axis toward the end.</summary>
        public static KwRigTie OnMast(Vector3 end, float radius, KwBiomeTier tier)
        {
            Vector3 level = new Vector3(end.x, end.y, 0f), outward = level.sqrMagnitude > 1e-6f ? level.normalized : Vector3.right;
            Vector3 seat = outward * radius + new Vector3(0f, 0f, end.z);
            return new KwRigTie(KwTieKind.Cleat, seat, -outward, seat + outward * 0.05f + new Vector3(0f, 0f, 0.01f), tier) { OnRail = true };
        }

        // A cleat on the railing's top rail, on its inner face (a hull railing stands over the edge, a roof railing inside it).
        private static KwRigTie Railed(KwRigEdge edge)
        {
            float inner = edge.Roof == null ? KwRailing.Half : 2f * KwRailing.Half;
            Vector3 seat = edge.At - edge.Out * inner + new Vector3(0f, 0f, KwRailing.RailHeight + KwRailing.TopRail / 2f);
            return new KwRigTie(KwTieKind.Cleat, seat, edge.Out, seat - edge.Out * 0.05f + new Vector3(0f, 0f, 0.01f), edge.Tier) { OnRail = true };
        }

        // Deadeyes over the gunwale (or over the railing's top rail), out past the side's face; the chainplate from the lower
        // down past the edge to the side's outer face under it; the upper on the rope's line, its top the rope's end.
        private static KwRigTie Deadeye(KwRigEdge edge, Vector3 from)
        {
            float off = edge.Face + 0.03f, high = edge.Railed ? KwRailing.RailHeight + KwRailing.TopRail : 0f;
            Vector3 top = edge.At + edge.Out * off + new Vector3(0f, 0f, high + Over);
            Vector3 lower = top + new Vector3(0f, 0f, Block), lead = (from - lower).normalized;
            Vector3 upper = lower + lead * Lanyard;
            KwRigTie tie = new KwRigTie(KwTieKind.Deadeye, lower, edge.Out, upper + lead * Block, edge.Tier) { Upper = upper };
            tie.Strap.AddRange(new[] { top, edge.At + edge.Out * off, edge.Below + edge.Out * 0.008f });
            return tie;
        }
    }
}
