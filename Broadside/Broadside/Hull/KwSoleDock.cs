using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// A planking part's hold floor in the world (<see cref="KwSole"/>): a "sole" child under its model wearing the floor's
    /// boards in the deck look of the part's own tier, walked on as one slab (its mesh collider), built with the part
    /// (<see cref="KwGenerated"/>) from its span and its stations as they stand, giving way to the decks loaded on this peer
    /// that lie low over it; a low deck that comes or goes builds the parts round it again (<see cref="KwGenerated.Refloor"/>).
    /// Under the model, it shows and hides with it, and the hammer hovers and removes it as the planking. Nothing is sent:
    /// every peer lays the same floor from the same pieces.
    /// </summary>
    public static class KwSoleDock
    {
        /// <summary>How far from a deck's start the planking it may lie over can start.</summary>
        public const float Reach = 30f;

        private const string Child = "sole";
        private static readonly string DeckPrefab = KwToolPieces.PartPrefix + "deck";
        private static readonly List<Piece> Found = new List<Piece>();

        /// <summary>Lays a planking part's floor again (or takes it up, where its hull has room for none or decks cover it all).</summary>
        public static void Lay(Transform part, KwSpan span, List<Station> stations, KwBiomeTier tier)
        {
            Transform? model = part.Find("model");
            if (model == null)
                return;
            KwDef? floor = KwSole.Def(span, stations, KwSole.Seed(part.position, part.forward));
            if (floor != null)
                KwSole.Dress(floor, KwSole.Gaps(part.position, part.forward, floor, Decks(part, floor)), tier);
            Mesh? look = floor != null ? KwMesh.Build(floor) : null;
            if (look != null && look.vertexCount == 0)
            {
                Object.Destroy(look);
                look = null;
            }
            Mesh? hard = look != null ? KwMesh.Build(floor!, true) : null;
            Meshes(model).Wear(look, hard, KwBiomeLooks.For(KwPart.Deck, tier) ?? KwPrefabs.Planks);
        }

        /// <summary>Whether a deck (its saved span) lies low enough over the planking under it for the floors there to give way to it.</summary>
        public static bool Low(string? saved)
        {
            if (!KwSpan.TryParse(saved ?? "", out KwSpan span))
                return false;
            KwDef deck = span.Def("deck");
            float low = float.IsNaN(deck.DeckZEnd) ? deck.DeckZ : Mathf.Min(deck.DeckZ, deck.DeckZEnd);
            return low < Mathf.Max(KwSole.Height(span.Family, span.FrameA), KwSole.Height(span.Family, span.FrameB)) + KwSole.Headroom;
        }

        // The decks loaded on this peer round a part: where each starts, the way it runs, and its definition.
        private static List<(Vector3 At, Vector3 Forward, KwDef Def)> Decks(Transform part, KwDef floor)
        {
            List<(Vector3, Vector3, KwDef)> decks = new List<(Vector3, Vector3, KwDef)>();
            float length = floor.Stations[floor.Stations.Count - 1].X;
            Found.Clear();
            Piece.GetAllPiecesInRadius(part.position + part.forward * (length / 2f), length / 2f + Reach, Found);
            foreach (Piece piece in Found)
            {
                if (piece == null || Utils.GetPrefabName(piece.gameObject) != DeckPrefab)
                    continue;
                ZDO? zdo = piece.GetComponent<ZNetView>()?.GetZDO();
                if (zdo != null && KwSpan.TryParse(zdo.GetString(KwGenerated.Key), out KwSpan span))
                    decks.Add((piece.transform.position, piece.transform.forward, span.Def("deck")));
            }
            Found.Clear();
            return decks;
        }

        // The model's floor child, made the first time.
        private static KwSoleMeshes Meshes(Transform model)
        {
            Transform? found = model.Find(Child);
            KwSoleMeshes? made = found != null ? found.GetComponent<KwSoleMeshes>() : null;
            if (made != null)
                return made;
            GameObject child = new GameObject(Child) { layer = model.gameObject.layer };
            child.transform.SetParent(model, false);
            child.AddComponent<MeshFilter>();
            child.AddComponent<MeshRenderer>();
            child.AddComponent<MeshCollider>();
            return child.AddComponent<KwSoleMeshes>();
        }
    }

    /// <summary>A hold floor's child: its look and its collider, the meshes it was given let go when new ones come or it goes.</summary>
    public sealed class KwSoleMeshes : MonoBehaviour
    {
        private Mesh? look, hard;

        /// <summary>Wears new meshes (none: nothing drawn or walked on) in a material (none: the one it wears).</summary>
        public void Wear(Mesh? newLook, Mesh? newHard, Material? material)
        {
            GetComponent<MeshFilter>().sharedMesh = newLook;
            MeshRenderer renderer = GetComponent<MeshRenderer>();
            renderer.enabled = newLook != null;
            if (material != null)
                renderer.sharedMaterial = material;
            MeshCollider collider = GetComponent<MeshCollider>();
            collider.sharedMesh = newHard;
            collider.enabled = newHard != null;
            Let();
            (look, hard) = (newLook, newHard);
        }

        private void OnDestroy() => Let();

        private void Let()
        {
            if (look != null)
                Destroy(look);
            if (hard != null)
                Destroy(hard);
        }
    }
}
