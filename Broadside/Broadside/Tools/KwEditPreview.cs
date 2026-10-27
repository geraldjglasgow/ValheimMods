using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// An edit's preview in the parts' real looks (<see cref="KwEditSession"/>), while the parts themselves are hidden
    /// (<see cref="KwEditHide"/>), so the player sees only what OK would build: every planned part made from its new text
    /// and tier as a blueprint's ghost makes a ship (<see cref="KwBpMeshSet"/>) at the part's own place: its tier's looks, a
    /// railing's or a ladder's plank wood, a planking part's painted bands, hold floor and openings' frames and covers, a
    /// cabin's three looks, a deck's hatch openings as they stand; a piece moved with them (a cabin carried on an edited
    /// cabin's roof, what stands on it) where it would stand. Solid and shadowed, one object a material, no colliders.
    /// Made again only when what it shows changes. Local to this player, never networked.
    /// </summary>
    public static class KwEditPreview
    {
        private const float DeckReach = 60f;
        private static readonly int SpanHash = KwGenerated.Key.GetStableHashCode();
        private static readonly string DeckPrefab = KwToolPieces.PartPrefix + "deck";
        private static readonly List<Mesh> meshes = new List<Mesh>();
        private static GameObject? shown;
        private static string key = "";

        /// <summary>The planned parts shown in their looks (made again only when next, what they are, changes).</summary>
        public static void Show(string next, List<KwEditPlan> plans, string tool)
        {
            if (next == key && shown != null)
                return;
            Clear();
            Transform? anchor = plans.Count > 0 && plans[0].Target.Piece != null ? plans[0].Target.Piece.transform : null;
            if (anchor == null)
                return;
            key = next;
            shown = new GameObject("KwEditPreview");
            shown.transform.position = anchor.position;
            foreach ((Material material, Mesh mesh) in Looks(plans, anchor.position, tool))
                Part(material, mesh);
        }

        /// <summary>The preview gone, its meshes let go.</summary>
        public static void Clear()
        {
            key = "";
            if (shown != null)
                Object.Destroy(shown);
            shown = null;
            foreach (Mesh mesh in meshes)
            {
                if (mesh != null)
                    Object.Destroy(mesh);
            }
            meshes.Clear();
        }

        // Every planned part gathered at its own place, as the world's axes from the anchor, combined one mesh a material.
        private static List<(Material Material, Mesh Mesh)> Looks(List<KwEditPlan> plans, Vector3 anchor, string tool)
        {
            Dictionary<KwBpPiece, Transform> standing = new Dictionary<KwBpPiece, Transform>();
            KwBpMeshSet set = new KwBpMeshSet { Shut = true, Keels = KwKeelLines.World() };
            set.DeckHoles = (piece, def) => standing.TryGetValue(piece, out Transform at) && at != null ? KwDeckHoles.For(at, def) : new List<Rect>();
            if (tool == "planking")
                set.Decks = Decks(anchor);
            foreach (KwEditPlan plan in plans)
            {
                Piece? part = plan.Target.Piece;
                if (part == null)
                    continue;
                KwBpPiece piece = new KwBpPiece(Utils.GetPrefabName(part.gameObject), part.transform.position + plan.Shift, part.transform.rotation, Data(plan));
                standing[piece] = part.transform;
                set.Add(piece, anchor);
            }
            return set.Combine("KW_edit_preview");
        }

        // A plan's saved values as a blueprint keeps them: its new text and its tier (the part's own for a kind with none).
        private static List<KwBpValue> Data(KwEditPlan plan) => new List<KwBpValue>
        {
            new KwBpValue(SpanHash, plan.Text),
            new KwBpValue(KwBiomeTiers.Hash, (plan.Tier ?? plan.Target.Tier).Id),
        };

        // The decks loaded round the parts, which their holds' floors give way to where they lie low (as on the dock, KwSoleDock).
        private static List<(Vector3 At, Vector3 Forward, KwDef Def)> Decks(Vector3 near)
        {
            List<(Vector3, Vector3, KwDef)> decks = new List<(Vector3, Vector3, KwDef)>();
            List<Piece> found = new List<Piece>();
            Piece.GetAllPiecesInRadius(near, DeckReach, found);
            foreach (Piece piece in found)
            {
                ZDO? zdo = piece != null && Utils.GetPrefabName(piece.gameObject) == DeckPrefab ? piece.GetComponent<ZNetView>()?.GetZDO() : null;
                if (zdo != null && KwSpan.TryParse(zdo.GetString(KwGenerated.Key), out KwSpan span))
                    decks.Add((piece!.transform.position, piece.transform.forward, span.Def("deck")));
            }
            return decks;
        }

        // One material's mesh drawn under the preview, solid, casting and taking shadows as the part does.
        private static void Part(Material material, Mesh mesh)
        {
            meshes.Add(mesh);
            GameObject look = new GameObject(material.name);
            look.transform.SetParent(shown!.transform, false);
            look.AddComponent<MeshFilter>().sharedMesh = mesh;
            look.AddComponent<MeshRenderer>().sharedMaterial = material;
        }
    }
}
