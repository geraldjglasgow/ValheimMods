using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// The rudders, one a biome (<see cref="KwRudderTiers"/>), each its own design in its own materials: a blade hung on
    /// the after face of a stern post (every keel with a post, a stem's or a flat stern's, has a "Rudder" snap point at
    /// the post's foot), on a round stock that rises past the post's top to a tiller over it. Each is sized to its post
    /// (<see cref="KwRudderFit"/>): the prefab is the standard post's size (3 m over the standard keel), and a rudder on
    /// another post is built again to that post's height, width and depth, its ghost while it is placed
    /// (<see cref="KwRudderPlace"/>) and the piece on every peer (<see cref="KwRudderBlade"/>). What holds it to the post stays put (the "model", one
    /// mesh with a submesh per material); the stock, blade, head and tiller or yoke are the "blade" child, which stands
    /// amidships. How it is steered from the deck (<see cref="KwRudderLay"/>: a tiller to the helm's hand, a yoke with
    /// lines or rods to a deck tiller on its own post, or a captain's wheel's steering gear, <see cref="KwRudderGear"/>) is
    /// picked from the stern it hangs on and built with it (<see cref="KwRudderLink"/>). It goes only on a post's rudder point, centred and facing the ship. Kit axes: x
    /// forward (the origin on the post's after face at its foot, the blade aft of it), y left, z up.
    /// </summary>
    public static class KwRudder
    {
        public const string Prefix = "KW_rudder_", BladeName = "blade";

        /// <summary>The rudder's turn at full helm, each way, in degrees (the game's ships turn theirs 30): its linkage must clear the stern that far.</summary>
        public const float MaxAngle = 35f;

        public static bool Is(string prefab) => prefab.StartsWith(Prefix, System.StringComparison.Ordinal);

        public static IEnumerable<KwDef> All()
        {
            foreach (KwRudderTier tier in KwRudderTiers.All)
                yield return Def(tier);
        }

        // Its model is first made from its post parts in one material (the kit's way); Setup dresses it in its materials.
        private static KwDef Def(KwRudderTier tier)
        {
            KwDef def = new KwDef(tier.Prefab, tier.Label, "fitting")
            {
                Shape = b => tier.Fixed(new KwRudderKit(tier, KwRudderFit.Standard, b)),
                Wood = true,
                Setup = prefab => Setup(prefab, tier),
            };
            def.Snaps.Add(("Rudder pin", Vector3.zero));
            return def;
        }

        // The blade child with a box the hammer and the hover can hit, both parts dressed, and the turning component.
        private static void Setup(GameObject prefab, KwRudderTier tier)
        {
            GameObject blade = new GameObject(BladeName) { layer = prefab.layer };
            blade.transform.SetParent(prefab.transform, false);
            blade.AddComponent<MeshFilter>();
            blade.AddComponent<MeshRenderer>();
            blade.AddComponent<BoxCollider>();
            Mesh? first = prefab.transform.Find("model")?.GetComponent<MeshFilter>().sharedMesh;
            Dress(prefab, tier, KwRudderFit.Standard, false, KwRudderLay.Plain);
            if (first != null)
                Object.Destroy(first);
            prefab.AddComponent<KwRudderBlade>();
            prefab.GetComponent<Piece>().m_description = $"{tier.Made} Hangs on a stern post and is sized to the ship, its blade from the keel up to the waterline; a tiller, or a yoke with lines or rods " +
                "to a deck tiller, links it to the deck where the stern allows; with a captain's wheel aboard, a steering gear under the deck.";
        }

        /// <summary>
        /// A placed rudder or its ghost built again to a post's fit and linked to its helm: what holds
        /// it to the post, its blade, and the deck tiller and links (<see cref="KwRudderTiers.Of(GameObject)"/> finds which
        /// rudder it is).
        /// </summary>
        public static void Refit(GameObject piece, KwRudderFit fit, KwRudderLay? lay = null)
        {
            KwRudderTier? tier = KwRudderTiers.Of(piece);
            if (tier != null)
                Dress(piece, tier, fit, true, lay ?? KwRudderLay.Plain);
        }

        // The model (what stays on the post, the deck tiller's post too), the blade and the linkage built to a fit and a
        // lay; owned: a placed rudder's or ghost's own meshes, destroyed with it or when it is built again (the prefab's
        // are shared by every copy).
        private static void Dress(GameObject piece, KwRudderTier tier, KwRudderFit fit, bool owned, KwRudderLay lay)
        {
            Transform? model = piece.transform.Find("model"), blade = piece.transform.Find(BladeName);
            if (model == null || blade == null)
                return;
            KwRudderKit kit = tier.Build(fit, lay);
            Mesh straps = Wear(model, kit, false, owned, tier.Prefab + "_fitted");
            MeshCollider? collider = model.GetComponent<MeshCollider>();
            if (collider != null)
                collider.sharedMesh = straps;
            blade.localPosition = KwMesh.ToUnity(tier.Pivot(fit));
            Wear(blade, kit, true, owned, tier.Prefab + "_blade");
            Box(blade.GetComponent<BoxCollider>(), kit);
            if (owned)
                KwRudderLink.Dress(piece, kit, blade);
        }

        private static Mesh Wear(Transform part, KwRudderKit kit, bool onBlade, bool owned, string name)
        {
            (Mesh mesh, Material[] materials) = kit.Combined(onBlade, name);
            if (owned)
                Own(part, mesh);
            else
                part.GetComponent<MeshFilter>().sharedMesh = mesh;
            part.GetComponent<MeshRenderer>().sharedMaterials = materials;
            return mesh;
        }

        private static void Own(Transform part, Mesh mesh)
        {
            MeshFilter filter = part.GetComponent<MeshFilter>();
            KwMeshOwner? owner = part.GetComponent<KwMeshOwner>();
            if (owner != null && filter.sharedMesh != null)
                Object.Destroy(filter.sharedMesh);
            filter.sharedMesh = mesh;
            if (owner == null)
                part.gameObject.AddComponent<KwMeshOwner>();
        }

        // Round the blade from the stock to its longest reach aft, from the stock's foot to the post's top or the head.
        private static void Box(BoxCollider box, KwRudderKit k)
        {
            float length = k.Tier.Longest() * k.B + 0.05f, foot = k.StockFoot, tall = Mathf.Max(k.Top, k.HeadBase + 0.15f * k.T) - foot;
            box.center = KwMesh.ToUnity(new Vector3(-length / 2f, 0f, foot + tall / 2f));
            box.size = new Vector3(0.12f * Mathf.Max(k.T, k.BT), tall, length);
        }
    }
}
