using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// A rudder's linkage to its helm built on a rudder object (a placed rudder, its ghost, or a launched ship's copy), from
    /// its kit (<see cref="KwRudderKit.Lay"/>): under a "link" child, the deck tiller (its own mesh, turning on its pivot
    /// post, which is drawn with the rudder's model) and the two links, the game's rope as the sails' ropes are drawn or
    /// rods of the tier's metal (<see cref="KwRudderTier.Rod"/>); and the <see cref="KwRudderLinkage"/> that moves them with
    /// the blade. Built again whenever the rudder is; a tiller has no parts here, only its grip for the helmsman, and the
    /// steering gear none (<see cref="KwRudderHelm.Gear"/>: its shafts are drawn by <see cref="KwRudderGearLook"/>).
    /// </summary>
    public static class KwRudderLink
    {
        public const string Name = "link", HelmName = "helm";
        private const float RopeWidth = 0.022f;

        public static void Dress(GameObject piece, KwRudderKit kit, Transform blade)
        {
            Clear(piece.transform);
            KwRudderLay lay = kit.Lay;
            KwRudderLinkParts parts = new KwRudderLinkParts { Steered = lay.Steered };
            if (lay.Kind == KwRudderHelm.Tiller)
                parts.Hand = KwMesh.ToUnity(lay.Grip - kit.Pivot - Drop);
            else if (lay.Kind == KwRudderHelm.Lines)
                Lines(Holder(piece.transform), kit, parts);
            KwRudderLinkage driver = piece.GetComponent<KwRudderLinkage>() ?? piece.AddComponent<KwRudderLinkage>();
            driver.Set(blade, parts);
        }

        // From the grip down to the idle hand on the peg hanging under it.
        private static Vector3 Drop => new Vector3(0f, 0f, KwRudderLay.GripHeight - KwRudderLay.Hand);

        // The old linkage goes; renamed first, so the new one is found by name before the old is destroyed at the frame's end.
        private static void Clear(Transform piece)
        {
            Transform? old = piece.Find(Name);
            if (old == null)
                return;
            old.name = Name + "_old";
            old.gameObject.SetActive(false);
            Object.Destroy(old.gameObject);
        }

        private static Transform Holder(Transform piece)
        {
            Transform link = new GameObject(Name) { layer = piece.gameObject.layer }.transform;
            link.SetParent(piece, false);
            return link;
        }

        // The deck tiller and the two links, each from the yoke's eye to the deck tiller's.
        private static void Lines(Transform link, KwRudderKit kit, KwRudderLinkParts parts)
        {
            KwRudderLay lay = kit.Lay;
            parts.Helm = Helm(link, kit);
            parts.Hand = KwMesh.ToUnity(new Vector3(KwRudderLay.Handle, 0f, 0f) - Drop);
            for (int i = 0; i < 2; i++)
            {
                Vector3 side = new Vector3(0f, i == 0 ? lay.Half : -lay.Half, 0f);
                parts.YokeEnds[i] = KwMesh.ToUnity(side + new Vector3(0f, 0f, kit.YokeZ));
                parts.DeckEnds[i] = KwMesh.ToUnity(side);
                Vector3 yoke = kit.Pivot + side + new Vector3(0f, 0f, kit.YokeZ), deck = lay.Pivot + side;
                if (kit.Tier.Rod.HasValue)
                    parts.Rods[i] = Rod(link, "rod" + i, kit.Tier.Rod.Value, yoke, deck);
                else
                    parts.Lines[i] = Rope(link, "line" + i, yoke, deck);
            }
        }

        // The deck tiller at its pivot, its own mesh with a submesh per material.
        private static Transform Helm(Transform link, KwRudderKit kit)
        {
            Transform helm = Part(link, HelmName, KwMesh.ToUnity(kit.Lay.Pivot));
            (Mesh mesh, Material[] materials) = kit.CombinedHelm(kit.Tier.Prefab + "_helm");
            helm.GetComponent<MeshFilter>().sharedMesh = mesh;
            helm.GetComponent<MeshRenderer>().sharedMaterials = materials;
            return helm;
        }

        // A rod of the tier's metal from the yoke's eye (where it is placed) to the deck tiller's.
        private static Transform Rod(Transform link, string name, KwLook look, Vector3 yoke, Vector3 deck)
        {
            Transform rod = Part(link, name, KwMesh.ToUnity(yoke));
            MeshBuilder b = new MeshBuilder(Vector3.zero);
            KwRudderLinkShapes.Rod(b, deck - yoke);
            rod.GetComponent<MeshFilter>().sharedMesh = b.ToMesh("kw_rudder_" + name);
            Material? material = KwTierMaterials.Of(look);
            if (material != null)
                rod.GetComponent<MeshRenderer>().sharedMaterial = material;
            return rod;
        }

        // A drawn part under the linkage's holder whose mesh is its own.
        private static Transform Part(Transform link, string name, Vector3 at)
        {
            GameObject part = new GameObject(name) { layer = link.gameObject.layer };
            part.transform.SetParent(link, false);
            part.transform.localPosition = at;
            part.AddComponent<MeshFilter>();
            part.AddComponent<MeshRenderer>();
            part.AddComponent<KwMeshOwner>();
            return part.transform;
        }

        // A line in the game's rope (the leather's look when the game has none), its points in the rudder's frame.
        private static LineRenderer Rope(Transform link, string name, Vector3 yoke, Vector3 deck)
        {
            GameObject go = new GameObject(name) { layer = link.gameObject.layer };
            go.transform.SetParent(link, false);
            LineRenderer line = go.AddComponent<LineRenderer>();
            line.useWorldSpace = false;
            line.widthMultiplier = RopeWidth;
            line.textureMode = LineTextureMode.Tile;
            line.numCapVertices = 2;
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            Material? rope = KwSailCloth.Rope != null ? KwSailCloth.Rope : KwTierMaterials.Of(KwLook.Leather);
            if (rope != null)
                line.sharedMaterial = rope;
            line.positionCount = 2;
            line.SetPosition(0, KwMesh.ToUnity(yoke));
            line.SetPosition(1, KwMesh.ToUnity(deck));
            return line;
        }
    }
}
