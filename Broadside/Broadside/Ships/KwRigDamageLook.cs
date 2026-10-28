using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// A rig piece's own look on a launched ship (a mast and its spars, a bowsprit, a post cap), kept out of the ship's
    /// combined look (<see cref="KwShipFittings.Moves"/>) so it can be cut for its damage and fall: the piece's meshes
    /// gathered alone (<see cref="KwBpMeshSet"/>, the booms left to their sails, <see cref="KwBoomSwing.Apart"/>), one per
    /// material, in the ship's frame. Shown whole until damaged; then cut by its plan (<see cref="KwRigLookPlan"/>): the
    /// spars that go left out, drawn again notched or broken (<see cref="KwRigDamageSpar"/>), a cap's chips cratered
    /// (<see cref="KwRigDamageMesh.Chipped"/>), and for a fallen piece split into the stump (low) and what falls (high,
    /// under its own frame, which <see cref="KwMastFall"/> topples, then hidden). Only where the ship is drawn.
    /// </summary>
    public sealed class KwRigLook : MonoBehaviour
    {
        private readonly List<(Material Material, Mesh Whole, Vector3[] Kit)> wholes = new List<(Material, Mesh, Vector3[])>();
        private readonly List<(MeshFilter Low, MeshFilter High)> filters = new List<(MeshFilter, MeshFilter)>();
        private readonly List<Mesh> made = new List<Mesh>();
        private string shown = "";

        public KwRigPiece Piece { get; internal set; } = null!;

        /// <summary>What falls with the piece: its look's high part.</summary>
        public Transform High { get; private set; } = null!;

        /// <summary>The piece's working parts: a mast's sails and ropes, a bowsprit's jibs.</summary>
        public Transform? Fitting { get; set; }

        /// <summary>What of its fitting went with its fall, gathered apart (<see cref="KwMastFallSplit"/>); null while it stands.</summary>
        public Transform? Fallen { get; set; }

        /// <summary>A rig piece's look under the ship's visual holder; null when it draws nothing.</summary>
        public static KwRigLook? Make(Transform visual, KwRigPiece piece)
        {
            KwBpMeshSet set = new KwBpMeshSet();
            KwBoomSwing.Apart(() => set.Add(piece.Piece, Vector3.zero));
            List<(Material Material, Mesh Mesh)> meshes = set.Combine("KW_rig_" + piece.Index);
            GameObject root = new GameObject("rig" + piece.Index) { layer = visual.gameObject.layer };
            root.transform.SetParent(visual, false);
            KwRigLook look = root.AddComponent<KwRigLook>();
            (look.Piece, look.High) = (piece, KwRigDamageBoxes.Holder(root.transform, "high", Vector3.zero, Quaternion.identity));
            look.High.gameObject.layer = root.layer;
            foreach ((Material material, Mesh mesh) in meshes)
                look.Keep(material, mesh);
            foreach ((Mesh mesh, Material[] materials, Matrix4x4 at) in set.Loose)
                Loose(look.High, mesh, materials, at);
            return look;
        }

        // A material's whole mesh kept with its points in the piece's kit axes, and its low and high filters.
        private void Keep(Material material, Mesh mesh)
        {
            Vector3[] v = mesh.vertices;
            Vector3[] kit = new Vector3[v.Length];
            for (int i = 0; i < v.Length; i++)
                kit[i] = Piece.ToKit(v[i]);
            wholes.Add((material, mesh, kit));
            filters.Add((Filter(transform, material, mesh), Filter(High, material, null)));
        }

        private static MeshFilter Filter(Transform parent, Material material, Mesh? mesh)
        {
            GameObject part = new GameObject("look") { layer = parent.gameObject.layer };
            part.transform.SetParent(parent, false);
            MeshFilter filter = part.AddComponent<MeshFilter>();
            filter.sharedMesh = mesh;
            part.AddComponent<MeshRenderer>().sharedMaterial = material;
            return filter;
        }

        // A mesh Unity cannot read, drawn apart as the ship's look draws it: it falls whole with the piece.
        private static void Loose(Transform high, Mesh mesh, Material[] materials, Matrix4x4 at)
        {
            GameObject part = new GameObject("look_loose") { layer = high.gameObject.layer };
            part.transform.SetParent(high, false);
            part.transform.SetLocalPositionAndRotation(at.GetColumn(3), at.rotation);
            part.transform.localScale = at.lossyScale;
            part.AddComponent<MeshFilter>().sharedMesh = mesh;
            part.AddComponent<MeshRenderer>().sharedMaterials = materials;
        }

        /// <summary>The look as the rig's condition leaves it, cut again only when the piece's marks changed.</summary>
        public bool Show(KwRigCondition condition)
        {
            string key = KwRigLookKey.Of(condition, Piece);
            if (key == shown)
                return false;
            shown = key;
            Cut(KwRigLookPlan.Of(Piece, condition), condition.Chips(Piece.Index));
            return true;
        }

        // Each material's low and high meshes made again from its whole one by the plan; the meshes made before let go.
        private void Cut(KwRigLookPlan plan, List<KwRigMark> chips)
        {
            Forget();
            for (int k = 0; k < wholes.Count; k++)
            {
                (Material _, Mesh whole, Vector3[] kit) = wholes[k];
                bool plain = plan.Whole && chips.Count == 0;
                filters[k].Low.sharedMesh = plain ? whole : Own(Part(plan, chips, k, false));
                filters[k].High.sharedMesh = plan.Fall.HasValue ? Own(Part(plan, chips, k, true)) : null;
            }
        }

        // One material's low or high part: its whole mesh's kept triangles, with the spars drawn again (the first material's), chipped.
        private Mesh Part(KwRigLookPlan plan, List<KwRigMark> chips, int k, bool high)
        {
            (Material _, Mesh whole, Vector3[] kit) = wholes[k];
            Mesh kept = KwRigDamageMesh.Kept(whole, kit, (a, b, c) => plan.Keeps(a, b, c) && plan.High(a, b, c) == high, "KW_rig_cut");
            List<(Mesh Mesh, Matrix4x4 At)> parts = new List<(Mesh Mesh, Matrix4x4 At)> { (kept, Matrix4x4.identity) };
            foreach (KwRigSparDraw spar in k == 0 ? plan.Spars : new List<KwRigSparDraw>())
            {
                if (spar.High == high)
                    parts.Add((KwRigDamageSpar.Notched(spar.Spar, spar.Notches, spar.From, spar.To, spar.RaggedFrom, spar.RaggedTo, spar.Seed), Piece.Matrix));
            }
            Mesh joined = KwRigDamageMesh.Joined(parts, "KW_rig_damaged");
            KwRigDamageMesh.Chipped(joined, Piece.Matrix, chips);
            return joined;
        }

        private Mesh Own(Mesh mesh)
        {
            made.Add(mesh);
            return mesh;
        }

        private void Forget()
        {
            foreach (Mesh mesh in made)
            {
                if (mesh != null)
                    Destroy(mesh);
            }
            made.Clear();
        }

        private void OnDestroy()
        {
            Forget();
            foreach ((Material _, Mesh whole, Vector3[] _) in wholes)
            {
                if (whole != null)
                    Destroy(whole);
            }
        }
    }

    /// <summary>What a rig piece's looks are drawn from, as text, to know when to draw them again (<see cref="KwRigCondition.Key"/>).</summary>
    public static class KwRigLookKey
    {
        /// <summary>A piece's own look: its notches, chips and fall.</summary>
        public static string Of(KwRigCondition condition, KwRigPiece piece) => condition.Key(piece.Index, "ncx");

        /// <summary>A mast's sails: its notches (fastenings, booms, gaffs) and holes.</summary>
        public static string Sails(KwRigCondition condition, KwRigPiece piece) =>
            condition.Key(piece.Index, new string(new[] { KwRigMark.Notch, KwRigMark.Hole, KwRigMark.Fall }));
    }
}
