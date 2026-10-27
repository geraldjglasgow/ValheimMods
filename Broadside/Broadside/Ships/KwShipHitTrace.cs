using System;
using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// A heavy shot's path through a launched ship (<see cref="KwShipHitRouter"/>), on its owner: the ship's colliders as its
    /// body stands (the boxes as its holes have opened them, <see cref="KwHoleBoxes"/>; its railings, <see cref="KwShipRails"/>;
    /// its fittings; the pieces built aboard) and the characters aboard, along the flight from a metre before the hit to
    /// past the ship's far end, nearest first, each classed by kind (<see cref="KwShipHitClass"/>). The colliders of one
    /// thing struck next to each other count once; nobody beyond the ship's last collider is on its path (the shot meets them
    /// as it flies on), and the shooter never is.
    /// </summary>
    public static class KwShipHitTrace
    {
        private const float Back = 1f, Beside = 0.5f;
        private static int mask;

        private static int Mask => mask != 0 ? mask : mask = LayerMask.GetMask("Default", "static_solid", "Default_small", "piece", "piece_nonsolid",
            "character", "character_net", "character_ghost", "hitbox", "character_noenv", "vehicle");

        /// <summary>What a shot meets from a point of the world along a way, nearest first.</summary>
        public static List<KwShipHitMeet> Through(KwShipHitRouter ship, Vector3 at, Vector3 way, ZDOID shooter)
        {
            RaycastHit[] hits = Physics.RaycastAll(at - way * Back, way, Back + Reach(ship), Mask, QueryTriggerInteraction.Ignore);
            Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
            List<KwShipHitMeet> path = new List<KwShipHitMeet>();
            foreach (RaycastHit hit in hits)
            {
                if (KwShipHitClass.Of(ship, Started(hit, at - way * Back), way, shooter) is KwShipHitMeet meet && !Again(path, meet))
                    path.Add(meet);
            }
            Trim(path);
            return path;
        }

        // A hit on a collider the trace started inside has no point of its own: the trace's start.
        private static RaycastHit Started(RaycastHit hit, Vector3 from)
        {
            if (hit.distance <= 0f)
                hit.point = from;
            return hit;
        }

        /// <summary>
        /// The same thing struck again right after: a box beside a box of the same deck, a railing's next stretch on the
        /// same run (one blow breaks them both by the post), a body's or a rider's second collider, the rudder's blade
        /// and its hit box.
        /// </summary>
        public static bool Again(List<KwShipHitMeet> path, KwShipHitMeet meet)
        {
            if (path.Count == 0)
                return false;
            KwShipHitMeet last = path[path.Count - 1];
            bool near = meet.Distance - last.Distance < Beside;
            if (meet.Kind == KwShipHitRouter.Rail && last.Kind == KwShipHitRouter.Rail)
                return near && meet.Piece == last.Piece && meet.Part / 1000 == last.Part / 1000;
            if (meet.Kind == KwShipHitRouter.Character || meet.Kind == KwShipHitRouter.Rider)
                return last.Same(meet);
            return near && meet.Kind == last.Kind && meet.Piece == last.Piece && meet.Part == last.Part;
        }

        /// <summary>Nothing past the ship's last collider of its own: characters there are met by the shot flying on.</summary>
        public static void Trim(List<KwShipHitMeet> path)
        {
            int last = path.FindLastIndex(m => m.Kind != KwShipHitRouter.Character);
            path.RemoveRange(last + 1, path.Count - last - 1);
        }

        // As far as a shot could go inside the ship: its length, beam and depth together, and a little.
        private static float Reach(KwShipHitRouter ship) => ship.Size + 2f;
    }

    /// <summary>
    /// What a collider a shot meets is to a launched ship (<see cref="KwShipHitTrace"/>): a character (its own body), else only
    /// the ship's own colliders count: one tagged (<see cref="KwShipHitPart"/>: railings, rig); a piece built aboard
    /// (<see cref="KwShipRider"/>); one of the ship's boxes, its hull part found by the hull map (<see cref="KwHullPartMap"/>:
    /// planking, transoms and keels are the hull, a deck, a cabin's sides, walls and roof); the rudder's blade; any other
    /// fitting. Null for anything else, and for the shooter.
    /// </summary>
    public static class KwShipHitClass
    {
        public static KwShipHitMeet? Of(KwShipHitRouter ship, RaycastHit hit, Vector3 way, ZDOID shooter)
        {
            Collider c = hit.collider;
            Character? body = c.GetComponentInParent<Character>();
            if (body != null)
                return body.GetZDOID() == shooter || body.IsDead() ? (KwShipHitMeet?)null : Meet(hit, KwShipHitRouter.Character, -1, -1, body);
            if (c.attachedRigidbody != ship.Body && !c.transform.IsChildOf(ship.transform))
                return null;
            KwShipHitPart? tag = c.GetComponentInParent<KwShipHitPart>();
            if (tag != null)
                return Meet(hit, tag.Kind, tag.Piece, tag.Part, tag);
            KwShipRider? rider = c.GetComponentInParent<KwShipRider>();
            if (rider != null)
                return Meet(hit, KwShipHitRouter.Rider, -1, -1, rider);
            Transform? colliders = ship.transform.Find(KwShipPrefab.Colliders);
            if (colliders != null && c.transform.IsChildOf(colliders))
                return Box(ship, hit, way);
            return Fitting(ship, hit);
        }

        // One of the ship's boxes: the hull part the map finds for the point, facing as the struck face does (a deck's slab
        // struck at a slant near the side is the deck, not the planking); the hull when none is near. A planking box lies on
        // the face as drawn, turned fore and aft where the hull narrows, and the map's planking faces its sections' way: its
        // normal is taken in the section's plane, as the map's are (else a cabin's side over it wins near the stern).
        private static KwShipHitMeet Box(KwShipHitRouter ship, RaycastHit hit, Vector3 way)
        {
            if (hit.collider.name == KwHullSkin.Name)
                hit.normal = Sectioned(ship.Body!.rotation * Vector3.forward, hit.normal);
            Matrix4x4 into = Matrix4x4.TRS(ship.Body!.position, ship.Body.rotation, Vector3.one).inverse;
            Vector3 facing = hit.normal.sqrMagnitude > 0.5f ? hit.normal : way;
            if (!ship.Map.Struck(into.MultiplyPoint3x4(hit.point), into.MultiplyVector(facing), out int part, out _, out _))
                return Meet(hit, KwShipHitRouter.Hull, -1, -1, ship);
            return Meet(hit, KindOf(ship.Blueprint!.Pieces[part].Prefab), part, -1, ship);
        }

        /// <summary>A normal less its part along the ship (a unit), kept as it is when it points nearly along the ship.</summary>
        public static Vector3 Sectioned(Vector3 along, Vector3 normal)
        {
            Vector3 flat = normal - along * Vector3.Dot(normal, along);
            return flat.sqrMagnitude > 0.04f ? flat.normalized : normal;
        }

        /// <summary>The kind of a hull part: a deck, a cabin, or the hull (planking).</summary>
        public static string KindOf(string prefab) => prefab == KwToolPieces.PartPrefix + "deck" ? KwShipHitRouter.Deck
            : KwHouses.IsPart(prefab) ? KwShipHitRouter.Cabin : KwShipHitRouter.Hull;

        // A fitting's collider: the rudder's blade (its own kind, the rudder's piece), else any other fitting.
        private static KwShipHitMeet Fitting(KwShipHitRouter ship, RaycastHit hit)
        {
            for (Transform? t = hit.collider.transform; t != null && t != ship.transform; t = t.parent)
            {
                if (t.name == KwRudder.BladeName && t.parent != null && t.parent.name == "rudder")
                    return Meet(hit, KwShipHitRouter.Rudder, Piece(ship, t.parent), -1, t.parent);
            }
            return Meet(hit, KwShipHitRouter.Fitting, -1, -1, hit.collider.transform.parent);
        }

        // The blueprint piece a fitting was made from: the rudder whose place it stands at.
        private static int Piece(KwShipHitRouter ship, Transform fitting)
        {
            List<KwBpPiece> pieces = ship.Blueprint!.Pieces;
            for (int i = 0; i < pieces.Count; i++)
            {
                if (KwRudder.Is(pieces[i].Prefab) && (pieces[i].Position - fitting.localPosition).sqrMagnitude < 0.01f)
                    return i;
            }
            return -1;
        }

        private static KwShipHitMeet Meet(RaycastHit hit, string kind, int piece, int part, UnityEngine.Object? owner) =>
            new KwShipHitMeet(hit.collider, hit.point, hit.normal, hit.distance, kind, piece, part, owner);
    }
}
