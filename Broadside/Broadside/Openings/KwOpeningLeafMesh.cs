using System;
using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// A leaf's look (<see cref="KwLeafPlan"/>): a cover's boards running along its hinge in the plain plank wood
    /// (<see cref="KwLeafBoards"/>; a round port's lid a disc of them), and iron straps across them from the hinge, two on a
    /// lid or a shutter, one on a round lid; a door's leaf a door of its part's tier's style (<see cref="KwDoorMesh"/>, in the
    /// game's door wood, <see cref="KwDoorLooks"/>). Drawn in the leaf's own hinge axes, each point handed to where it goes
    /// (<c>place</c>: into the leaf's own object, or into a part's or a ship's mesh), into a wood and an iron builder.
    /// </summary>
    public static class KwOpeningLeafMesh
    {
        private const float Strap = 0.05f, StrapThick = 0.012f, Sink = 0.001f;

        /// <summary>A point of a leaf's own (Unity) axes as a kit point that a builder turns back into it.</summary>
        public static Vector3 Own(Vector3 local) => new Vector3(local.z, -local.x, local.y);

        /// <summary>The leaf's wood and its straps, each point placed by place; a door in its tier's style (Meadows' for none).</summary>
        public static void Draw(MeshBuilder wood, MeshBuilder iron, KwLeafPlan leaf, Func<Vector3, Vector3> place, KwBiomeTier? tier = null)
        {
            if (leaf.Door)
            {
                KwDoorMesh.Draw(wood, iron, leaf, KwDoorLooks.Style(tier), place);
                return;
            }
            Vector3 size = leaf.Size;
            KwLeafBoards.Draw(wood, KwLeafBoards.Outline(leaf), size.y, size.z, place);
            foreach (float x in Straps(leaf))
                Slab(iron, new Vector3(x - Strap / 2f, 0.02f, size.z - Sink), new Vector3(x + Strap / 2f, size.y * 0.85f, size.z + StrapThick), place);
        }

        // Where the straps run across the leaf, along its hinge.
        private static float[] Straps(KwLeafPlan leaf) =>
            leaf.Round ? new[] { 0f } : new[] { -leaf.Size.x * 0.3f, leaf.Size.x * 0.3f };

        // An iron strap: a box in the leaf's axes, its faces mapped along it from the hinge.
        private static void Slab(MeshBuilder b, Vector3 min, Vector3 max, Func<Vector3, Vector3> place)
        {
            Vector3[] corners =
            {
                new Vector3(min.x, min.y, min.z), new Vector3(max.x, min.y, min.z), new Vector3(max.x, max.y, min.z), new Vector3(min.x, max.y, min.z),
                new Vector3(min.x, min.y, max.z), new Vector3(max.x, min.y, max.z), new Vector3(max.x, max.y, max.z), new Vector3(min.x, max.y, max.z),
            };
            Solid.Hexa(b, Array.ConvertAll(corners, c => place(c)), (face, corner, _) => Uv(corners[corner], face));
        }

        // A face's texture point: across the strap along the hinge, along it from the hinge (a side face its own two axes).
        private static Vector2 Uv(Vector3 p, int face) => face < 2
            ? new Vector2(KwUv.Along(p.x), KwUv.Along(p.y))
            : new Vector2(KwUv.Along(p.x + p.y), KwUv.Along(p.z));
    }
}
