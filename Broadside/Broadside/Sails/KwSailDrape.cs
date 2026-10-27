using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// The cloth a sail's lines have hauled up, hanging as gathered sailcloth hangs rather than in even pleats. Under a
    /// square sail's set part the hauled cloth hangs in bags between the points its lines lift it by (one about every
    /// metre across, at most five, spread evenly over the yard, so a wide sail hangs in fewer, larger bags; the sail's
    /// sides hang down with its clews at their feet): each bag sags to its own depth, puffs
    /// out, and creases fan down from the lift points; a middle and a back sheet hang behind the front one, so a bag reads as bunched cloth. On a boom, the lowered cloth lies
    /// in flakes over it, bights swelling out to each side by turns, their size wandering along the boom. Each sail's
    /// irregularities come from its spec, so every player sees the same folds. Cloth coordinates as
    /// <see cref="KwSailCanvas"/>'s.
    /// </summary>
    public static class KwSailDrape
    {
        private const int Rows = 5, Flakes = 4, Along = 14, MostBags = 5;

        /// <summary>A square sail's gathered cloth at hoist f: a front, a middle and a back sheet.</summary>
        public static void Square(MeshBuilder b, KwSailLayout s, float f)
        {
            if (s.Bags(f) < 0.005f)
                return;
            float gathered = 1f - f;
            int columns = Columns(s);
            Rowed(b, columns, Rows, (a, t) => Bag(s, f, a, t, 1), (a, t) => new Vector2(a, gathered * (1f - 0.34f * t)));
            Rowed(b, columns, Rows, (a, t) => Bag(s, f, a, t, 0), (a, t) => new Vector2(a, gathered * (0.66f - 0.33f * t)));
            Rowed(b, columns, Rows, (a, t) => Bag(s, f, a, t, -1), (a, t) => new Vector2(a, gathered * 0.33f * (1f - t)));
        }

        /// <summary>The square sail's foot at hoist f: the bottoms of its front bags, clew to clew.</summary>
        public static List<Vector3> Foot(KwSailLayout s, float f)
        {
            List<Vector3> foot = new List<Vector3>();
            int columns = Columns(s);
            for (int c = 0; c <= columns; c++)
                foot.Add(Bag(s, f, (float)c / columns, 1f, 1));
            return foot;
        }

        /// <summary>The front sheet's side at a (0 or 1) from the set cloth's foot down to just above its clew (the foot's end); none fully set.</summary>
        public static List<Vector3> Side(KwSailLayout s, float f, float a)
        {
            List<Vector3> side = new List<Vector3>();
            for (int r = 0; r < Rows && s.Bags(f) >= 0.005f; r++)
                side.Add(Bag(s, f, a, (float)r / Rows, 1));
            return side;
        }

        // A point of a bag sheet: a across the sail (0 to 1), t from the set part's foot (0) down to the bag's bottom (1);
        // the front sheet (1) hangs deepest and puffs forward, the middle one (0) a little, the back one (-1) aft.
        private static Vector3 Bag(KwSailLayout s, float f, float a, float t, int sheet)
        {
            int bags = Bags(s);
            float across = a * bags, seed = Seed(s), edge = Mathf.Min(a, 1f - a) * bags;
            int i = Mathf.Min((int)across, bags - 1);
            float local = across - i, depth = 0.92f + 0.16f * Noise(seed, i), deep = s.Bags(f) * depth * (0.86f + 0.14f * sheet);
            float sag = Sag(edge, local, seed, i), close = Mathf.Clamp01(edge * 4f);
            float crease = 0.25f * t * Mathf.Sin(6f * Mathf.PI * local + 6.28f * Noise(seed, i + 23));
            float puff = (0.35f * deep + 0.04f) * sag * close * ((sheet == 1 ? 1f : sheet == 0 ? 0.3f : -0.6f) * Mathf.Sin(Mathf.PI * t) + crease);
            float shown = f * s.Height, side = 2f * a - 1f;
            float foot = s.BellyAt(shown) * 0.45f * (1f - 0.88f * side * side);
            return new Vector3(foot + puff, side * s.Width / 2f, -shown - deep * sag * t);
        }

        // How far down a bag hangs across it: nothing at a lift point between bags, all of it in a bag's middle; the sail's
        // own sides (its leeches) hang nearly as deep as the bags beside them, the cloth closed there.
        private static float Sag(float edge, float local, float seed, int i)
        {
            if (edge < 0.5f)
                return 0.85f + 0.15f * Mathf.Sin(Mathf.PI * edge);
            return Mathf.Pow(Mathf.Sin(Mathf.PI * local), 0.85f) * (1f + 0.08f * Mathf.Sin(4f * Mathf.PI * local + 6.28f * Noise(seed, i + 11)));
        }

        /// <summary>A sail along the ship's lowered cloth lying in flakes on its boom, at hoist f.</summary>
        public static void Boom(MeshBuilder b, KwSailLayout s, float f)
        {
            float gathered = s.Bundle(f), share = 1f - f;
            if (gathered < 0.005f)
                return;
            Rowed(b, Along, Flakes * 4, (u, t) => Flake(s, gathered, u, t * Flakes), (u, t) => KwSailCanvas.FlakeUv(s, u, share * (1f - t)));
        }

        // A point of the flaked cloth: u aft along the boom, k up through the flakes (whole numbers on the boom's middle,
        // the halves swelling out to one side or the other).
        private static Vector3 Flake(KwSailLayout s, float gathered, float u, float k)
        {
            float seed = Seed(s), turn = Mathf.Sin(Mathf.PI * k);
            float side = Mathf.Floor(k) % 2f == 0f ? 1f : -1f;
            float bight = (0.04f + 0.12f * gathered) * (0.5f + 0.5f * Mathf.Sin(2f * Mathf.PI * (2.5f * u + Noise(seed, (int)k))));
            float y = side * (0.05f + 0.05f * gathered + bight) * turn;
            float z = gathered * k / Flakes - 0.4f * bight * turn;
            return new Vector3(-0.05f - u * (s.Width - 0.05f), y, z);
        }

        // A grid of single faces, columns across and rows down, its corners from at and its texture from uv.
        private static void Rowed(MeshBuilder b, int columns, int rows, System.Func<float, float, Vector3> at, System.Func<float, float, Vector2> uv)
        {
            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < columns; c++)
                {
                    float a0 = (float)c / columns, a1 = (float)(c + 1) / columns, t0 = (float)r / rows, t1 = (float)(r + 1) / rows;
                    b.Face(new List<Vector3> { at(a0, t0), at(a1, t0), at(a1, t1), at(a0, t1) },
                        new List<Vector2> { uv(a0, t0), uv(a1, t0), uv(a1, t1), uv(a0, t1) });
                }
            }
        }

        /// <summary>How many bags a square sail's hauled cloth hangs in: about one a metre across, two to five.</summary>
        public static int Bags(KwSailLayout s) => Mathf.Clamp(Mathf.RoundToInt(s.Width * 0.94f / 1.1f), 2, MostBags);

        // The columns of faces across the bags: six a bag, more in a wide bag (one every 40 cm or so, nine at most), so a
        // large bag's sag stays round.
        private static int Columns(KwSailLayout s) => Bags(s) * Mathf.Clamp(Mathf.CeilToInt(s.Width / Bags(s) / 0.4f), 6, 9);

        private static float Seed(KwSailLayout s) => s.Width * 7.13f + s.Height * 3.71f + s.Spec.Shape * 1.3f + s.Mast * 0.77f;

        // A steady pseudo-random share (0 to 1) for a sail and an index: the same on every peer.
        private static float Noise(float seed, int i)
        {
            float v = Mathf.Sin(seed + i * 12.9898f) * 43758.5453f;
            return v - Mathf.Floor(v);
        }
    }
}
