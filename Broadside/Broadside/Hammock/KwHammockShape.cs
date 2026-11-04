using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// The hammock's sling as meshes, from its maths (<see cref="KwHammockSling"/>): the cloth as one smooth sheet seen from
    /// both sides and the spreader bars, in the sling's own frame (they swing with it), and the same in the hammock's frame
    /// for the placing preview (<see cref="KwHammockView"/>). The cloth's sizes: <see cref="Cloth"/> long (shorter between
    /// close ends, <see cref="ClothFor"/>), <see cref="Width"/> wide, sagging <see cref="Sag"/>, cradling <see cref="Curl"/>.
    /// The workshop's archive (AssetLab's <c>KwHammockShape</c>, a hammock between two hooks on walls) gave the sling.
    /// </summary>
    public static class KwHammockShape
    {
        /// <summary>How the hammock is drawn, the stamp its kept icon is found by: raise it with any change to its look.</summary>
        public const string Stamp = "hammock-2";

        /// <summary>The cloth: longest, shortest, the ropes' run along the span both ends together, width, sag, cradle; a bar's half length.</summary>
        public const float Cloth = 1.9f, Shortest = 1.5f, Ends = 0.3f, Width = 0.8f, Sag = 0.15f, Curl = 0.09f, Bar = Width * 0.5f + 0.05f;

        private const int Along = 16, Across = 6;
        private static int[]? triangles;

        /// <summary>The cloth's length for a span between the rope points.</summary>
        public static float ClothFor(float span) => Mathf.Clamp(span - Ends, Shortest, Cloth);

        /// <summary>A Unity point in kit axes (<see cref="KwMesh.ToUnity"/> undone).</summary>
        public static Vector3 Kit(Vector3 unity) => new Vector3(unity.z, -unity.x, unity.y);

        /// <summary>The cloth in the sling's own frame, its UVs the whole cloth once.</summary>
        public static Mesh ClothMesh(KwHammockSling sling, string name)
        {
            List<Vector3> vertices = new List<Vector3>();
            List<Vector2> uvs = new List<Vector2>();
            for (int side = 0; side < 2; side++)
            {
                for (int i = 0; i <= Along; i++)
                {
                    for (int j = 0; j <= Across; j++)
                    {
                        float u = (float)i / Along, v = (float)j / Across;
                        vertices.Add(KwMesh.ToUnity(sling.In(sling.ClothPoint(sling.Length * (u - 0.5f), Width * (v - 0.5f)))));
                        uvs.Add(new Vector2(u, v));
                    }
                }
            }
            return Finish(new Mesh { name = name }, vertices, uvs);
        }

        /// <summary>The two spreader bars in the sling's own frame.</summary>
        public static Mesh BarsMesh(KwHammockSling sling, string name)
        {
            MeshBuilder b = new MeshBuilder(Vector3.zero);
            foreach ((Vector3 from, Vector3 to) in sling.Bars())
                Solid.Log(b, sling.In(from), sling.In(to), 0.026f, 6);
            return b.ToMesh(name);
        }

        /// <summary>The bars, the ropes (thin logs) and the cloth (a grid of faces) in the hammock's frame: the preview's sling.</summary>
        public static void Preview(MeshBuilder b, KwHammockSling sling)
        {
            foreach ((Vector3 from, Vector3 to) in sling.Bars())
                Solid.Log(b, from, to, 0.026f, 6);
            foreach ((Vector3 from, Vector3 to) in sling.Ropes())
                Solid.Log(b, from, to, 0.012f, 4);
            for (int i = 0; i < Along; i++)
            {
                for (int j = 0; j < Across; j++)
                {
                    List<Vector3> cell = new List<Vector3> { Grid(sling, i, j), Grid(sling, i + 1, j), Grid(sling, i + 1, j + 1), Grid(sling, i, j + 1) };
                    b.Polygon(cell, Solid.Planar(cell));
                }
            }
        }

        private static Vector3 Grid(KwHammockSling sling, int i, int j) =>
            sling.ClothPoint(sling.Length * ((float)i / Along - 0.5f), Width * ((float)j / Across - 0.5f));

        private static Mesh Finish(Mesh mesh, List<Vector3> vertices, List<Vector2> uvs)
        {
            mesh.SetVertices(vertices);
            mesh.SetUVs(0, uvs);
            mesh.SetColors(vertices.ConvertAll(_ => Color.white));
            mesh.SetTriangles(triangles ??= Triangles(), 0);
            mesh.RecalculateNormals();
            mesh.RecalculateTangents();
            mesh.RecalculateBounds();
            return mesh;
        }

        // Two triangles a cell on the first side, the same cells wound the other way on the second.
        private static int[] Triangles()
        {
            List<int> list = new List<int>();
            int row = Across + 1, back = (Along + 1) * row;
            for (int i = 0; i < Along; i++)
            {
                for (int j = 0; j < Across; j++)
                {
                    int a = i * row + j, b = a + 1, c = a + row, d = c + 1;
                    list.AddRange(new[] { a, c, b, b, c, d });
                    list.AddRange(new[] { back + a, back + b, back + c, back + b, back + d, back + c });
                }
            }
            return list.ToArray();
        }
    }
}
