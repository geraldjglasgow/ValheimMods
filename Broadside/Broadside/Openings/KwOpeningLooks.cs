using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// What a part's openings add to its look, from its tool and saved text alone (the same on every screen, for a part on
    /// the dock, a ship's look and a saved ship's ghost): the frames (<see cref="KwOpeningFrames"/>) as a mesh in the plank
    /// wood (<see cref="Wood"/>) and one of iron bands in the game's iron, and, for a ghost, the leaves shut over their
    /// openings (<see cref="KwOpeningLeafMesh"/>; doors in the game's door wood, <see cref="Dressed"/>). Meshes in the part's
    /// kit axes about its origin, as its own mesh.
    /// </summary>
    public static class KwOpeningLooks
    {
        /// <summary>
        /// The wood every frame, lining board, lid and shutter wears, whatever the tier: the railings' plank wood
        /// (<see cref="KwRailLooks.Look"/>, the game's <c>woodwall</c> at its own texture scale), its grain laid along each
        /// board (<see cref="KwPlankGrain"/>). The tier's deck look drew the deck's boards and patches over every frame (the
        /// user's words, 2026-10-25: "so they don't look like the deck texture was reused"); a tier is a door's look and an
        /// opening's cost only. Null before the kit is made.
        /// </summary>
        public static Material? Wood(KwBiomeTier tier) => KwRailLooks.Look;

        /// <summary>The iron the bands and straps wear: the game's iron wall's.</summary>
        public static Material? Iron => KwTierMaterials.Of(KwLook.Iron);

        /// <summary>
        /// A part's frames as meshes (wood, iron), with its leaves shut over them when asked; empty when it has no openings.
        /// Planking seated where seat says (<see cref="KwOpeningSurfaces.Of"/>).
        /// </summary>
        public static List<(bool Iron, Mesh Mesh)> Of(string tool, string saved, bool shut, KwSeatAt? seat = null)
        {
            List<(bool Iron, Mesh Mesh)> made = new List<(bool Iron, Mesh Mesh)>();
            List<KwOpeningWall> walls = KwOpeningSurfaces.Of(tool, saved, seat);
            if (walls.Count == 0)
                return made;
            MeshBuilder wood = new MeshBuilder(Vector3.zero), iron = new MeshBuilder(Vector3.zero);
            KwOpeningFrames.Draw(wood, iron, walls);
            if (shut)
            {
                foreach (KwLeafPlan leaf in KwOpeningLeaves.Of(walls))
                    KwOpeningLeafMesh.Draw(wood, iron, leaf, Placed(leaf));
            }
            Add(made, false, wood.ToMesh("KW_opening_frames"));
            Add(made, true, iron.ToMesh("KW_opening_iron"));
            return made;
        }

        /// <summary>
        /// A part's frames and, when asked, its leaves shut, each mesh with what it wears: the frames and covers the plank
        /// wood, the doors the game's door wood in the tier's style (<see cref="KwDoorLooks"/>), the iron the game's
        /// iron (a material null where the game has none). Empty when it has no openings. Planking seated where seat says.
        /// </summary>
        public static List<(Material? Material, Mesh Mesh)> Dressed(string tool, string saved, bool shut, KwBiomeTier tier, KwSeatAt? seat = null)
        {
            List<(Material? Material, Mesh Mesh)> made = new List<(Material? Material, Mesh Mesh)>();
            List<KwOpeningWall> walls = KwOpeningSurfaces.Of(tool, saved, seat);
            if (walls.Count == 0)
                return made;
            MeshBuilder wood = new MeshBuilder(Vector3.zero), door = new MeshBuilder(Vector3.zero), iron = new MeshBuilder(Vector3.zero);
            KwOpeningFrames.Draw(wood, iron, walls);
            foreach (KwLeafPlan leaf in shut ? KwOpeningLeaves.Of(walls) : new List<KwLeafPlan>())
                KwOpeningLeafMesh.Draw(leaf.Door ? door : wood, iron, leaf, Placed(leaf), tier);
            Dress(made, wood.ToMesh("KW_opening_frames"), () => Wood(tier));
            Dress(made, door.ToMesh("KW_opening_doors"), () => KwDoorLooks.Wood(tier));
            Dress(made, iron.ToMesh("KW_opening_iron"), () => Iron);
            return made;
        }

        // A mesh with what it wears, kept when it has anything in it.
        private static void Dress(List<(Material? Material, Mesh Mesh)> made, Mesh mesh, System.Func<Material?> look)
        {
            if (mesh.vertexCount > 0)
                made.Add((look(), mesh));
            else
                Object.Destroy(mesh);
        }

        /// <summary>A leaf's own point shut in place, as a kit point of its part.</summary>
        public static System.Func<Vector3, Vector3> Placed(KwLeafPlan leaf)
        {
            Vector3 hinge = KwMesh.ToUnity(leaf.Hinge);
            return local => KwMesh.ToKit(hinge + leaf.Turn * local);
        }

        /// <summary>
        /// A leaf's own point swung a share of its way open (stage -1 to 1, 0 shut) about its pivot, eased, as a kit point of
        /// its part: where <see cref="KwLeafSwing.Pose"/> puts it on the dock and on a ship.
        /// </summary>
        public static System.Func<Vector3, Vector3> Placed(KwLeafPlan leaf, float stage)
        {
            Vector3 hinge = KwMesh.ToUnity(leaf.Hinge), pivot = leaf.Pivot(stage);
            Quaternion swing = KwLeafSwing.Swing(leaf, stage);
            return local => KwMesh.ToKit(hinge + leaf.Turn * (pivot + swing * (local - pivot)));
        }

        private static void Add(List<(bool Iron, Mesh Mesh)> made, bool iron, Mesh mesh)
        {
            if (mesh.vertexCount > 0)
                made.Add((iron, mesh));
            else
                Object.Destroy(mesh);
        }
    }
}
