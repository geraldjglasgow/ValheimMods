using System;
using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// A door leaf's look (<see cref="KwLeafPlan"/>, a single door's or one of a double door's two), so that it reads as a
    /// door at a glance: upright boards side by side, about a hand wide each, every long edge bevelled on both faces so each
    /// seam shows as a groove and each board starting the wood's grain at its own place; on the inner face three ledges
    /// across (foot, middle, top) with a brace rising from each lower ledge at the hinge side to the one above at the free
    /// side; and its iron by its tier (<see cref="KwDoorIron"/>: strap hinges, straps or bands, studs, a ring pull on each
    /// face). The boards are <see cref="KwLeafPlan.Size"/>'s thickness through, the ledges stand proud of the inner face.
    /// Drawn in the door space (<see cref="KwDoorSpace"/>), the wood into one builder and the iron into another. Pure.
    /// </summary>
    public static class KwDoorMesh
    {
        /// <summary>A board's width aimed at, its bevel, the ledges' width and how far they stand off the inner face.</summary>
        public const float Board = 0.24f, Bevel = 0.009f, Ledge = 0.13f, LedgeThick = 0.026f;

        /// <summary>How far the ledges stop short of the leaf's edges, a brace's width, the foot and top ledges' middles in from the ends.</summary>
        private const float Inset = 0.03f, BraceWide = 0.1f, End = 0.2f;

        /// <summary>A door leaf's wood and its iron, its points placed by place.</summary>
        public static void Draw(MeshBuilder wood, MeshBuilder iron, KwLeafPlan leaf, KwDoorStyle style, Func<Vector3, Vector3> place)
        {
            KwDoorSpace door = new KwDoorSpace(leaf, place);
            Boards(wood, door);
            Ledges(wood, door);
            KwDoorIron.Draw(iron, door, style);
        }

        /// <summary>How many boards make a leaf this wide (at least two).</summary>
        public static int Count(float wide) => Mathf.Clamp(Mathf.RoundToInt(wide / Board), 2, 12);

        /// <summary>The ledges' middles up the leaf from its foot (the hinge straps outside at the foot's and the top's).</summary>
        public static float[] Heights(float high) => new[] { Mathf.Min(End, high / 6f), high / 2f, high - Mathf.Min(End, high / 6f) };

        /// <summary>The boards' middles across the leaf from its hinge.</summary>
        public static List<float> Middles(float wide)
        {
            int count = Count(wide);
            List<float> middles = new List<float>();
            for (int i = 0; i < count; i++)
                middles.Add((i + 0.5f) * wide / count);
            return middles;
        }

        // The boards side by side across the leaf, each its full height.
        private static void Boards(MeshBuilder b, KwDoorSpace door)
        {
            int count = Count(door.Wide);
            float wide = door.Wide / count;
            for (int i = 0; i < count; i++)
                Plank(b, door, i * wide, (i + 1) * wide, new Vector2(0.37f * i, 0.29f * ((i * 5) % 7)));
        }

        // One board from a0 to a1 across, its long edges bevelled on both faces: an eight-sided section pushed up the leaf.
        private static void Plank(MeshBuilder b, KwDoorSpace door, float a0, float a1, Vector2 shift)
        {
            float t = door.Half, c = Mathf.Min(Bevel, t / 3f, (a1 - a0) / 4f);
            List<Vector3> foot = new List<Vector3>
            {
                new Vector3(a0 + c, 0f, -t), new Vector3(a1 - c, 0f, -t), new Vector3(a1, 0f, c - t), new Vector3(a1, 0f, t - c),
                new Vector3(a1 - c, 0f, t), new Vector3(a0 + c, 0f, t), new Vector3(a0, 0f, t - c), new Vector3(a0, 0f, c - t),
            };
            door.Prism(b, foot, new Vector3(0f, door.High, 0f), Vector3.up, shift);
        }

        // Three ledges across the inner face and a brace between each two.
        private static void Ledges(MeshBuilder b, KwDoorSpace door)
        {
            float[] at = Heights(door.High);
            float a0 = Inset, a1 = door.Wide - Inset, d0 = -door.Half, d1 = d0 - LedgeThick, half = Mathf.Min(Ledge, door.High / 8f) / 2f;
            for (int i = 0; i < at.Length; i++)
                door.Box(b, new Vector3(a0, at[i] - half, d1), new Vector3(a1, at[i] + half, d0), Vector3.right, new Vector2(0.21f * i, 0.53f + 0.17f * i));
            for (int i = 0; i + 1 < at.Length; i++)
                Brace(b, door, a0, a1, at[i] + half, at[i + 1] - half, d0, d1);
        }

        // A brace from the hinge side on the lower ledge up to the free side on the upper one, its ends cut level to lie on
        // them; none where the ledges are too close or the leaf too narrow for one.
        private static void Brace(MeshBuilder b, KwDoorSpace door, float a0, float a1, float h0, float h1, float d0, float d1)
        {
            float rise = h1 - h0, run = a1 - a0;
            if (rise < 0.15f || run < 0.2f)
                return;
            float cut = BraceWide * Mathf.Sqrt(run * run + rise * rise) / rise;
            if (cut > run / 2f)
                return;
            List<Vector3> ring = new List<Vector3> { new Vector3(a0, h0, d0), new Vector3(a0 + cut, h0, d0), new Vector3(a1, h1, d0), new Vector3(a1 - cut, h1, d0) };
            door.Prism(b, ring, new Vector3(0f, 0f, d1 - d0), new Vector3(run - cut, rise, 0f), new Vector2(0.61f, 0.11f));
        }
    }
}
