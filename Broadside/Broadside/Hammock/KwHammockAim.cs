using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// One end of a hammock as aimed at or clicked (<see cref="KwHammockAim.Click"/>): a post or a hook, on a launched ship
    /// or not, and the point and the way its surface faces, kept in the ship's own frame (its body's, where its colliders
    /// are) so a first click on a sailing or rocking ship stays on the same spot of it until the second.
    /// </summary>
    public sealed class KwHammockClick
    {
        /// <summary>A free-standing post (a floor, a deck or the ground aimed at); else an iron hook.</summary>
        public bool Post { get; set; }

        /// <summary>The launched ship it is on, or null; <see cref="Aboard"/> stays true when that ship goes.</summary>
        public KwShipRiders? Ship { get; set; }
        public bool Aboard { get; set; }

        /// <summary>The point and its surface's way out, in the ship's body frame (the world's off a ship).</summary>
        public Vector3 Local { get; set; }
        public Vector3 LocalFace { get; set; }

        /// <summary>Why an end may not go there; null when it may.</summary>
        public string? Why { get; set; }

        /// <summary>Its ship gone since it was clicked.</summary>
        public bool Lost => Aboard && (Ship == null || Ship.Zdo == null);

        /// <summary>Up where it is: the ship's up aboard, the world's on land.</summary>
        public Vector3 Up => Ship != null ? Ship.Pose.Turn * Vector3.up : Vector3.up;

        /// <summary>The point and its surface's way in the world now, by the ship's body.</summary>
        public Vector3 At => Ship != null ? Ship.Pose.At + Ship.Pose.Turn * Local : Local;
        public Vector3 Face => Ship != null ? Ship.Pose.Turn * LocalFace : LocalFace;

        public string Says => Post ? "a post" : "an iron hook";

        /// <summary>What it is, to the centimetre: a still aim keeps the plan made for it.</summary>
        public string Key => $"{(Post ? "P" : "H")}{(Ship != null ? Ship.Id : 0L)}|{Local.x:F2},{Local.y:F2},{Local.z:F2}|{LocalFace.x:F2},{LocalFace.y:F2},{LocalFace.z:F2}|{Why}";
    }

    /// <summary>
    /// What the hammock's aim means, on the placing player's game: the first solid thing (or the water) along the camera's
    /// aim within the game's building reach (the piece's own extra reach included). Its face looking up (at least
    /// <see cref="KwHammockRules.Walkable"/> of the world's up, a ship's aboard), or the ground: a post stands there. Any
    /// other face (a wall, beam, ceiling, post, mast, cabin side, rock): an iron hook is fixed into it. Never on the water or
    /// on anything else that moves (a cart, the game's own boats); a launched Broadside ship is taken (its hull, decks,
    /// cabins, masts and the pieces riding it), the point kept in its frame.
    /// </summary>
    public static class KwHammockAim
    {
        private const float Reach = 50f;

        /// <summary>The solid things a hammock's ends go on and its checks see.</summary>
        public static readonly int Solid = LayerMask.GetMask("Default", "static_solid", "Default_small", "piece", "terrain", "vehicle");

        /// <summary>The same and the water, which the aim stops at.</summary>
        public static readonly int Aimed = Solid | LayerMask.GetMask("Water");

        private static readonly int Water = LayerMask.NameToLayer("Water"), Terrain = LayerMask.NameToLayer("terrain");
        private static readonly RaycastHit[] hits = new RaycastHit[16];

        /// <summary>The end the camera's aim gives now; null with nothing aimed at.</summary>
        public static KwHammockClick? Click(Player player)
        {
            if (GameCamera.instance == null || !First(GameCamera.instance.transform, out RaycastHit hit))
                return null;
            Rigidbody? body = hit.collider.attachedRigidbody;
            KwShipRiders? ship = body != null ? body.GetComponent<KwShipRiders>() : null;
            KwHammockClick click = Make(ship, hit);
            if (hit.collider.gameObject.layer == Water)
                click.Why = KwHammockWords.OnWater;
            else if (body != null && ship == null)
                click.Why = KwHammockWords.Moving;
            else if (Vector3.Distance(player.m_eye.position, hit.point) > Range(player))
                click.Why = KwHammockWords.TooFarAway;
            return click;
        }

        // The nearest hit along the aim that is solid or the water's surface (another trigger, a heat or ward area, passed through).
        private static bool First(Transform eye, out RaycastHit first)
        {
            first = default;
            int count = Physics.RaycastNonAlloc(eye.position, eye.forward, hits, Reach, Aimed, QueryTriggerInteraction.Collide);
            float best = float.MaxValue;
            for (int i = 0; i < count; i++)
            {
                Collider? collider = hits[i].collider;
                if (collider != null && hits[i].distance < best && (!collider.isTrigger || collider.gameObject.layer == Water))
                    (first, best) = (hits[i], hits[i].distance);
            }
            return best < float.MaxValue;
        }

        // The end at a hit, kept in the ship's body frame aboard; a post where the face looks up or on the ground.
        private static KwHammockClick Make(KwShipRiders? ship, RaycastHit hit)
        {
            (Vector3 at, Quaternion turn) = ship != null ? ship.Pose : (Vector3.zero, Quaternion.identity);
            Quaternion back = Quaternion.Inverse(turn);
            bool post = hit.collider.gameObject.layer == Terrain || Vector3.Dot(hit.normal, turn * Vector3.up) >= KwHammockRules.Walkable;
            return new KwHammockClick { Post = post, Ship = ship, Aboard = ship != null, Local = back * (hit.point - at), LocalFace = back * hit.normal };
        }

        // The game's building reach for the hammock.
        private static float Range(Player player)
        {
            Piece? piece = player.GetSelectedPiece();
            return player.m_maxPlaceDistance + (piece != null ? piece.m_extraPlacementDistance : 0);
        }

        /// <summary>A pose by a ship's body (where its colliders are) carried to where the ship is drawn; the same off a ship.</summary>
        public static (Vector3 At, Quaternion Turn) Drawn(KwShipRiders? ship, Vector3 at, Quaternion turn)
        {
            if (ship == null)
                return (at, turn);
            (Vector3 bodyAt, Quaternion bodyTurn) = ship.Pose;
            Transform drawn = ship.transform;
            return (KwShipRiderPose.Carry(bodyAt, bodyTurn, drawn.position, drawn.rotation, at), drawn.rotation * (Quaternion.Inverse(bodyTurn) * turn));
        }
    }
}
