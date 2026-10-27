using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// What a ship cannon hauled about a launched ship's deck (<see cref="KwDeckHaul"/>) stands on and runs into, from the
    /// ship's own colliders (its body's: the hull's, decks', railings', cabins', masts' and spars' hit shapes, hatches',
    /// ladders', the helm's, and every other piece riding it), where its body's physics has them, all in the ship's frame,
    /// never the cannon's own. Under each truck the deck is the highest walkable face within a step of where that truck
    /// stands now (<see cref="Height"/>); none there (an opening, a hatch, the deck's edge, a step too far down) and it may
    /// not go there. In its way (<see cref="Blocked"/>) is anything solid in the carriage's room (from a step over the deck
    /// to over its breech, out to its trucks' outer faces, from its rear to its front trucks) or the barrel's (ahead of the
    /// brackets out to the muzzle): what it stands in already never holds it (<see cref="Settle"/>, per room: a cannon built
    /// with its barrel over a railing is hauled back out of it, but its carriage never into it).
    /// </summary>
    public sealed class KwDeckFloor
    {
        // How far over and under a truck's foot the deck is looked for; the least upward face a truck stands on; how far
        // ahead of and behind its foot a truck's tread reaches, and to each side.
        private const float Above = 0.5f, Below = 0.5f, Walkable = 0.7f, Span = 0.06f, Wide = 0.05f;

        private static int floors, walls;
        private readonly Transform ship, cannon;
        private readonly Rigidbody? body;
        private readonly (Vector3 Centre, Vector3 Half)[] rooms;
        private readonly HashSet<Collider>[] inside;
        private readonly RaycastHit[] hits = new RaycastHit[24];
        private readonly Collider[] found = new Collider[64];

        public KwDeckFloor(Transform ship, Transform cannon, KwShipSpec spec, KwArmDef def)
        {
            (this.ship, this.cannon, body) = (ship, cannon, ship.GetComponent<Rigidbody>());
            rooms = Rooms(spec, def);
            inside = new[] { new HashSet<Collider>(), new HashSet<Collider>() };
            floors = floors != 0 ? floors : LayerMask.GetMask("Default", "static_solid", "Default_small", "piece", "vehicle");
            walls = walls != 0 ? walls : floors | LayerMask.GetMask("piece_nonsolid", "hitbox");
        }

        /// <summary>The ship as its body's physics has it now (where its colliders are; it is drawn a little behind them).</summary>
        public (Vector3 At, Quaternion Turn) Pose => body != null ? (body.position, body.rotation) : (ship.position, ship.rotation);

        /// <summary>
        /// The carriage's room and the barrel's, in the cannon's own axes (Unity's): each a box's centre and half size. The
        /// carriage's from a step over the deck (<see cref="KwDeckMath.Climb"/>) to over the breech, out to the trucks'
        /// outer faces, from its rear (or rear trucks) to its front trucks; the barrel's round it, ahead of the brackets.
        /// </summary>
        public static (Vector3 Centre, Vector3 Half)[] Rooms(KwShipSpec spec, KwArmDef def)
        {
            float rear = Mathf.Min(spec.BracketRear, spec.RearAxle - spec.Rear), front = Mathf.Max(spec.BracketFront, spec.FrontAxle + spec.Front);
            float low = KwDeckMath.Climb, high = spec.Height + spec.Under.y + 0.02f, muzzle = spec.TrunnionX + def.Muzzle.x, r = spec.Under.y * 0.85f;
            return new[]
            {
                Room(new Vector3(rear, -spec.TruckOut, low), new Vector3(front, spec.TruckOut, high)),
                Room(new Vector3(spec.BracketFront, -r, spec.Height - r), new Vector3(muzzle, r, spec.Height + r)),
            };
        }

        // A box between two corners in kit axes, as a centre and half size in Unity's.
        private static (Vector3, Vector3) Room(Vector3 a, Vector3 b)
        {
            Vector3 centre = KwMesh.ToUnity((a + b) / 2f), size = KwMesh.ToUnity(b - a);
            return (centre, new Vector3(Mathf.Abs(size.x), Mathf.Abs(size.y), Mathf.Abs(size.z)) / 2f);
        }

        /// <summary>
        /// The deck's height under a truck's foot (the ship's frame) that stands at <paramref name="now"/>: the highest
        /// walkable face of the ship's within <see cref="KwDeckMath.Climb"/> over it; false when there is none within as
        /// much under it either. Looked for under the foot, then (a ray slips down the seams between a deck's slabs, a few
        /// centimetres wide where one is laid a little askew) along and across its tread: a truck rolling the way
        /// <paramref name="ahead"/> spans a seam, standing on more than a point.
        /// </summary>
        public bool Height(Vector3 foot, Vector3 ahead, float now, out float height)
        {
            height = Probe(foot, now);
            Vector3 along = ahead * Span, across = KwCartMath.Side(ahead) * Wide;
            for (int i = 0; i < Tread.Length && height < now - KwDeckMath.Climb; i++)
                height = Mathf.Max(height, Probe(foot + along * Tread[i].x + across * Tread[i].y, now));
            return height >= now - KwDeckMath.Climb;
        }

        // A truck's tread round its foot: ahead of and behind it, to each side.
        private static readonly Vector2[] Tread = { new Vector2(1f, 0f), new Vector2(-1f, 0f), new Vector2(0f, 1f), new Vector2(0f, -1f) };

        // The highest walkable face of the ship's straight under a point, no more than a step over where it stands now;
        // float.MinValue for none.
        private float Probe(Vector3 foot, float now)
        {
            (Vector3 shipAt, Quaternion shipTurn) = Pose;
            Vector3 up = shipTurn * Vector3.up, from = shipAt + shipTurn * new Vector3(foot.x, now + Above, foot.z);
            int count = Physics.RaycastNonAlloc(from, -up, hits, Above + Below, floors, QueryTriggerInteraction.Ignore);
            float height = float.MinValue;
            for (int i = 0; i < count; i++)
            {
                RaycastHit hit = hits[i];
                float y = now + Above - hit.distance;
                if (y > height && y <= now + KwDeckMath.Climb && Vector3.Dot(hit.normal, up) >= Walkable && Ours(hit.collider))
                    height = y;
            }
            return height;
        }

        /// <summary>Whether something of the ship's stands in the carriage's or the barrel's room with the cannon at a pose (its origin and rotation in the ship's frame), but what it stands in already.</summary>
        public bool Blocked(Vector3 at, Quaternion turn)
        {
            for (int room = 0; room < rooms.Length; room++)
            {
                int count = Overlap(room, at, turn);
                for (int i = 0; i < count; i++)
                {
                    if (Ours(found[i]) && !inside[room].Contains(found[i]))
                        return true;
                }
            }
            return false;
        }

        /// <summary>What the cannon stands in at a pose (where it is now), room by room: none of it holds it.</summary>
        public void Settle(Vector3 at, Quaternion turn)
        {
            for (int room = 0; room < rooms.Length; room++)
            {
                inside[room].Clear();
                int count = Overlap(room, at, turn);
                for (int i = 0; i < count; i++)
                {
                    if (Ours(found[i]))
                        inside[room].Add(found[i]);
                }
            }
        }

        // The ship's colliders in a room with the cannon at a pose.
        private int Overlap(int room, Vector3 at, Quaternion turn)
        {
            (Vector3 shipAt, Quaternion shipTurn) = Pose;
            (Vector3 centre, Vector3 half) = rooms[room];
            Vector3 world = shipAt + shipTurn * (at + turn * centre);
            return Physics.OverlapBoxNonAlloc(world, half, found, shipTurn * turn, walls, QueryTriggerInteraction.Ignore);
        }

        // One of the ship's own colliders (its body's, or under it), not the cannon's.
        private bool Ours(Collider? collider)
        {
            if (collider == null || collider.transform.IsChildOf(cannon))
                return false;
            return body != null ? collider.attachedRigidbody == body : collider.transform.IsChildOf(ship);
        }
    }
}
