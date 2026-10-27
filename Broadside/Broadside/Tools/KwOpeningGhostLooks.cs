using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// The looks of one part's stand-in in the Doorway and Window tools' ghost (<see cref="KwOpeningGhost"/>), each a child of
    /// the stand-in owning its mesh: the body as the part builds it from a saved text (planking with its planks laid where it
    /// stands and its painted bands, in the materials its own model wears; a cabin in its tier's looks), and the frames and
    /// leaves of the openings in a text in the plank wood, doors in the game's door wood, and the game's iron: the leaves
    /// shut as built, or for an edit window's ghost swung <see cref="Open"/> of their way open as the game swings them, so
    /// a lid's or shutters' or a door's style shows.
    /// </summary>
    public static class KwOpeningGhostLooks
    {
        /// <summary>The share of its swing an edit window's ghost hangs each leaf open at.</summary>
        public const float Open = 0.6f, Placing = 1f;

        /// <summary>The part's body from its new saved text, under the stand-in.</summary>
        public static void Body(Transform root, Piece piece, KwOpeningSite site, string text)
        {
            Transform? real = piece.transform.Find("model");
            if (site.Planking)
                Planking(root, piece, real, site, text);
            else
                Cabin(root, real, site, text);
        }

        // Planking as it builds where it stands, in its own model's materials, with its painted bands.
        private static void Planking(Transform root, Piece piece, Transform? real, KwOpeningSite site, string text)
        {
            Mesh? mesh = KwGenerated.MeshFor("planking", text, piece.transform, out KwDef? def, null, site.Tier);
            if (mesh == null)
                return;
            MeshRenderer? own = real != null ? real.GetComponent<MeshRenderer>() : null;
            Child(root, "model", mesh, own != null ? own.sharedMaterials : new[] { KwPrefabs.Hull }, real);
            KwBandsShown.Dress(root, text, def, site.Tier);
        }

        // A cabin in its tier's looks, one submesh a look.
        private static void Cabin(Transform root, Transform? real, KwOpeningSite site, string text)
        {
            KwHouseLayers? layers = KwHouses.Layers(site.Tool, text, site.Tier);
            if (layers == null)
                return;
            (Mesh look, Material[] materials) = layers.Layered(site.Tier, "KW_opening_ghost");
            Child(root, "model", look, materials, real);
        }

        /// <summary>
        /// The frames and the leaves of the openings a saved text holds, under the stand-in, each mesh in what it wears (the
        /// plank wood, doors the game's door wood, the iron): shut, or a share of their way open.
        /// </summary>
        public static void Frames(Transform root, KwOpeningSite site, string text, float open = 0f)
        {
            KwSeatAt? seat = site.Piece != null ? KwSeatAt.World(site.Piece.transform) : (KwSeatAt?)null;
            foreach ((Material? material, Mesh mesh) in open > 0f ? Ajar(site.Tool, text, site.Tier, open, seat) : KwOpeningLooks.Dressed(site.Tool, text, true, site.Tier, seat))
                Child(root, mesh.name, mesh, new[] { material }, null);
        }

        /// <summary>
        /// The leaves alone of the openings a saved text holds (a launched ship's ghost, whose part's frames are in its body,
        /// <see cref="KwSeaOpeningGhost"/>), under a holder placed as the part is: a share of their way open (0: shut).
        /// </summary>
        public static void Leaves(Transform root, KwOpeningSite site, string text, float open, KwSeatAt? seat)
        {
            foreach ((Material? material, Mesh mesh) in Ajar(site.Tool, text, site.Tier, open, seat, false))
                Child(root, mesh.name, mesh, new[] { material }, null);
        }

        // A part's frames (unless left out) with its leaves that share of their way open, as meshes with what each wears;
        // empty ones left out.
        private static List<(Material? Material, Mesh Mesh)> Ajar(string tool, string text, KwBiomeTier tier, float open, KwSeatAt? seat, bool frames = true)
        {
            MeshBuilder wood = new MeshBuilder(Vector3.zero), door = new MeshBuilder(Vector3.zero), iron = new MeshBuilder(Vector3.zero);
            Draw(wood, door, iron, tool, text, open, tier, seat, frames);
            List<(Material? Material, Mesh Mesh)> made = new List<(Material? Material, Mesh Mesh)>();
            foreach ((MeshBuilder b, string name, Material? look) in new[] { (wood, "frames", KwOpeningLooks.Wood(tier)), (door, "doors", KwDoorLooks.Wood(tier)), (iron, "iron", KwOpeningLooks.Iron) })
            {
                Mesh mesh = b.ToMesh(name);
                if (mesh.vertexCount > 0)
                    made.Add((look, mesh));
                else
                    UnityEngine.Object.Destroy(mesh);
            }
            return made;
        }

        /// <summary>
        /// A part's frames and leaves drawn from its saved text (doors into their own builder), each leaf swung a share of
        /// its way open about its pivot as the game swings it (<see cref="KwOpeningLooks.Placed(KwLeafPlan, float)"/>; 0: shut);
        /// planking seated where seat says (<see cref="KwOpeningSurfaces.Of"/>). Pure.
        /// </summary>
        public static void Draw(MeshBuilder wood, MeshBuilder door, MeshBuilder iron, string tool, string text, float open, KwBiomeTier? tier = null, KwSeatAt? seat = null, bool frames = true)
        {
            List<KwOpeningWall> walls = KwOpeningSurfaces.Of(tool, text, seat);
            if (frames)
                KwOpeningFrames.Draw(wood, iron, walls);
            foreach (KwLeafPlan leaf in KwOpeningLeaves.Of(walls))
                KwOpeningLeafMesh.Draw(leaf.Door ? door : wood, iron, leaf, KwOpeningLooks.Placed(leaf, open), tier);
        }

        // A child drawing a mesh it owns, placed as the part's own child is (none: at the stand-in's origin).
        private static void Child(Transform root, string name, Mesh mesh, Material?[] materials, Transform? like)
        {
            GameObject look = new GameObject(name) { layer = root.gameObject.layer };
            look.transform.SetParent(root, false);
            if (like != null)
                look.transform.SetLocalPositionAndRotation(like.localPosition, like.localRotation);
            look.AddComponent<MeshFilter>().sharedMesh = mesh;
            look.AddComponent<MeshRenderer>().sharedMaterials = materials;
            look.AddComponent<KwMeshOwner>();
        }
    }
}
