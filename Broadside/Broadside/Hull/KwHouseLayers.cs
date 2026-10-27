using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// A cabin's mesh as it is drawn, in three looks: <see cref="Hull"/> (walls and raised sides, planked
    /// like the hull in its tier's hull look), <see cref="Deck"/> (decks and roofs, its tier's deck look) and
    /// <see cref="Wood"/> (its corner posts, the game's plank wood), and <see cref="Hard"/>, what it is walked on and
    /// bumped into (a whole slab for a deck laid as boards). Built in the part's kit axes (<see cref="MeshBuilder"/>), then
    /// made into Unity meshes: one per look (<see cref="Looks"/>: a launched ship's look and a blueprint's), one with a
    /// submesh per look for a part on the dock (<see cref="Layered"/>), all in one (a see-through preview, <see cref="Whole"/>).
    /// Made with <c>one</c>, every look draws into one builder.
    /// </summary>
    public sealed class KwHouseLayers
    {
        public KwHouseLayers(bool one = false)
        {
            Hull = new MeshBuilder(Vector3.zero);
            Deck = one ? Hull : new MeshBuilder(Vector3.zero);
            Wood = one ? Hull : new MeshBuilder(Vector3.zero);
            Hard = new MeshBuilder(Vector3.zero);
        }

        public MeshBuilder Hull { get; }
        public MeshBuilder Deck { get; }
        public MeshBuilder Wood { get; }
        public MeshBuilder Hard { get; }

        /// <summary>A shape drawn in one look, and into what is walked on too when it is solid.</summary>
        public void Draw(MeshBuilder look, System.Action<MeshBuilder> shape, bool solid = true)
        {
            shape(look);
            if (solid)
                shape(Hard);
        }

        /// <summary>Each look that has faces as its own mesh, with its material in a tier (Meadows' looks before a tier is painted).</summary>
        public List<(Material Material, Mesh Mesh)> Looks(KwBiomeTier tier)
        {
            List<(Material, Mesh)> looks = new List<(Material, Mesh)>();
            Look(looks, KwBiomeLooks.For(KwPart.Hull, tier) ?? KwPrefabs.Planks, Hull, "KW_house_hull");
            Look(looks, KwBiomeLooks.For(KwPart.Deck, tier) ?? KwPrefabs.Planks, Deck, "KW_house_deck");
            Look(looks, KwPrefabs.Planks, Wood, "KW_house_wood");
            return looks;
        }

        private static void Look(List<(Material, Mesh)> looks, Material? material, MeshBuilder builder, string name)
        {
            Mesh mesh = builder.ToMesh(name);
            if (material != null && mesh.vertexCount > 0)
                looks.Add((material, mesh));
            else
                Object.Destroy(mesh);
        }

        /// <summary>The looks as one mesh with a submesh each, and their materials in that order; the looks' own meshes let go.</summary>
        public (Mesh Mesh, Material[] Materials) Layered(KwBiomeTier tier, string name)
        {
            List<(Material Material, Mesh Mesh)> looks = Looks(tier);
            CombineInstance[] parts = looks.ConvertAll(l => new CombineInstance { mesh = l.Mesh, transform = Matrix4x4.identity }).ToArray();
            Mesh mesh = new Mesh { name = name, indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
            mesh.CombineMeshes(parts, false, false);
            mesh.RecalculateBounds();
            foreach ((Material _, Mesh part) in looks)
                Object.Destroy(part);
            return (mesh, looks.ConvertAll(l => l.Material).ToArray());
        }

        /// <summary>Every look in one mesh (made with <c>one</c>: the one builder's).</summary>
        public Mesh Whole(string name) => Hull.ToMesh(name);
    }
}
