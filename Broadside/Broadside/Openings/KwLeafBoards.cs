using System;
using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// A cover's wood (a gunport lid, a drop lid, a shutter, a round port's lid; <see cref="KwOpeningLeafMesh"/>) as boards,
    /// so it reads as boards in the plain plank wood: a backing as the leaf's outline (a rectangle, or the round lid's disc)
    /// and on its outer face boards running along the hinge, about a hand wide, a shallow groove between each two; the iron
    /// straps cross them from the hinge. Each board's grain runs along it at the plank wood's density, starting at a place
    /// of its own (<see cref="KwPlankGrain"/>). In the leaf's hinge axes (x along the hinge, y across from it, z out),
    /// every point handed to where it goes. Pure.
    /// </summary>
    public static class KwLeafBoards
    {
        /// <summary>A board's width aimed at, the groove between two (its width and depth).</summary>
        public const float Board = 0.15f, Groove = 0.006f, Deep = 0.005f;

        /// <summary>A cover's boards and their backing, the outline its own in (x, y), its thickness t.</summary>
        public static void Draw(MeshBuilder b, List<Vector2> outline, float across, float t, Func<Vector3, Vector3> place)
        {
            int count = Mathf.Clamp(Mathf.RoundToInt(across / Board), 2, 10);
            Slab(b, outline, 0f, t - Deep, true, KwPlankGrain.Shift(count, KwPlankGrain.Leaf), place);
            for (int k = 0; k < count; k++)
            {
                float y0 = across * k / count + (k > 0 ? Groove / 2f : -1f), y1 = across * (k + 1) / count - (k + 1 < count ? Groove / 2f : -1f);
                List<Vector2> strip = KwClip.Between(outline, p => p.y, Vector2.LerpUnclamped, y0, y1);
                if (strip.Count >= 3)
                    Slab(b, strip, t - Deep, t, false, KwPlankGrain.Shift(k, KwPlankGrain.Leaf), place);
            }
        }

        /// <summary>A leaf's outline in (x, y): a rectangle along its hinge, or a round lid's disc hanging from it.</summary>
        public static List<Vector2> Outline(KwLeafPlan leaf)
        {
            Vector3 size = leaf.Size;
            if (!leaf.Round)
                return new List<Vector2> { new Vector2(-size.x / 2f, 0f), new Vector2(size.x / 2f, 0f), new Vector2(size.x / 2f, size.y), new Vector2(-size.x / 2f, size.y) };
            List<Vector2> disc = new List<Vector2>();
            float r = size.y / 2f;
            for (int i = 0; i < KwOpeningShape.RoundSides; i++)
            {
                float a = i * 2f * Mathf.PI / KwOpeningShape.RoundSides;
                disc.Add(new Vector2(Mathf.Cos(a) * r, r + Mathf.Sin(a) * r));
            }
            return disc;
        }

        // An outline pushed from z0 to z1: its top, its back when asked (a board's lies on the backing, unseen), its sides.
        private static void Slab(MeshBuilder b, List<Vector2> outline, float z0, float z1, bool back, Vector2 shift, Func<Vector3, Vector3> place)
        {
            Face(b, outline, z1, shift, place);
            if (back)
                Face(b, outline, z0, shift, place);
            float along = 0f;
            for (int i = 0; i < outline.Count; i++)
            {
                Vector2 p = outline[i], q = outline[(i + 1) % outline.Count];
                float next = along + Vector2.Distance(p, q);
                List<Vector3> side = new List<Vector3> { new Vector3(p.x, p.y, z0), new Vector3(q.x, q.y, z0), new Vector3(q.x, q.y, z1), new Vector3(p.x, p.y, z1) };
                List<Vector2> uv = new List<Vector2> { KwPlankGrain.At(along, z0, shift), KwPlankGrain.At(next, z0, shift), KwPlankGrain.At(next, z1, shift), KwPlankGrain.At(along, z1, shift) };
                b.Polygon(side.ConvertAll(c => place(c)), uv);
                along = next;
            }
        }

        // A flat face of an outline at a height, the grain along the hinge (x).
        private static void Face(MeshBuilder b, List<Vector2> outline, float z, Vector2 shift, Func<Vector3, Vector3> place) =>
            b.Polygon(outline.ConvertAll(p => place(new Vector3(p.x, p.y, z))), outline.ConvertAll(p => KwPlankGrain.At(p.x, p.y, shift)));
    }
}
