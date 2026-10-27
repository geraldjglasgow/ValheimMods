using System;
using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// The raw hide sail's wear (the Meadows sail, the crudest a player makes): its even hide (<see cref="KwSailHide"/>)
    /// holed, patched, stained and torn, painted over its pattern for each sail's shape and proportions
    /// (<see cref="KwSailLooks"/>), so the wear lies on each cloth as the cloth lies in its texture
    /// (<see cref="KwSailFrame"/>): a handful of holes of different sizes cut out (alpha under the sail material's cutout),
    /// each with a darker frayed rim; two rough patches of undyed hide sewn on with crude stitches, a tear sewn shut coming
    /// out from under one; stains and spots worn thin; and the free edges torn ragged (a square sail's sides and foot,
    /// every other sail's leech), while the edges laced to a yard, gaff, mast, boom, club or stay and every corner a rope
    /// holds stay whole. Everything is placed by fixed numbers and a fixed seed, never at random, so every player sees the
    /// same sail; plain arithmetic on pixels. Linen and scale hide stay whole.
    /// </summary>
    public static class KwSailWear
    {
        /// <summary>The worn cloth's type: raw hide (<see cref="KwSailCloth"/>).</summary>
        public const string Cloth = "rawhide";

        /// <summary>Whether a cloth type is worn: raw hide only.</summary>
        public static bool Worn(string cloth) => cloth == Cloth;

        /// <summary>
        /// The painted cloth's pixels (size by size, bottom row first) worn as a cloth of this frame lies in them; own is the
        /// cloth's texture before its pattern, which the patches are cut from.
        /// </summary>
        public static Color32[] Wear(Color32[] painted, Color32[] own, int size, KwSailFrame frame)
        {
            KwWearPlan plan = KwWearPlan.Of(frame);
            Color32[] made = new Color32[painted.Length];
            for (int j = 0; j < size; j++)
            {
                for (int i = 0; i < size; i++)
                {
                    Vector2 on = frame.On((i + 0.5f) / size, (j + 0.5f) / size);
                    made[j * size + i] = plan.Texel(painted[j * size + i], new KwWearTexel(own, size, i, j, on));
                }
            }
            return made;
        }
    }

    /// <summary>
    /// One texel being worn (<see cref="KwSailWear"/>): the cloth's own pixels before its pattern (a patch's hide comes
    /// from them), the texel's place in them, and its place on the cloth (<see cref="KwSailFrame.On"/>).
    /// </summary>
    internal readonly struct KwWearTexel
    {
        public KwWearTexel(Color32[] own, int size, int i, int j, Vector2 on)
        {
            (Own, Size, I, J, On) = (own, size, i, j, on);
        }

        public Color32[] Own { get; }
        public int Size { get; }
        public int I { get; }
        public int J { get; }
        public Vector2 On { get; }

        /// <summary>The cloth's own texel that far from this one, round the texture.</summary>
        public Color32 OwnAt(int dx, int dy) => Own[(J + dy) % Size * Size + (I + dx) % Size];
    }

    /// <summary>
    /// Where one cloth's wear goes (<see cref="KwSailWear"/>): each hole, patch and stain at fixed shares of the way along
    /// the cloth's foot and up it, placed between its corners as its texture lies (a triangle's as a jib's,
    /// <see cref="KwJibUv"/>), so every shape has its handful; sized to the cloth (smaller on a narrow one) and kept clear of
    /// its edges, or left out where there is no room. Its free edges torn. Places in heights of the sail.
    /// </summary>
    internal sealed class KwWearPlan
    {
        // Holes: shares along the foot and up, radius, how much longer than wide, and which way (degrees).
        private static readonly (float S, float T, float R, float Long, float Turn)[] HoleAt =
        {
            (0.34f, 0.46f, 0.060f, 1.35f, 25f), (0.71f, 0.70f, 0.040f, 1.15f, -35f), (0.60f, 0.21f, 0.030f, 1.5f, 70f),
            (0.20f, 0.78f, 0.024f, 1.2f, 5f), (0.87f, 0.40f, 0.021f, 1.6f, 100f), (0.50f, 0.63f, 0.016f, 1.1f, 40f),
        };

        // Patches: shares, shares to try where there is no room there (a jib's narrow corners), half width and height, turn
        // (degrees), and their hide's shade, a lighter and a darker piece.
        private static readonly (float S, float T, float OrS, float OrT, float W, float H, float Turn, Vector3 Tone)[] PatchAt =
        {
            (0.16f, 0.24f, 0.42f, 0.28f, 0.075f, 0.056f, 7f, new Vector3(1.3f, 1.2f, 1.02f)),
            (0.82f, 0.86f, 0.82f, 0.86f, 0.050f, 0.042f, -11f, new Vector3(0.8f, 0.74f, 0.68f)),
        };

        // Stains and thin spots: shares, radius, strength and kind.
        private static readonly (float S, float T, float R, float Strength, int Kind)[] StainAt =
        {
            (0.45f, 0.20f, 0.13f, 0.20f, KwWearStain.Dark), (0.62f, 0.86f, 0.10f, 0.18f, KwWearStain.Dark),
            (0.13f, 0.58f, 0.08f, 0.15f, KwWearStain.Ring), (0.66f, 0.48f, 0.15f, 0.13f, KwWearStain.Thin),
            (0.30f, 0.82f, 0.09f, 0.14f, KwWearStain.Thin), (0.90f, 0.14f, 0.08f, 0.16f, KwWearStain.Dark),
        };

        // The sewn tear: from under the first patch, this way (degrees) and this long.
        private const float SlitTurn = -20f, SlitLength = 0.24f;

        private readonly List<KwWearStain> stains = new List<KwWearStain>();
        private readonly List<KwWearPatch> patches = new List<KwWearPatch>();
        private readonly List<KwWearHole> holes = new List<KwWearHole>();
        private readonly List<KwWearEdge> edges = new List<KwWearEdge>();
        private KwWearSlit? slit;

        /// <summary>A cloth's wear: its frame's outline (<see cref="KwSailFrame.Corners"/>) and proportions.</summary>
        public static KwWearPlan Of(KwSailFrame frame)
        {
            Vector2[] corners = frame.Corners;
            Vector2[] outline = Array.ConvertAll(corners, c => frame.On(c.x, c.y));
            float k = Mathf.Clamp(frame.Least, 0.55f, 1f);
            Func<float, float, Vector2> at = (s, t) => Place(frame, corners, s, t);
            KwWearPlan plan = new KwWearPlan();
            plan.Stain(at, k);
            plan.Sew(at, outline, k);
            plan.Hole(at, outline, k);
            plan.Tear(outline, frame.Shape == 0, k);
            return plan;
        }

        /// <summary>A texel worn: stained, a tear sewn shut, patched, then cut out where a hole or a torn edge takes it.</summary>
        public Color32 Texel(Color32 c, KwWearTexel t)
        {
            foreach (KwWearStain stain in stains)
                c = stain.Over(c, t.On);
            if (slit != null)
                c = slit.Over(c, t.On);
            foreach (KwWearPatch patch in patches)
                c = patch.Over(c, t);
            float shade = 1f;
            bool cut = false;
            foreach (KwWearHole hole in holes)
                cut |= hole.Cut(t, ref shade);
            foreach (KwWearEdge edge in edges)
                cut |= edge.Cut(t, ref shade);
            return KwWearNoise.Shade(c, new Vector3(shade, shade, shade), cut ? (byte)0 : c.a);
        }

        // A place on the cloth at shares s along its foot and t up it: between its four corners, or in its triangle as a
        // jib's texture is laid (KwJibUv).
        private static Vector2 Place(KwSailFrame frame, Vector2[] c, float s, float t)
        {
            Vector2 x = c.Length == 3 ? c[0] + s * (1f - t) * (c[1] - c[0]) + t * (c[2] - c[0])
                : Vector2.Lerp(Vector2.Lerp(c[0], c[1], s), Vector2.Lerp(c[3], c[2], s), t);
            return frame.On(x.x, x.y);
        }

        private void Stain(Func<float, float, Vector2> at, float k)
        {
            for (int n = 0; n < StainAt.Length; n++)
            {
                var s = StainAt[n];
                stains.Add(new KwWearStain(at(s.S, s.T), s.R * k, s.Strength, s.Kind, 701 + 13 * n));
            }
        }

        // The patches where they have room (shrunk a little to fit, or left out), and the sewn tear from under the first.
        private void Sew(Func<float, float, Vector2> at, Vector2[] outline, float k)
        {
            for (int n = 0; n < PatchAt.Length; n++)
            {
                var p = PatchAt[n];
                Vector2 half = new Vector2(p.W, p.H) * k, centre = at(p.S, p.T);
                if (Fit(outline, centre, half) <= 0.6f)
                    centre = at(p.OrS, p.OrT);
                float fit = Fit(outline, centre, half);
                if (fit > 0.6f)
                    patches.Add(KwWearPatch.Of(centre, half * fit, p.Turn, p.Tone, 501 + 29 * n));
            }
            if (patches.Count == 0)
                return;
            Vector2 way = new Vector2(Mathf.Cos(SlitTurn * Mathf.Deg2Rad), Mathf.Sin(SlitTurn * Mathf.Deg2Rad));
            slit = KwWearSlit.Of(patches[0].Centre, way, SlitLength * k, outline, 601);
        }

        // How much of its size a patch keeps to stay clear of the cloth's edges there (1 all of it).
        private static float Fit(Vector2[] outline, Vector2 centre, Vector2 half) =>
            Mathf.Min(1f, (KwSailFrame.Inside(outline, centre) - 0.02f) / (1.2f * half.magnitude));

        // The holes where they have room: shrunk to stay clear of every edge (the laced ones stay whole) and the patches, or
        // left out.
        private void Hole(Func<float, float, Vector2> at, Vector2[] outline, float k)
        {
            for (int n = 0; n < HoleAt.Length; n++)
            {
                var h = HoleAt[n];
                Vector2 centre = at(h.S, h.T);
                float r = Mathf.Min(h.R * k, Room(outline, centre) / (1.4f * h.Long + 0.5f));
                if (r >= 0.008f)
                    holes.Add(new KwWearHole(centre, r, h.Long, h.Turn, 101 + 17 * n));
            }
        }

        // The room a hole has at a place: how far it is from the cloth's edges and from the patches, less a margin.
        private float Room(Vector2[] outline, Vector2 centre)
        {
            float room = KwSailFrame.Inside(outline, centre) - 0.015f;
            foreach (KwWearPatch patch in patches)
                room = Mathf.Min(room, (centre - patch.Centre).magnitude - patch.Reach);
            return room;
        }

        // The free edges torn: a square sail's sides and foot (all but the head on its yard), any other sail's leech (its
        // second edge: the foot is on a boom or club, the luff on the mast or stay, a gaff sail's head on its gaff).
        private void Tear(Vector2[] outline, bool square, float k)
        {
            for (int e = 0; e < outline.Length; e++)
            {
                if (square ? e != 2 : e == 1)
                    edges.Add(new KwWearEdge(outline[e], outline[(e + 1) % outline.Length], k, 301 + 23 * e));
            }
        }
    }
}
