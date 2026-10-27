using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>The Cabin tool window's sizes: the cabin's height over the deck, and how far its back, front, left and right are drawn in.</summary>
    public readonly struct KwCabinSizes
    {
        public KwCabinSizes(float height, float back, float front, float left, float right)
        {
            (Height, Back, Front, Left, Right) = (height, back, front, left, right);
        }

        public float Height { get; }
        public float Back { get; }
        public float Front { get; }
        public float Left { get; }
        public float Right { get; }

        /// <summary>A built cabin's sizes as they were asked (what its text saves, before the fit), as its edit window opens on them.</summary>
        public static KwCabinSizes Of(KwCabinSpec spec)
        {
            KwCabinSpec a = spec.Asked;
            return new KwCabinSizes(a.Height, a.Back.Inset, a.Front.Inset, a.Left.Inset, a.Right.Inset);
        }
    }

    /// <summary>A deck section along a keel line: the deck part, where it starts and ends along the line, its height over the line's keel at its lowest and highest.</summary>
    public readonly struct KwDeckRun
    {
        public KwDeckRun(Piece piece, float from, float to, float low, float high)
        {
            (Piece, From, To, Low, High) = (piece, from, to, low, high);
        }

        public Piece Piece { get; }
        public float From { get; }
        public float To { get; }
        public float Low { get; }
        public float High { get; }
        public float Middle => (Low + High) / 2f;
    }

    /// <summary>
    /// The decks and the hull along one keel line, read once at the Cabin tool's first click on a deck: a point on the line
    /// (the deck's origin) and its level way, the deck sections on it at every height, the planking's sections along it
    /// in order (how far along, the half section lifted to the line's keel, whether the hull ends there in a flat stern),
    /// and which way along it the bow is (1, -1, or 0 when the ship does not tell yet).
    /// </summary>
    public sealed class KwCabinSite
    {
        public KwCabinSite(Vector3 origin, Vector3 forward, List<KwDeckRun> decks, List<(float Along, Vector2[] Half, bool Flat)> sections, float bow)
        {
            (Origin, Forward, Decks, Sections, Bow) = (origin, forward, decks, sections, bow);
        }

        public Vector3 Origin { get; }
        public Vector3 Forward { get; }
        public List<KwDeckRun> Decks { get; }
        public List<(float Along, Vector2[] Half, bool Flat)> Sections { get; }
        public float Bow { get; }

        /// <summary>A deck part's section on the line; null for a piece not on it.</summary>
        public KwDeckRun? Find(Piece piece)
        {
            foreach (KwDeckRun run in Decks)
            {
                if (run.Piece == piece)
                    return run;
            }
            return null;
        }
    }

    /// <summary>
    /// What the Cabin tool's two clicks chose, before the window's sizes: where the cabin's frame stands (on the keel line
    /// at its back end) and the way it faces, the hull's sections in that frame, the deck's height under it and where
    /// drawn-in sides are measured, what its two ends are at and how far in the edges its sides stand on are (0 on a deck;
    /// on a roof, the cabin under it's sides). A cabin of any sizes over it is <see cref="Spec"/>.
    /// </summary>
    public sealed class KwCabinRange
    {
        public KwCabinRange(Vector3 position, Vector3 direction, List<Station> stations, float floor, float reference, int back, int front, float left, float right)
        {
            (Position, Direction, Stations, Floor, Ref) = (position, direction, stations, floor, reference);
            (BackHull, FrontHull, LeftBase, RightBase) = (back, front, left, right);
        }

        public Vector3 Position { get; }
        public Vector3 Direction { get; }
        public List<Station> Stations { get; }
        public float Floor { get; }
        public float Ref { get; }
        public int BackHull { get; }
        public int FrontHull { get; }
        public float LeftBase { get; }
        public float RightBase { get; }

        public float Length => Stations[Stations.Count - 1].X;

        /// <summary>The cabin of these sizes over the range, fitted to its hull (<see cref="KwCabinFit"/>).</summary>
        public KwCabinSpec Spec(KwCabinSizes sizes) => KwCabinFit.Of(new KwCabinSpec(Stations, Floor, Floor + sizes.Height, Ref, new KwCabinEnd(BackHull, sizes.Back),
            new KwCabinEnd(FrontHull, sizes.Front), new KwCabinSide(LeftBase, sizes.Left), new KwCabinSide(RightBase, sizes.Right), 0));
    }
}
