using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace Broadside
{
    /// <summary>
    /// A blueprint's look gathered piece by piece and combined into one mesh per material: each piece prefab's own drawn
    /// meshes (its shown renderers, every submesh with its material; a post cap in its saved keel tier, a carved head in the heads' look), or for a generated
    /// part the mesh its saved span builds (<see cref="KwSpan"/>; a deck in its tier's boards), in its saved biome tier's look (<see cref="KwBiomeLooks"/>;
    /// a railing, tiered or not, a ladder or a mast in the plank wood, <see cref="KwRailLooks"/>), planking shifted by its place in the ship as built parts are. A mesh Unity cannot read stays apart
    /// (<see cref="Loose"/>). Placed in the entry's frame: the blueprint's frame moved so the anchor is at the origin. Planking
    /// lays its hold's floor too (<see cref="KwSole"/>), in its tier's deck boards, giving way to the blueprint's low decks (<see cref="Decks"/>),
    /// and its painted bands over its strakes (<see cref="KwBandMesh"/>, in <see cref="KwPaintLooks"/>). A part's openings are
    /// cut from its mesh and framed (<see cref="KwOpeningLooks"/>), their covers and doors drawn shut only when asked (<see cref="Shut"/>).
    /// </summary>
    public sealed class KwBpMeshSet
    {
        private static readonly int SpanHash = KwGenerated.Key.GetStableHashCode();
        private readonly Dictionary<Material, List<CombineInstance>> groups = new Dictionary<Material, List<CombineInstance>>();
        private readonly List<Mesh> temporary = new List<Mesh>();

        public List<(Mesh Mesh, Material[] Materials, Matrix4x4 At)> Loose { get; } = new List<(Mesh Mesh, Material[] Materials, Matrix4x4 At)>();

        public void Add(KwBpPiece piece, Vector3 anchor)
        {
            piece = KwCapEdit.AsBuilt(piece);
            Matrix4x4 at = Matrix4x4.TRS(piece.Position - anchor, piece.Rotation, Vector3.one);
            if (ToolOf(piece.Prefab) != null)
                Generated(piece, at);
            else
                Prefab(piece.Prefab, at, Carved(piece));
        }

        // A post cap's look in its keel's tier (a carved head's own, else the keel's: KwTierCap.Look, its own material group);
        // null for any other piece, which draws as its prefab does.
        private static Material? Carved(KwBpPiece piece) =>
            KwCaps.Is(piece.Prefab) ? KwTierCap.Look(piece.Prefab, KwBpData.Text(piece.Data, SpanHash), KwBiomeTiers.Of(piece.Data)) : null;

        // The tool a generated hull part comes from (planking, deck, rail); null for any other piece.
        private static string? ToolOf(string prefab)
        {
            if (!prefab.StartsWith(KwToolPieces.PartPrefix, StringComparison.Ordinal))
                return null;
            string tool = prefab.Substring(KwToolPieces.PartPrefix.Length);
            return Array.IndexOf(KwToolPieces.Tools, tool) >= 0 ? tool : null;
        }

        /// <summary>The combined meshes, the largest first, each with its material; the gathered parts are let go.</summary>
        public List<(Material Material, Mesh Mesh)> Combine(string name)
        {
            List<(Material Material, Mesh Mesh)> made = new List<(Material Material, Mesh Mesh)>();
            foreach (KeyValuePair<Material, List<CombineInstance>> group in groups.OrderByDescending(g => g.Value.Sum(c => c.mesh.vertexCount)))
            {
                Mesh mesh = new Mesh { name = name, indexFormat = IndexFormat.UInt32 };
                mesh.CombineMeshes(group.Value.ToArray(), true, true);
                mesh.RecalculateBounds();
                made.Add((group.Key, mesh));
            }
            foreach (Mesh part in temporary)
                Object.Destroy(part);
            temporary.Clear();
            groups.Clear();
            return made;
        }

        // A cabin's looks, each its own mesh in its own material (KwHouses), placed as the piece is.
        private bool Layered(KwBpPiece piece, List<(Material Material, Mesh Mesh)> looks, Matrix4x4 at)
        {
            foreach ((Material material, Mesh mesh) in looks)
                Own(piece, material, mesh, at);
            return true;
        }

        /// <summary>
        /// Which pieces keep their own look apart in <see cref="Parts"/> (a launched ship's hull parts, which holes are cut
        /// into, <see cref="KwHoleLooks"/>): their strakes, painted bands, deck boards or cabin looks; null, all combined.
        /// </summary>
        public Func<KwBpPiece, bool>? Apart { get; set; }

        /// <summary>The looks kept apart (<see cref="Apart"/>), each with its piece, material and place; the caller's to keep and let go.</summary>
        public List<(KwBpPiece Piece, Material Material, Mesh Mesh, Matrix4x4 At)> Parts { get; } = new List<(KwBpPiece, Material, Mesh, Matrix4x4)>();

        // A part's own look: kept apart when asked, else combined with the rest.
        private void Own(KwBpPiece piece, Material material, Mesh mesh, Matrix4x4 at)
        {
            if (Apart != null && Apart(piece))
            {
                Parts.Add((piece, material, mesh, at));
                return;
            }
            temporary.Add(mesh);
            Group(material, mesh, 0, at);
        }

        /// <summary>A deck's hatch openings, when the pieces gathered are a whole ship's (a launched ship's look); none otherwise.</summary>
        public Func<KwBpPiece, KwDef, List<Rect>>? DeckHoles { get; set; }

        /// <summary>
        /// The keels the pieces stand on, in their frame, which the hull parts and ribs are seated on as on the dock
        /// (<see cref="KwSeat"/>); null, each built from its text alone.
        /// </summary>
        public KwKeelLines? Keels { get; set; }

        // Where a piece stands among the keels, when they are known.
        private KwSeatAt? Seat(KwBpPiece piece) => Keels != null ? new KwSeatAt(Keels, piece.Position, piece.Rotation * Vector3.forward) : (KwSeatAt?)null;

        /// <summary>The blueprint's decks, which the holds' floors give way to where they lie low (<see cref="KwSole.Decks"/>); none, a floor all along.</summary>
        public List<(Vector3 At, Vector3 Forward, KwDef Def)> Decks { get; set; } = new List<(Vector3 At, Vector3 Forward, KwDef Def)>();

        /// <summary>Whether the openings' covers and doors are drawn shut in the look (a saved ship's ghost); a launched ship hangs its own.</summary>
        public bool Shut { get; set; }

        private void Generated(KwBpPiece piece, Matrix4x4 at)
        {
            string tool = ToolOf(piece.Prefab)!, saved = KwBpData.Text(piece.Data, SpanHash) ?? "";
            Openings(piece, tool, saved, at);
            if (KwHouses.Owns(tool) && Layered(piece, KwPartMeshes.Looks(tool, saved, KwBiomeTiers.Of(piece.Data)), at))
                return;
            Material? material = Carved(piece) ?? KwBiomeLooks.Of(tool, piece.Data) ?? KwPrefabs.Planks;
            Mesh? mesh = KwPartMeshes.MeshFor(tool, saved, out KwDef? def, DeckHoles == null ? null : d => DeckHoles(piece, d), KwBiomeTiers.Of(piece.Data), Seat(piece));
            if (material == null || mesh == null)
                return;
            if (tool == "planking")
                KwPlankShift.Apply(mesh, piece.Position, piece.Rotation * Vector3.forward);
            Own(piece, material, mesh, at);
            if (tool == "planking" && def != null)
            {
                Sole(piece, saved, def.Stations, at);
                Bands(piece, saved, def, at);
            }
        }

        // A part's openings' frames (KwOpeningLooks) in the plank wood and the iron, its covers and doors too when shut
        // (doors in the game's door wood).
        private void Openings(KwBpPiece piece, string tool, string saved, Matrix4x4 at)
        {
            foreach ((Material? look, Mesh mesh) in KwPartMeshes.Dressed(tool, saved, Shut, KwBiomeTiers.Of(piece.Data), Seat(piece)))
            {
                temporary.Add(mesh);
                if (look != null)
                    Group(look, mesh, 0, at);
            }
        }

        // A planking part's painted bands (KwBandMesh), each colour in its paint over the part's tier, its planks shifted
        // as the planking's are.
        private void Bands(KwBpPiece piece, string saved, KwDef def, Matrix4x4 at)
        {
            KwPaint.Strip(saved, out string paint);
            KwBiomeTier tier = KwBiomeTiers.Of(piece.Data);
            foreach ((int colour, Mesh mesh) in KwBandMesh.Meshes(def, paint))
            {
                KwPlankShift.Apply(mesh, piece.Position, piece.Rotation * Vector3.forward);
                if (KwPaintLooks.For(tier, colour) is Material look)
                    Own(piece, look, mesh, at);
                else
                    temporary.Add(mesh);
            }
        }

        // A planking part's hold floor, in its tier's deck boards and deck look, left out under the blueprint's low decks.
        private void Sole(KwBpPiece piece, string saved, List<Station> stations, Matrix4x4 at)
        {
            KwBiomeTier tier = KwBiomeTiers.Of(piece.Data);
            Vector3 forward = piece.Rotation * Vector3.forward;
            Material? look = KwBiomeLooks.For(KwPart.Deck, tier) ?? KwPrefabs.Planks;
            if (look == null || !KwSpan.TryParse(saved, out KwSpan span)
                || KwSole.Def(Seat(piece)?.Seated(span) ?? span, stations, KwSole.Seed(piece.Position, forward)) is not KwDef floor)
                return;
            Mesh mesh = KwMesh.Build(KwSole.Dress(floor, KwSole.Gaps(piece.Position, forward, floor, Decks), tier));
            temporary.Add(mesh);
            if (mesh.vertexCount > 0)
                Group(look, mesh, 0, at);
        }

        private void Prefab(string name, Matrix4x4 at, Material? carved)
        {
            GameObject? prefab = ZNetScene.instance != null ? ZNetScene.instance.GetPrefab(name) : null;
            if (prefab == null)
                return;
            Transform root = prefab.transform;
            foreach (MeshFilter filter in prefab.GetComponentsInChildren<MeshFilter>(true))
            {
                MeshRenderer? renderer = filter.GetComponent<MeshRenderer>();
                Mesh? mesh = filter.sharedMesh;
                if (renderer == null || !renderer.enabled || mesh == null || mesh.vertexCount == 0 || !Shown(filter.transform, root))
                    continue;
                Renderer(mesh, Swap(renderer.sharedMaterials, carved), at * root.worldToLocalMatrix * filter.transform.localToWorldMatrix);
            }
        }

        // A post cap's Meadows look (the keel's, or a carved head's) swapped for its own tier's.
        private static Material[] Swap(Material[] materials, Material? carved) =>
            carved is Material look ? materials.Select(m => KwTierCap.Undressed(m) ? look : m).ToArray() : materials;

        private void Renderer(Mesh mesh, Material[] materials, Matrix4x4 at)
        {
            if (materials.Length == 0)
                return;
            if (!mesh.isReadable)
            {
                Loose.Add((mesh, materials, at));
                return;
            }
            for (int i = 0; i < mesh.subMeshCount; i++)
            {
                Material material = materials[Mathf.Min(i, materials.Length - 1)];
                if (material != null)
                    Group(material, mesh, i, at);
            }
        }

        private void Group(Material material, Mesh mesh, int submesh, Matrix4x4 at)
        {
            if (!groups.TryGetValue(material, out List<CombineInstance> list))
                groups[material] = list = new List<CombineInstance>();
            list.Add(new CombineInstance { mesh = mesh, subMeshIndex = submesh, transform = at });
        }

        // Drawn in the prefab as it stands: the object and every parent up to the prefab's root switched on.
        private static bool Shown(Transform part, Transform root)
        {
            for (Transform? t = part; t != null && t != root; t = t.parent)
            {
                if (!t.gameObject.activeSelf)
                    return false;
            }
            return true;
        }
    }
}
