using System;
using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// An opening of a launched ship a ship cannon may be set behind (<see cref="KwGunports"/>), in the ship's frame: its
    /// part and wall, its middle on the wall (the part's kit u and z) and on the outer face (<see cref="At"/>), its way out
    /// laid level (<see cref="Way"/>: square to the wall at its middle, as its tunnel is cut), and its sill's and head's
    /// heights. Pure.
    /// </summary>
    public sealed class KwGunport
    {
        // The least part of its way out that must lie level: a wall turned further toward the sky or the deck takes no cannon.
        private const float Flat = 0.7f;

        public KwGunport(KwBpPiece piece, KwOpeningSurface wall, Vector2 middle, float sill)
        {
            (Piece, Wall, Middle) = (piece, wall, middle);
            At = KwShipOpeningPlan.Ship(piece, wall.Point(middle.x, middle.y, 0f));
            Vector3 way = piece.Rotation * KwMesh.ToUnity(wall.Out(middle.x, middle.y));
            way.y = 0f;
            Way = way.sqrMagnitude > Flat * Flat ? way.normalized : Vector3.zero;
            (Sill, Top) = (sill, 2f * At.y - sill);
        }

        public KwBpPiece Piece { get; }
        public KwOpeningSurface Wall { get; }
        public Vector2 Middle { get; }
        public Vector3 At { get; }
        public Vector3 Way { get; }
        public float Sill { get; }
        public float Top { get; }

        /// <summary>Whether a cannon can face out through it: its wall stands near upright there.</summary>
        public bool Upright => Way != Vector3.zero;

        /// <summary>
        /// How far out from its middle, along its way (less than 0: inboard), the wall's inside face is at a height of the
        /// ship's frame, on the port's middle line (the height held within the wall's own).
        /// </summary>
        public float Inside(float height)
        {
            Vector3 local = Quaternion.Inverse(Piece.Rotation) * (new Vector3(At.x, height, At.z) - Piece.Position);
            (float lo, float hi) = Wall.Range(Middle.x);
            float z = Mathf.Clamp(KwMesh.ToKit(local).z, lo, hi);
            Vector3 inside = KwShipOpeningPlan.Ship(Piece, Wall.Point(Middle.x, z, -Wall.Thick));
            return Vector3.Dot(inside - At, Way);
        }
    }

    /// <summary>
    /// The gunports of a launched ship as a ship cannon's placing sees them (<see cref="KwArmPortSnap"/>): every opening its
    /// openings' plan has but doorways (<see cref="KwShipOpenings.Plan"/>: gunports, portholes and windows, in its planking,
    /// its cabins' raised sides and their own walls) whose wall stands near upright, read once a plan (planned again after
    /// an edit at sea); the one nearest an aim on its own deck; and where a cannon stands run out behind one, as the
    /// broadside-ships skill's generator sets the cannons it arms a ship with (<see cref="Stand"/>). Pure but for the deck's
    /// ray.
    /// </summary>
    public static class KwGunports
    {
        /// <summary>How near the aim a port's middle must be across the deck (the ship's frame, heights left out), metres.</summary>
        public const float Reach = 2.5f;

        // How far under the deck under a port and over its head the aim may be and still be on its deck (the deck, or the
        // hull's side about the port).
        private const float Below = 0.3f, Above = 0.3f;

        // Run out, as the generator sets a cannon: its brackets' front this far inside the wall's inside face at the
        // trunnions' height, and its front trucks this far inside it low down, at these heights over the deck.
        private const float Bracket = 0.12f, Truck = 0.06f;
        private static readonly float[] Low = { 0.1f, 0.2f, 0.3f, 0.4f, 0.5f };

        // How far under a port's sill its deck is looked for; how near a port's spot another cannon's middle makes it taken.
        private const float Drop = 2.5f, Occupied = 0.6f;

        private static readonly List<KwGunport> ports = new List<KwGunport>(), near = new List<KwGunport>();
        private static readonly RaycastHit[] hits = new RaycastHit[16];
        private static readonly IComparer<RaycastHit> Nearer = Comparer<RaycastHit>.Create((a, b) => a.distance.CompareTo(b.distance));
        private static readonly Comparison<KwGunport> Closer = (a, b) => Across(a.At, aim).CompareTo(Across(b.At, aim));
        private static KwShipOpeningPlan? planned;
        private static Vector3 aim;
        private static int mask;

        /// <summary>A ship's ports, read again only when its openings were planned again (the last plan's are kept).</summary>
        public static List<KwGunport> Of(KwShipOpeningPlan plan, KwBlueprint blueprint)
        {
            if (plan == planned)
                return ports;
            ports.Clear();
            foreach (KwShipOpening o in plan.Openings)
            {
                KwGunport? port = Port(o, blueprint);
                if (port != null)
                    ports.Add(port);
            }
            planned = plan;
            return ports;
        }

        /// <summary>
        /// The port nearest an aim (a point of the ship's frame) across the deck, within <see cref="Reach"/>, with the aim on
        /// its deck, the barrel at its height and no cannon in it yet, and that deck's height; null for none. A port's deck is the first of the ship's
        /// own faces under where a cannon would stand behind it, going down from the port's middle; the aim is on it from a
        /// little under it to a little over the port's head (aimed at the hull's side about the port too).
        /// </summary>
        public static (KwGunport Port, float Deck)? Nearest(List<KwGunport> all, Vector3 at, KwShipRiders ship, KwShipSpec spec, Transform? ghost)
        {
            aim = at;
            Near(all, at).Sort(Closer);
            foreach (KwGunport port in near)
            {
                float? deck = Deck(ship, Stand(port, spec, port.Sill), port.At.y, ghost);
                if (deck != null && at.y >= deck.Value - Below && Reaches(port, spec, deck.Value) && !Taken(ship, Stand(port, spec, deck.Value)))
                    return (port, deck.Value);
            }
            return null;
        }

        // Whether the barrel, its trunnions' height over the deck, lies between the port's sill and head: a porthole high in
        // a cabin's side, which the barrel would sit under, is passed over.
        private static bool Reaches(KwGunport port, KwShipSpec spec, float deck)
        {
            float barrel = deck + spec.Height;
            return barrel > port.Sill && barrel < port.Top;
        }

        // Whether a cannon riding the ship already stands at a spot (its middle within Occupied across the deck): a port
        // with its cannon in it is passed over (cannons skip the game's clipping test, so one would go into the other).
        private static bool Taken(KwShipRiders ship, Vector3 spot)
        {
            foreach (KwShipRider rider in ship.Riders)
            {
                if (rider != null && Across(rider.transform.localPosition, spot) < Occupied && rider.GetComponent<KwArm>() != null)
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Where a ship cannon stands run out behind a port, on a deck at a height of the ship's frame: on the port's middle
        /// line, as near the wall as its brackets' front (<see cref="Bracket"/> inside its inside face at the trunnions'
        /// height) and its front trucks (<see cref="Truck"/> inside it low down: a flaring hull is narrower there) let it.
        /// Its muzzle then reaches out through the port, if the port is low enough for the barrel.
        /// </summary>
        public static Vector3 Stand(KwGunport port, KwShipSpec spec, float deck)
        {
            float back = port.Inside(deck + spec.Height) - Bracket - spec.BracketFront, trucks = spec.FrontAxle + spec.Front;
            foreach (float up in Low)
                back = Mathf.Min(back, port.Inside(deck + up) - Truck - trucks);
            return new Vector3(port.At.x, deck, port.At.z) + port.Way * back;
        }

        // An opening as a port: none for a doorway, one not fitted to its wall, or one in a wall far from upright.
        private static KwGunport? Port(KwShipOpening o, KwBlueprint blueprint)
        {
            KwOpening opening = o.Opening;
            KwOpeningSurface? wall = opening.Surface;
            if (opening.IsDoorway || wall == null || o.Piece < 0 || o.Piece >= blueprint.Pieces.Count)
                return null;
            Vector2 middle = wall.Fit(opening, new Vector2(opening.U, opening.Sill + opening.Height / 2f));
            KwGunport port = new KwGunport(blueprint.Pieces[o.Piece], wall, middle, o.Lowest);
            return port.Upright ? port : null;
        }

        // The ports within reach across the deck whose head the aim is not far over.
        private static List<KwGunport> Near(List<KwGunport> all, Vector3 at)
        {
            near.Clear();
            foreach (KwGunport port in all)
            {
                if (Across(port.At, at) <= Reach && at.y <= port.Top + Above)
                    near.Add(port);
            }
            return near;
        }

        private static float Across(Vector3 a, Vector3 b) => new Vector2(a.x - b.x, a.z - b.z).magnitude;

        // The first of the ship's own faces under a point of its frame, going down from a height: its height in the ship's
        // frame; null for none within reach. Looked for where the physics has the ship (its colliders are there).
        private static float? Deck(KwShipRiders ship, Vector3 spot, float from, Transform? ghost)
        {
            mask = mask != 0 ? mask : LayerMask.GetMask("Default", "static_solid", "Default_small", "piece", "terrain", "vehicle");
            (Vector3 at, Quaternion turn) = ship.Pose;
            Vector3 start = at + turn * new Vector3(spot.x, from, spot.z);
            int count = Physics.RaycastNonAlloc(start, turn * Vector3.down, hits, Drop + from - spot.y, mask, QueryTriggerInteraction.Ignore);
            Array.Sort(hits, 0, count, Nearer);
            for (int i = 0; i < count; i++)
            {
                if (Own(hits[i].collider, ship, ghost))
                    return (Quaternion.Inverse(turn) * (hits[i].point - at)).y;
            }
            return null;
        }

        // Whether a collider is the ship's own (its hull, decks, cabins, fittings), not a piece riding it nor the ghost.
        private static bool Own(Collider? collider, KwShipRiders ship, Transform? ghost)
        {
            Rigidbody? body = collider != null ? collider.attachedRigidbody : null;
            if (body == null || body.transform != ship.transform || ghost != null && collider!.transform.IsChildOf(ghost))
                return false;
            return collider!.GetComponentInParent<KwShipRider>() == null;
        }
    }
}
