using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;

namespace Broadside
{
    /// <summary>
    /// A launched ship's hull looks (<see cref="KwHoleLooks"/>) packed into chunks: the looks of one material, taken in
    /// order along the ship, joined into one mesh until it would pass <see cref="Most"/> vertices, so a ship's hull is a
    /// few draw calls and a hit cuts a mesh of at most that size again, not the whole ship's.
    /// </summary>
    public static class KwHoleChunks
    {
        /// <summary>The most vertices a chunk is packed to (one look alone may be more).</summary>
        public const int Most = 32000;

        /// <summary>
        /// Looks packed into chunks, each the indices of its looks: by look (any key), along the ship (the place given), a
        /// chunk closed when the next look would take it past Most vertices. Pure.
        /// </summary>
        public static List<List<int>> Pack<T>(List<(T Look, int Count, float Along)> looks)
        {
            List<List<int>> chunks = new List<List<int>>();
            foreach (IGrouping<T, int> same in Enumerable.Range(0, looks.Count).GroupBy(i => looks[i].Look))
            {
                List<int> chunk = new List<int>();
                int count = 0;
                foreach (int i in same.OrderBy(i => looks[i].Along))
                {
                    if (chunk.Count > 0 && count + looks[i].Count > Most)
                    {
                        chunks.Add(chunk);
                        (chunk, count) = (new List<int>(), 0);
                    }
                    chunk.Add(i);
                    count += looks[i].Count;
                }
                chunks.Add(chunk);
            }
            return chunks;
        }

        /// <summary>The looks combined chunk by chunk, each in the ship's frame with its material; the looks given are let go.</summary>
        public static List<(Material Material, Mesh Mesh)> Combine(List<(KwBpPiece Piece, Material Material, Mesh Mesh, Matrix4x4 At)> made)
        {
            List<(Material, Mesh)> chunks = new List<(Material, Mesh)>();
            foreach (List<int> chunk in Pack(made.ConvertAll(m => (m.Material, m.Mesh.vertexCount, m.Piece.Position.z))))
            {
                int count = chunk.Sum(i => made[i].Mesh.vertexCount);
                Mesh mesh = new Mesh { name = "KW_ship_hull", indexFormat = count > 65000 ? IndexFormat.UInt32 : IndexFormat.UInt16 };
                mesh.CombineMeshes(chunk.Select(i => new CombineInstance { mesh = made[i].Mesh, transform = made[i].At }).ToArray(), true, true);
                mesh.RecalculateBounds();
                chunks.Add((made[chunk[0]].Material, mesh));
            }
            foreach ((KwBpPiece _, Material _, Mesh look, Matrix4x4 _) in made)
                Object.Destroy(look);
            return chunks;
        }
    }
}
