using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// The framed edge of every opening on a part's walls: lining boards round the cut through the wall's thickness, a frame
    /// round it on the outside face and one on the inside face, as wide and as proud as its kind (<see cref="Width"/>: plain
    /// timber, heavy timber, iron-banded); an iron-banded frame also has an iron band round the middle of its outside frame.
    /// The frames stand clear of the wall on both faces all along (<see cref="Front"/>, <see cref="Back"/>: the user's words,
    /// 2026-10-25, "No wall glitching in or through the frame"): past the furthest the wall's face reaches (a planking's laps)
    /// by the frame's own proud, at least <see cref="Clear"/>, every point on the wall's own face in steps of
    /// <see cref="KwFrameRing.Step"/> (<see cref="KwFrameRing"/>), so a frame bends with a curving hull instead of cutting
    /// into it on a chord; their rims run back into the wall, and the lining meets both faces. A doorway's frame stands on
    /// the floor: nothing along its foot. Where an opening runs past its wall's top or foot (a doorway cut down through a
    /// hull's sheer, a cabin's raised side over the planking) its frame stops there. The wood wears the plank wood with its
    /// grain along each board (<see cref="KwFramePen"/>, <see cref="KwPlankGrain"/>), the band the game's iron
    /// (<see cref="KwOpeningLooks"/>). On planking and raised sides it all goes along the opening's tunnel, square to the
    /// hull (<see cref="KwOpeningTunnel"/>). Kit axes of the part, as its own mesh.
    /// </summary>
    public static class KwOpeningFrames
    {
        private const float Band = 0.4f, BandProud = 0.008f;

        /// <summary>
        /// How far a frame's face stands past the wall's furthest face at least; how much more, for the little the wall's
        /// mesh strays from its surface between its corners (measured offline on curved hulls); how deep a rim runs back into
        /// the wall, so no seam shows at its foot.
        /// </summary>
        public const float Clear = 0.012f, Slack = 0.004f, Sunk = 0.015f;

        /// <summary>A frame's width on the face and how far it stands out of it, by kind.</summary>
        public static (float Wide, float Proud) Width(string frame) => frame switch
        {
            KwOpening.Heavy => (0.12f, 0.04f),
            KwOpening.Iron => (0.09f, 0.025f),
            _ => (0.07f, 0.02f),
        };

        /// <summary>The outside frame's face, out of the wall's outer face line: past its furthest face by the frame's proud.</summary>
        public static float Front(KwOpeningSurface s, KwOpening o) => s.Proud + Mathf.Max(Width(o.Frame).Proud, Clear) + Slack;

        /// <summary>The inside frame's face (less than 0: in through the wall): past its inner face by half the frame's proud.</summary>
        public static float Back(KwOpeningSurface s, KwOpening o) => -s.Thick - Mathf.Max(Width(o.Frame).Proud / 2f, Clear) - Slack;

        /// <summary>An opening's frame rings: its cut and its frame's outer edge on its wall.</summary>
        public static KwFrameRing Rings(KwOpening o) => KwFrameRing.Of(Grown(o, 0f), Grown(o, Width(o.Frame).Wide), o.IsDoorway, o.Sill);

        /// <summary>Every opening's frame on a part's walls, the wood into one builder and the iron into another.</summary>
        public static void Draw(MeshBuilder wood, MeshBuilder iron, List<KwOpeningWall> walls)
        {
            foreach (KwOpeningWall wall in walls)
            {
                foreach (KwOpening o in wall.Openings)
                    One(wood, iron, wall.Surface, o);
            }
        }

        // One opening's lining, outside and inside frames, and band, along its tunnel where it has one (KwOpeningTunnel).
        private static void One(MeshBuilder wood, MeshBuilder iron, KwOpeningSurface wall, KwOpening o)
        {
            KwOpeningSurface s = o.Surface ?? wall;
            float front = Front(s, o), back = Back(s, o), wide = Width(o.Frame).Wide;
            KwFrameRing ring = Rings(o);
            (float lo, float hi) = Clip(s, o, ring.Outer);
            KwFramePen pen = new KwFramePen(wood, s, lo, hi);
            pen.Lining(ring, front, back);
            pen.Face(ring, front, -Sunk, KwPlankGrain.Outside);
            pen.Face(ring, back, -s.Thick + Sunk, KwPlankGrain.Inside);
            if (o.Frame != KwOpening.Iron)
                return;
            KwFrameRing band = KwFrameRing.Of(Grown(o, wide * (0.5f - Band / 2f)), Grown(o, wide * (0.5f + Band / 2f)), o.IsDoorway, o.Sill);
            new KwFramePen(iron, s, lo, hi).Face(band, front + BandProud, front - 0.002f, KwPlankGrain.Outside, 1f / KwUv.Tile);
        }

        /// <summary>The outline grown by a margin (on its wall when fitted to one), a doorway's foot kept on its sill.</summary>
        public static List<Vector2> Grown(KwOpening o, float grow)
        {
            List<Vector2> ring = o.Outline(grow);
            return o.IsDoorway ? ring.ConvertAll(p => new Vector2(p.x, Mathf.Max(p.y, o.Sill))) : ring;
        }

        // The heights the frame stays between: the wall's foot and top over the frame's reach along, at their tightest.
        private static (float Lo, float Hi) Clip(KwOpeningSurface s, KwOpening o, List<Vector2> outer)
        {
            float lo = float.MinValue, hi = float.MaxValue, u0 = float.MaxValue, u1 = float.MinValue;
            foreach (Vector2 p in outer)
                (u0, u1) = (Mathf.Min(u0, p.x), Mathf.Max(u1, p.x));
            foreach (float u in new[] { u0, o.U, u1 })
            {
                (float a, float c) = s.Range(u);
                (lo, hi) = (Mathf.Max(lo, a), Mathf.Min(hi, c));
            }
            return (lo, hi);
        }
    }
}
