using UnityEngine;

namespace Broadside
{
    /// <summary>Where a dock would start (<see cref="KwPierAim.Start"/>): on the ground, or on a dock it continues or branches off.</summary>
    public sealed class KwPierStart
    {
        /// <summary>The start of the new dock's centre line, at its deck's top.</summary>
        public Vector3 At { get; set; }

        /// <summary>The way it must run: straight on from a dock's end, square off its side; null on the ground (it runs to the end click).</summary>
        public Vector3? Way { get; set; }

        /// <summary>The dock section it meets (null on the ground); <see cref="OnDock"/> stays true if that one is removed meanwhile.</summary>
        public KwPier? Joins { get; set; }
        public bool OnDock { get; set; }

        /// <summary>How the player reads it ("from the shore", "continuing this dock", "branching off this dock"), and why it may not start there (null when it may).</summary>
        public string Says { get; set; } = "";
        public string? Why { get; set; }
    }

    /// <summary>
    /// What the Dock tool's aim means, on the placing player's game, along the game's own placement ray with the water
    /// (the camera's, 50 m): a start on the ground or the shore (its deck 0.1 m over the ground there, within the game's
    /// building reach and at least <see cref="MinOverWater"/> over the sea); on a dock section within
    /// <see cref="EndZone"/> of an end no other section continues, straight on from that end at its deck's height; anywhere
    /// else on a dock, square off the side aimed at, from the deck's edge at a half-metre along it. Never on the water, a
    /// ship or another piece. The end is where the aim meets the water or the ground (or, aimed over everything, the deck's
    /// height); never a ship. Docks it runs into on the way are the plan's (<see cref="KwPierMeet"/>).
    /// </summary>
    public static class KwPierAim
    {
        public const float EndZone = 1.5f, Lift = 0.1f, MinOverWater = 0.5f, Reach = 50f;
        private static int water = -1;

        /// <summary>The first thing the camera's aim meets within reach, the water included.</summary>
        public static RaycastHit? Hit(Player player)
        {
            Transform eye = GameCamera.instance.transform;
            return Physics.Raycast(eye.position, eye.forward, out RaycastHit hit, Reach, player.m_placeWaterRayMask) ? hit : (RaycastHit?)null;
        }

        public static bool IsWater(Collider collider)
        {
            if (water < 0)
                water = LayerMask.NameToLayer("Water");
            return collider.gameObject.layer == water;
        }

        /// <summary>Where a dock would start from what is aimed at; null with nothing aimed at.</summary>
        public static KwPierStart? Start(Player player)
        {
            RaycastHit? aim = Hit(player);
            if (aim == null)
                return null;
            RaycastHit hit = aim.Value;
            if (hit.collider.attachedRigidbody != null || KwShipRiderAim.Ship != null)
                return Refused(hit.point, "Docks are built on the shore, not on a ship");
            if (IsWater(hit.collider))
                return Refused(hit.point, "Start the dock on the shore or on a dock, then click the water where it ends");
            KwPier? pier = hit.collider.GetComponentInParent<KwPier>();
            KwPierStart? start = pier != null ? KwPierJoin.Of(pier, hit.point) : hit.collider.GetComponentInParent<Piece>() != null
                ? Refused(hit.point, "Start the dock on the ground, the shore or another dock") : Ground(hit.point);
            if (start != null && start.Why == null && Vector3.Distance(player.m_eye.position, hit.point) > Range(player))
                start.Why = "Too far away to start a dock";
            return start;
        }

        // On the ground: the deck a little over it, high enough over the sea.
        private static KwPierStart Ground(Vector3 point)
        {
            KwPierStart start = new KwPierStart { At = point + Vector3.up * Lift, Says = "from the shore" };
            float over = start.At.y - Sea;
            if (over < MinOverWater)
                start.Why = $"Too low: the deck would be {over:0.0} m over the sea ({MinOverWater:0.0} m at least)";
            return start;
        }

        private static KwPierStart Refused(Vector3 point, string why) => new KwPierStart { At = point, Why = why };

        /// <summary>The sea's level.</summary>
        public static float Sea => ZoneSystem.instance != null ? ZoneSystem.instance.m_waterLevel : 30f;

        // The game's building reach for the tool (its own extra included).
        private static float Range(Player player)
        {
            Piece? tool = player.GetSelectedPiece();
            return player.m_maxPlaceDistance + (tool != null ? tool.m_extraPlacementDistance : 0);
        }

        /// <summary>Where the dock would end: the aim on the water, the ground or a dock, else the deck's height under it; null (and why) for none or a ship.</summary>
        public static Vector3? End(Player player, KwPierStart start, out string? why)
        {
            why = null;
            RaycastHit? aim = Hit(player);
            if (aim != null && (aim.Value.collider.attachedRigidbody != null || KwShipRiderAim.Ship != null))
                why = "Aim at the water or the ground, not a ship";
            else if (aim != null)
                return aim.Value.point;
            Transform eye = GameCamera.instance.transform;
            float drop = eye.position.y - start.At.y;
            if (why == null && eye.forward.y < -0.01f && drop > 0f)
                return eye.position + eye.forward * (drop / -eye.forward.y);
            why ??= "Aim at the water or the ground where the dock should end";
            return null;
        }
    }
}
