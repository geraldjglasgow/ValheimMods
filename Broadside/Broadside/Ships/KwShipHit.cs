using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// One thing a heavy shot meets on its way through a launched ship (<see cref="KwShipHitRouter"/>), handed to that
    /// kind's taker on the ship's owner: where and which way, what the shot was worth and what it has left, what it struck.
    /// </summary>
    public sealed class KwShipHit
    {
        internal KwShipHit(KwShipHitMeet meet, Vector3 direction, float damage, float energy, bool entry, HitData shot)
        {
            (Point, Normal, Collider, Kind, Piece, Part) = (meet.Point, meet.Normal, meet.Collider, meet.Kind, meet.Piece, meet.Part);
            (Direction, Damage, Energy, Entry, Shot, Attacker) = (direction, damage, energy, entry, shot, shot.m_attacker);
        }

        /// <summary>Where the shot meets it (world).</summary>
        public Vector3 Point { get; }

        /// <summary>The flight (world, unit).</summary>
        public Vector3 Direction { get; }

        /// <summary>The struck surface's normal there (world), as the ray found it, facing the shot.</summary>
        public Vector3 Normal { get; }

        /// <summary>The whole shot's structure damage after the ship's resistances.</summary>
        public float Damage { get; }

        /// <summary>What the shot has left as it meets this, in the same units as <see cref="Damage"/>.</summary>
        public float Energy { get; }

        /// <summary>The share of the shot's full blow still in it, 0 to 1.</summary>
        public float Share => Damage > 0f ? Mathf.Clamp01(Energy / Damage) : 0f;

        public Collider? Collider { get; }

        /// <summary>The kind it is (<see cref="KwShipHitRouter.Hull"/> and the rest).</summary>
        public string Kind { get; }

        /// <summary>Its piece's index in the ship's blueprint, or -1.</summary>
        public int Piece { get; }

        /// <summary>A part of the piece its tag names (a railing's stretch, a mast's sail), or -1.</summary>
        public int Part { get; }

        /// <summary>Whether it is the first thing the shot meets on this ship.</summary>
        public bool Entry { get; }

        public ZDOID Attacker { get; }

        /// <summary>The shot's own hit as the shooter sent it (its damage before the ship's resistances); a copy.</summary>
        public HitData Shot { get; }
    }

    /// <summary>
    /// What a collider of a launched ship is for its shots (<see cref="KwShipHitRouter"/>): its kind, its piece's index in
    /// the ship's blueprint and a part of that piece. Found from the collider up through its parents, so one on a holder
    /// classes every collider under it.
    /// </summary>
    public sealed class KwShipHitPart : MonoBehaviour
    {
        public string Kind = KwShipHitRouter.Fitting;
        public int Piece = -1;
        public int Part = -1;
    }

    /// <summary>One thing on a shot's path through a ship, as the trace found it (<see cref="KwShipHitTrace"/>): world point and normal.</summary>
    public readonly struct KwShipHitMeet
    {
        public KwShipHitMeet(Collider? collider, Vector3 point, Vector3 normal, float distance, string kind, int piece, int part, Object? owner)
        {
            (Collider, Point, Normal, Distance) = (collider, point, normal, distance);
            (Kind, Piece, Part, Owner) = (kind, piece, part, owner);
        }

        public Collider? Collider { get; }
        public Vector3 Point { get; }
        public Vector3 Normal { get; }

        /// <summary>How far along the trace from its start.</summary>
        public float Distance { get; }
        public string Kind { get; }
        public int Piece { get; }
        public int Part { get; }

        /// <summary>What it belongs to (a rider's piece, a character, the ship), so its colliders count once.</summary>
        public Object? Owner { get; }

        /// <summary>Whether this and another are the same thing struck.</summary>
        public bool Same(KwShipHitMeet other) => Kind == other.Kind && Piece == other.Piece && Part == other.Part && Owner == other.Owner;
    }
}
