using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>One edge of a dock section's deck, at the deck's top, as <see cref="KwPierDeck.Edges"/> lists them.</summary>
    public readonly struct KwPierEdge
    {
        public KwPierEdge(string side, Vector3 a, Vector3 b, Vector3 outward, bool water, bool open, IReadOnlyList<Vector2> joins)
        {
            (Side, A, B, Outward, Water, Open, Joins) = (side, a, b, outward, water, open, joins);
        }

        /// <summary>"left", "right" (seen looking from its start to its end), "start" or "end".</summary>
        public string Side { get; }

        /// <summary>The edge's two ends in the world, at the deck's top (a side's on its curb's outer face).</summary>
        public Vector3 A { get; }
        public Vector3 B { get; }

        /// <summary>Level, pointing out from the deck across this edge.</summary>
        public Vector3 Outward { get; }

        /// <summary>Over the water: the ground under it (at its pilings) below the sea's level, at half or more of its stations.</summary>
        public bool Water { get; }

        /// <summary>Not met by another loaded dock section anywhere along it (an end or start where none continues, a side no branch leaves).</summary>
        public bool Open { get; }

        /// <summary>
        /// Where other loaded sections meet it (a branch off a side, the section carrying on from an end): spans (from, to) in
        /// metres along it from <see cref="A"/> toward <see cref="B"/>, in order. There the edge is open into the other deck:
        /// no curb, no water; free everywhere else.
        /// </summary>
        public IReadOnlyList<Vector2> Joins { get; }
    }

    /// <summary>
    /// A loaded dock section's deck, read-only, worked out from its saved shape and where it stands (DOCKS.md, "Reading a
    /// dock"), for other code (the Decorate tool) to place things on it without blocking it: the deck's top, its centre
    /// line's two ends, its width and length, its material, the curbs along its sides, and its four edges with which face
    /// the water. Read on any peer that has the section loaded; null while the section has no shape yet.
    /// </summary>
    public sealed class KwPierDeck
    {
        private const float Joins = 0.3f;

        private KwPierDeck(KwPier pier, KwPierSpec spec)
        {
            Transform t = pier.transform;
            (Piece, Width, Length, Material, JoinedAtStart) = (pier.GetComponent<Piece>(), spec.Width, spec.Length, pier.Kind, spec.Joined);
            (Start, Forward, Right) = (t.position, Flat(t.forward), Flat(t.right));
            End = Start + Forward * spec.Length;
            KwPierKind kind = pier.Material;
            (CurbWidth, CurbHeight, PilingIn, CapFoot) = (kind.Curb.x, kind.Curb.y, kind.PostIn, kind.CapFoot);
            Edges = KwPierEdges.Of(this, spec, pier.Gaps);
            (left, right) = (Stations(spec, kind, false), Stations(spec, kind, true));
        }

        private readonly List<float> left, right;

        /// <summary>The section's piece (its owner removes it; its position is <see cref="Start"/>).</summary>
        public Piece Piece { get; }

        /// <summary>The deck's top: its walking height, the same all along it.</summary>
        public float Top => Start.y;

        /// <summary>The centre line's two ends in the world, at the deck's top.</summary>
        public Vector3 Start { get; }
        public Vector3 End { get; }

        /// <summary>Level unit vectors: along the section from its start to its end, and to its right.</summary>
        public Vector3 Forward { get; }
        public Vector3 Right { get; }

        public float Width { get; }
        public float Length { get; }

        /// <summary>The material's id: "wood", "corewood" or "finewood".</summary>
        public string Material { get; }

        /// <summary>Whether its start meets another dock (continued from its end, or branched off its side).</summary>
        public bool JoinedAtStart { get; }

        /// <summary>The curbs along both sides, on the deck: each this wide in from the edge and this high over the deck's top. Clear walking width is Width less two of them.</summary>
        public float CurbWidth { get; }
        public float CurbHeight { get; }

        /// <summary>How far in from a side edge the pilings' axes stand (under the deck, inside its width).</summary>
        public float PilingIn { get; }

        /// <summary>The deck box's underside below its top (the cap beams' underside, where the pilings start; negative).</summary>
        public float CapFoot { get; }

        /// <summary>The piling stations along one side (metres from the start, in order) where that side has a piling.</summary>
        public IReadOnlyList<float> Pilings(bool rightSide) => rightSide ? right : left;

        /// <summary>Its edges: left, right, start, end.</summary>
        public IReadOnlyList<KwPierEdge> Edges { get; }

        /// <summary>A point on the deck's top from metres across (right positive) and along from its start.</summary>
        public Vector3 At(float across, float along) => Start + Right * across + Forward * along;

        /// <summary>A dock section's deck; null for any other piece, or one not built yet.</summary>
        public static KwPierDeck? Of(Piece? piece)
        {
            KwPier? pier = piece != null ? piece.GetComponent<KwPier>() : null;
            return Of(pier);
        }

        public static KwPierDeck? Of(KwPier? pier) => pier != null && pier.Spec != null ? new KwPierDeck(pier, pier.Spec) : null;

        /// <summary>Every built dock section loaded on this peer whose deck comes within a distance of a point.</summary>
        public static List<KwPierDeck> Near(Vector3 point, float radius)
        {
            List<KwPierDeck> near = new List<KwPierDeck>();
            foreach (KwPier pier in KwPier.Loaded)
            {
                KwPierDeck? deck = pier != null ? Of(pier) : null;
                if (deck != null && deck.Distance(point) <= radius)
                    near.Add(deck);
            }
            return near;
        }

        /// <summary>Level distance from a point to the deck (0 on it).</summary>
        public float Distance(Vector3 point)
        {
            Vector3 d = point - Start;
            float along = Mathf.Clamp(Vector3.Dot(d, Forward), 0f, Length), across = Vector3.Dot(d, Right);
            float off = Mathf.Max(0f, Mathf.Abs(across) - Width * 0.5f);
            return new Vector2(Vector3.Dot(d, Forward) - along, off).magnitude;
        }

        /// <summary>Whether another loaded section starts, joined, where this one ends (it continues straight on).</summary>
        public bool ContinuedAtEnd() => ContinuedAt(End, Forward);

        /// <summary>Whether its start meets another dock: joined there, or another section built on from it the other way.</summary>
        public bool ContinuedAtStart() => JoinedAtStart || ContinuedAt(Start, -Forward);

        // Another loaded section starting, joined, at a point and running on that way.
        private bool ContinuedAt(Vector3 point, Vector3 way)
        {
            foreach (KwPier other in KwPier.Loaded)
            {
                if (other != null && other.Spec != null && other.Spec.Joined && other.transform != Piece.transform
                    && (other.transform.position - point).sqrMagnitude < Joins * Joins && Vector3.Dot(Flat(other.transform.forward), way) > 0.99f)
                    return true;
            }
            return false;
        }

        // Where a side's pilings stand along the section (KwPierShape.PostZ), at the stations it has one.
        private static List<float> Stations(KwPierSpec spec, KwPierKind kind, bool rightSide)
        {
            List<float> along = new List<float>();
            for (int i = 0; i < spec.Stations; i++)
            {
                if ((rightSide ? spec.Depths[i].y : spec.Depths[i].x) > 0f)
                    along.Add(KwPierShape.PostZ(spec, kind, i));
            }
            return along;
        }

        private static Vector3 Flat(Vector3 v) => new Vector3(v.x, 0f, v.z).normalized;
    }
}
