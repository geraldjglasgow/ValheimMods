using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// The helmsman at a ship's rudder: standing on the deck on his own feet beside the grip of what steers it
    /// (<see cref="KwRudderLay"/>: the tiller's end, or the deck tiller's on its own post with the yoke's lines or rods
    /// running aft to the rudder's head), facing the bow, his hand on the peg under it (<see cref="KwTillerGrip"/>). He is
    /// not attached: his feet stay planted as the helm swings and he steps after the grip with the game's own walk
    /// (<see cref="KwHelmsman"/>), so the player's own sync carries him to every peer. The helm's attach point stays at
    /// his stand with the helm amidships, the reach the game measures taking the helm from. A rudder saved before it was
    /// linked to a helm is linked to the first deck forward of its post (<see cref="Lay"/>).
    /// </summary>
    public static class KwShipTiller
    {
        private const float Look = 3f, Below = 15f, Side = 0.35f, Step = 0.2f, Ahead = 4f;

        /// <summary>The helmsman at a rudder's grip; false when it has none to stand at (he then sits at the stern).</summary>
        public static bool Stand(Transform ship, ShipControlls helm, Transform controls, Transform blade)
        {
            KwRudderLinkage? link = blade.parent != null ? blade.parent.GetComponent<KwRudderLinkage>() : null;
            if (link == null || !link.Steered)
                return false;
            Transform rudder = link.transform;
            Vector3 hand = ship.InverseTransformPoint(rudder.TransformPoint(link.HandLocal));
            Vector3 feet = ship.InverseTransformPoint(rudder.TransformPoint(link.FeetLocal));
            float lift = (Deck(ship, feet) ?? feet.y) - feet.y;
            controls.localPosition = hand;
            helm.m_attachPoint.position = ship.TransformPoint(feet + Vector3.up * lift);
            helm.m_attachPoint.rotation = ship.rotation;
            helm.m_attachAnimation = "";
            helm.m_detachOffset = new Vector3(0f, 0.2f, 0f);
            KwHelmsman.OnShip(helm, new KwTillerGrip(link, ship.GetComponent<Ship>(), lift));
            return true;
        }

        /// <summary>
        /// A lay for a rudder whose blueprint has none: lines to a deck tiller at the first deck the ship's colliders give
        /// forward of its post (the head a little over the post's top, the yoke well clear of the post's sides); the
        /// tiller over the post's top when there is no deck there.
        /// </summary>
        public static KwRudderLay Lay(Transform ship, Transform rudder, KwRudderFit fit)
        {
            for (float x = fit.PostDepth + 0.4f; x <= fit.PostDepth + Ahead; x += Step)
            {
                Vector3 at = ship.InverseTransformPoint(rudder.TransformPoint(KwMesh.ToUnity(new Vector3(x, 0f, fit.Height))));
                float? deck = Deck(ship, at);
                if (deck == null)
                    continue;
                float z = KwMesh.ToKit(rudder.InverseTransformPoint(ship.TransformPoint(new Vector3(at.x, deck.Value, at.z)))).z;
                return new KwRudderLay(KwRudderHelm.Lines, 0.3f, x + 0.3f, z, Mathf.Max(0.55f, fit.PostHalf + 0.2f));
            }
            return KwRudderLay.Plain;
        }

        // The deck's walking surface under a point of the ship (its own colliders): the highest face turned up with room to
        // stand across it (as high a metre to either side, so a plank's top edge or a narrow stern's sheer is no deck), from
        // Look over the point down to Below under it (a tall stern post's top stands metres over the deck, the user's
        // longship 2026-10-26: the tiller then hung over the helmsman's head); null where it has none.
        private static float? Deck(Transform ship, Vector3 local)
        {
            Collider[] all = Near(ship, local);
            foreach (float y in Tops(ship, all, local))
            {
                if (Wide(ship, all, local + Vector3.right * Side, y) && Wide(ship, all, local - Vector3.right * Side, y))
                    return y;
            }
            return null;
        }

        // The ship's own colliders standing within a little of a point's column (the rest cannot be under it).
        private static Collider[] Near(Transform ship, Vector3 local)
        {
            Transform? colliders = ship.Find(KwShipPrefab.Colliders);
            Vector3 at = ship.TransformPoint(local);
            List<Collider> near = new List<Collider>();
            foreach (Collider c in colliders != null ? colliders.GetComponentsInChildren<Collider>() : new Collider[0])
            {
                Bounds b = c.bounds;
                b.Expand(new Vector3(2f * Side + 1f, 0f, 2f * Side + 1f));
                if (at.x >= b.min.x && at.x <= b.max.x && at.z >= b.min.z && at.z <= b.max.z)
                    near.Add(c);
            }
            return near.ToArray();
        }

        // The heights (ship's frame) of the faces turned up under a point, highest first.
        private static List<float> Tops(Transform ship, Collider[] all, Vector3 local)
        {
            Ray down = new Ray(ship.TransformPoint(local + Vector3.up * Look), -ship.up);
            List<float> tops = new List<float>();
            foreach (Collider collider in all)
            {
                if (collider.Raycast(down, out RaycastHit hit, Look + Below) && Vector3.Dot(hit.normal, ship.up) > 0.7f)
                    tops.Add(ship.InverseTransformPoint(hit.point).y);
            }
            tops.Sort((a, b) => b.CompareTo(a));
            return tops;
        }

        // Whether a face turned up stands at that height (within a hand) under a point beside it.
        private static bool Wide(Transform ship, Collider[] all, Vector3 local, float y) => Tops(ship, all, local).Exists(t => Mathf.Abs(t - y) < 0.12f);
    }

    /// <summary>
    /// The "no pose" of a tiller's helm (<see cref="KwShipTiller.Stand"/>): should the game attach the helmsman itself (its
    /// own answer, when <see cref="KwHelmsman"/> could not take the helm), it sets the attach pose by name, and an empty
    /// name is no parameter of the player's animator.
    /// </summary>
    [HarmonyPatch(typeof(ZSyncAnimation), nameof(ZSyncAnimation.SetBool), typeof(string), typeof(bool))]
    public static class KwNoPosePatch
    {
        [HarmonyPrefix]
        private static bool Prefix(string name) => !string.IsNullOrEmpty(name);
    }
}
