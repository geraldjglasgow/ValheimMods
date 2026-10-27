using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// A ship's look: every piece of its blueprint gathered and combined into one mesh per material
    /// (<see cref="KwBpMeshSet"/>: a kit piece's own meshes, a generated part's from its span or sizes), under the ship's
    /// visual holder; meshes Unity cannot read are placed apart. The hull parts holes are cut into (planking with its
    /// painted bands, decks, cabins) are combined apart, a few chunks a material, so a hit cuts one chunk again and not
    /// the whole ship (<see cref="KwHoleLooks"/>). The parts that move are left to the fittings
    /// (<see cref="KwShipFittings"/>): the rudders and the hatches, whose openings the decks leave out
    /// (<see cref="KwShipDeckHoles"/>). Masts with their spars, bowsprits and post caps are drawn on their own, so shots can
    /// cut and fell them (<see cref="KwRigLook"/>); a sail's cloth and ropes are a fitting. The holds'
    /// floors are here too, left out under the low decks as on the dock (<see cref="KwSole"/>). Drawn only where there is
    /// a screen (<see cref="KwShipBody.Drawn"/>).
    /// </summary>
    public static class KwShipLook
    {
        public static void Add(Transform holder, KwBlueprint blueprint)
        {
            KwBpMeshSet set = new KwBpMeshSet
            {
                DeckHoles = (deck, def) => KwShipDeckHoles.For(blueprint, deck, def), Decks = KwSole.Decks(blueprint), Keels = KwKeelLines.Of(blueprint),
                Apart = piece => KwHoleLooks.Takes(piece.Prefab),
            };
            // the booms swing with their sails, so they are the sails' fittings' (KwBoomSwing)
            KwBoomSwing.Apart(() =>
            {
                foreach (KwBpPiece piece in blueprint.Pieces)
                {
                    if (!KwShipFittings.Moves(piece.Prefab) && !KwSeaKinds.DrawsOwn(piece))
                        set.Add(piece, Vector3.zero);
                }
            });
            foreach ((Material material, Mesh mesh) in set.Combine("KW_ship_look"))
                Part(holder, mesh, new[] { material }, Matrix4x4.identity).AddComponent<KwMeshOwner>();
            foreach ((Mesh mesh, Material[] materials, Matrix4x4 at) in set.Loose)
                Part(holder, mesh, materials, at);
            KwHoleLooks.Keep(holder, set.Parts);
        }

        // A combined mesh is this ship's own (it goes with the ship); a loose one is a prefab's, shared.
        private static GameObject Part(Transform holder, Mesh mesh, Material[] materials, Matrix4x4 at)
        {
            GameObject part = new GameObject("look") { layer = holder.gameObject.layer };
            part.transform.SetParent(holder, false);
            part.transform.SetLocalPositionAndRotation(at.GetColumn(3), at.rotation);
            part.transform.localScale = at.lossyScale;
            part.AddComponent<MeshFilter>().sharedMesh = mesh;
            part.AddComponent<MeshRenderer>().sharedMaterials = materials;
            return part;
        }
    }
}
