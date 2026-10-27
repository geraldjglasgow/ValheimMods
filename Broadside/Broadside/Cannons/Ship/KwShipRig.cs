using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// A ship cannon's moving and laid parts on the bench (after <see cref="KwArmBuild"/> has made its joints, parts and
    /// moving parts), each a node of its own, none with a collider: the four trucks about their hubs on the carriage (the
    /// yaw part: <c>truck_fl</c>, <c>truck_fr</c>, <c>truck_rl</c>, <c>truck_rr</c>, turned by <see cref="KwWheels"/>), its
    /// timber and its bolt heads; the quoin on the carriage (<see cref="KwQuoin.Name"/>) and the points of the breech it
    /// bears on, on the barrel (<c>mv_gun</c>); the breeching's points (the ringbolts' rings on the piece, the eyebolts' on
    /// the carriage, the rings' on the barrel) and its lengths of rope, all under the piece (<see cref="KwBreeching"/>).
    /// Then the quoin and the ropes are laid once at rest, as the hammer's ghost and icon show it.
    /// </summary>
    public static class KwShipRig
    {
        private static readonly float[] Sides = { 1f, -1f };

        public static void Fit(Transform root, KwShipSpec ship)
        {
            Transform yaw = root.Find(KwArm.YawName);
            foreach ((string name, Vector3 hub, float radius) in ship.Trucks())
                Truck(yaw, name, hub, radius, ship);
            KwArmMeshes.Child(KwCannonBody.Node(yaw, KwQuoin.Name, Vector3.zero), KwQuoin.Name + "_" + Look(ship.Wood), b => KwShipCarriage.Quoin(b, ship), ship.Wood);
            Transform? gun = Gun(root);
            if (gun != null)
                Barrel(gun, ship);
            Ropes(root, yaw, ship);
            KwQuoin.Of(root, ship)?.Place();
            KwBreeching.Of(root, ship)?.Lay();
        }

        /// <summary>The barrel's moving part (<c>mv_gun</c>, under the pitch part); null when it has none.</summary>
        public static Transform? Gun(Transform root)
        {
            Transform? yaw = root.Find(KwArm.YawName), pitch = yaw != null ? yaw.Find(KwArm.PitchName) : null;
            return pitch != null ? pitch.Find(KwArmMover.Prefix + "gun") : null;
        }

        // A truck about its hub on the carriage: its wooden disc and, on its outer face, its bolt heads.
        private static void Truck(Transform yaw, string name, Vector3 hub, float radius, KwShipSpec ship)
        {
            Transform node = KwCannonBody.Node(yaw, name, hub);
            float side = Mathf.Sign(hub.y), width = ship.Width;
            KwArmMeshes.Child(node, name + "_" + Look(ship.Wood), b => KwShipCarriage.Truck(b, radius, width), ship.Wood);
            KwArmMeshes.Child(node, name + "_" + Look(ship.Metal), b => KwShipIrons.TruckBolts(b, radius, width, side), ship.Metal);
        }

        // On the barrel (the gun's moving part has its pivot at the trunnions): the points the quoin bears on, and the
        // rings the breeching is made fast to.
        private static void Barrel(Transform gun, KwShipSpec ship)
        {
            for (int i = 0; i < ship.Bears.Length; i++)
                KwCannonBody.Node(gun, KwQuoin.Bears + i, new Vector3(ship.Bears[i].x, 0f, -ship.Bears[i].y));
            foreach (float side in Sides)
                KwCannonBody.Node(gun, KwBreeching.Named(KwBreeching.Ring, side), ship.Ring(side));
        }

        // The ringbolts' rings on the piece and the eyebolts' on the carriage, and every length of rope under the piece.
        private static void Ropes(Transform root, Transform yaw, KwShipSpec ship)
        {
            Mesh? line = null;
            foreach (float side in Sides)
            {
                KwCannonBody.Node(root, KwBreeching.Named(KwBreeching.Anchor, side), ship.Anchor(side));
                KwCannonBody.Node(yaw, KwBreeching.Named(KwBreeching.Eye, side), ship.Eye(side));
                for (int i = 0; i <= KwBreeching.Pieces; i++)
                    line = Length(root, KwBreeching.Named(KwBreeching.Rope, side) + i, ship, line);
            }
        }

        // One length of rope: a node with a unit line of rope under it, all lengths sharing the first one's mesh.
        private static Mesh Length(Transform root, string name, KwShipSpec ship, Mesh? line)
        {
            Transform node = KwCannonBody.Node(root, name, Vector3.zero);
            float radius = ship.Rope;
            if (line == null)
                return KwArmMeshes.Child(node, name + "_linen", b => Solid.Log(b, Vector3.zero, new Vector3(1f, 0f, 0f), radius, 6), KwLook.Linen)
                    .GetComponent<MeshFilter>().sharedMesh;
            GameObject part = new GameObject(name + "_linen") { layer = node.gameObject.layer };
            part.transform.SetParent(node, false);
            part.AddComponent<MeshFilter>().sharedMesh = line;
            MeshRenderer renderer = part.AddComponent<MeshRenderer>();
            Material? material = KwTierMaterials.Of(KwLook.Linen);
            if (material != null)
                renderer.sharedMaterial = material;
            return line;
        }

        private static string Look(KwLook look) => look.ToString().ToLowerInvariant();
    }
}
