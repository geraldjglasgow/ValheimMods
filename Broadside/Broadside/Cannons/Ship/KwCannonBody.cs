using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// A ship cannon's prefab finished on the bench (after <see cref="KwArmBuild"/> has made its joints, parts and moving
    /// parts): its trucks, quoin and breeching (<see cref="KwShipRig"/>), and its colliders, all on the piece itself and
    /// none on the carriage, so they never move when the carriage rolls back at a shot or is levered round (the piece stays
    /// where it is; only its look moves, <see cref="KwShipRecoil"/>): a ball low in each truck, a box round the brackets
    /// and one round the barrel out ahead of them (hovering them mans it), and the rear, a child low behind the brackets
    /// carrying <see cref="KwArmTow"/> (Shift+E on it hauls the cannon) with the hauler's two grips on the brackets' rear
    /// ends. It has no body of its own: built only on a launched ship, it rides it (the ship's child, its colliders part of
    /// the ship's body, <see cref="KwShipRider"/>) and is hauled about the deck by its hauler's game
    /// (<see cref="KwDeckHaul"/>). Its wear: it holds up nothing and has no fixed place.
    /// </summary>
    public static class KwCannonBody
    {
        public const string RearName = "haul", GripLeft = "tow_l", GripRight = "tow_r";

        public static void Fit(GameObject prefab, KwArmDef arm, KwShipSpec ship)
        {
            Transform root = prefab.transform;
            KwShipRig.Fit(root, ship);
            Contacts(root, ship);
            Boxes(root, arm, ship);
            Rear(root, ship);
            WearNTear? wear = prefab.GetComponent<WearNTear>();
            if (wear != null)
                (wear.m_supports, wear.m_staticPosition) = (false, false);
            // its trucks may touch the deck it is built on
            prefab.GetComponent<Piece>().m_noClipping = false;
            prefab.AddComponent<KwWheels>();
        }

        /// <summary>A box collider on a node, its centre and size in kit axes.</summary>
        public static BoxCollider Box(Transform root, Vector3 centre, Vector3 size)
        {
            BoxCollider box = root.gameObject.AddComponent<BoxCollider>();
            box.center = KwMesh.ToUnity(centre);
            Vector3 unity = KwMesh.ToUnity(size);
            box.size = new Vector3(Mathf.Abs(unity.x), Mathf.Abs(unity.y), Mathf.Abs(unity.z));
            return box;
        }

        /// <summary>An empty node under a parent at a kit point of the parent's.</summary>
        public static Transform Node(Transform parent, string name, Vector3 kit)
        {
            Transform node = new GameObject(name) { layer = parent.gameObject.layer }.transform;
            node.SetParent(parent, false);
            node.localPosition = KwMesh.ToUnity(kit);
            return node;
        }

        // A ball low in each truck, touching the deck under its hub.
        private static void Contacts(Transform root, KwShipSpec ship)
        {
            foreach ((string _, Vector3 hub, float radius) in ship.Trucks())
            {
                SphereCollider ball = root.gameObject.AddComponent<SphereCollider>();
                float r = radius * 0.75f;
                (ball.center, ball.radius) = (KwMesh.ToUnity(new Vector3(hub.x, hub.y, r)), r);
            }
        }

        // A box round the brackets (clear of the deck) and one round the barrel from their front to the muzzle.
        private static void Boxes(Transform root, KwArmDef arm, KwShipSpec ship)
        {
            float bottom = ship.Rear + ship.TreeTop - 0.02f, top = ship.Height + 0.05f, front = ship.BracketFront, rear = ship.BracketRear;
            Box(root, new Vector3((front + rear) / 2f, 0f, (bottom + top) / 2f), new Vector3(front - rear, 2f * (ship.Outer + 0.01f), top - bottom));
            float muzzle = ship.TrunnionX + arm.Muzzle.x, r = ship.Under.y * 0.85f;
            Box(root, new Vector3((front + muzzle) / 2f, 0f, ship.Height), new Vector3(muzzle - front, 2f * r, 2f * r));
        }

        // The rear as a child low across the brackets' rear ends and rear trucks (standing a little proud of the box round
        // the brackets, so hovering it finds it, but no further: the gunner stands close behind), with the tow that hauls
        // the cannon, and the hauler's grips.
        private static void Rear(Transform root, KwShipSpec ship)
        {
            float x0 = ship.BracketRear - 0.02f, x1 = ship.RearAxle, top = ship.RearTop - 0.06f, half = ship.TruckOut + 0.02f;
            Transform rear = Node(root, RearName, new Vector3((x0 + x1) / 2f, 0f, (0.03f + top) / 2f));
            BoxCollider box = rear.gameObject.AddComponent<BoxCollider>();
            box.size = new Vector3(2f * half, top - 0.03f, x1 - x0);
            rear.gameObject.AddComponent<KwArmTow>();
            Node(root, GripLeft, ship.Grip(1f));
            Node(root, GripRight, ship.Grip(-1f));
        }
    }
}
