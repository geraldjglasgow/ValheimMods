using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// The checks on a planned hammock (<see cref="KwHammockPlan"/>), on the placing player's game, in the world as the physics
    /// has it (aboard, by the ship's body): the cloth's lowest line at least <see cref="KwHammockRules.Clear"/> over the floor
    /// under it (five points along it) and its middle at most <see cref="KwHammockRules.MostOver"/>; nothing solid in its way
    /// (a box from just under the cloth to <see cref="Room"/> over the bars, the length of the cloth, where a sleeper lies; each
    /// rope from a little below its hook to its bar; each post from just over its foot to its top), the thing in the way
    /// named; and the game's own building rules at both ends and the origin: not in a no-build place
    /// (<c>Location.IsInsideNoBuildLocation</c>), every ward there one the player may use (<c>PrivateArea.CheckAccess</c>,
    /// flashing on the click only). The workbench in range and the cost are the game's own checks before the click.
    /// </summary>
    public static class KwHammockChecks
    {
        private const float Below = 6f, Room = 0.4f, RopeRoom = 0.02f, RopeFree = 0.12f, PostRoom = 0.05f, Lift = 0.02f;
        private const int Samples = 5;
        private static readonly Collider[] touching = new Collider[32];

        /// <summary>The floor under a point: the first solid thing, or the water, straight down within a few metres.</summary>
        public static bool Floor(Vector3 from, Vector3 up, out Vector3 floor)
        {
            bool found = Physics.Raycast(from, -up, out RaycastHit hit, Below, KwHammockAim.Aimed, QueryTriggerInteraction.Ignore);
            floor = found ? hit.point : from;
            return found;
        }

        public static void Run(KwHammockPlan plan, bool flash)
        {
            KwHammockSling sling = new KwHammockSling(plan.Spec!);
            Clearance(plan, sling);
            if (plan.Why == null)
                InTheWay(plan, sling);
            if (plan.Why == null)
                Places(plan, flash);
        }

        // The cloth's middle line over whatever is under it; its middle (the origin's floor) not too far over it either.
        private static void Clearance(KwHammockPlan plan, KwHammockSling sling)
        {
            Vector3 up = plan.Turn * Vector3.up;
            float least = float.MaxValue;
            for (int i = 0; i < Samples; i++)
            {
                Vector3 cloth = plan.World(sling.ClothPoint(sling.Length * 0.9f * ((float)i / (Samples - 1) - 0.5f), 0f));
                if (Physics.Raycast(cloth + up * Lift, -up, out RaycastHit hit, Below, KwHammockAim.Aimed, QueryTriggerInteraction.Ignore))
                    least = Mathf.Min(least, hit.distance - Lift);
            }
            float over = plan.Spec!.Bars - KwHammockShape.Sag;
            if (least < KwHammockRules.Clear)
                plan.Refuse(KwHammockWords.TooLow(least));
            else if (over > KwHammockRules.MostOver)
                plan.Refuse(KwHammockWords.TooHigh(over));
        }

        // Where a sleeper lies, the ropes and the posts clear of anything solid.
        private static void InTheWay(KwHammockPlan plan, KwHammockSling sling)
        {
            KwHammockSpec spec = plan.Spec!;
            float low = spec.Bars - KwHammockShape.Sag - 0.02f, high = spec.Bars + Room;
            Vector3 centre = plan.World(new Vector3(0f, 0f, (low + high) * 0.5f));
            Vector3 half = new Vector3(KwHammockShape.Width * 0.5f, (high - low) * 0.5f, sling.Length * 0.5f - 0.05f);
            string? what = Name(Physics.OverlapBoxNonAlloc(centre, half, touching, plan.Turn, KwHammockAim.Solid, QueryTriggerInteraction.Ignore));
            what ??= Ropes(plan, sling) ?? Post(plan, spec.A) ?? Post(plan, spec.B);
            if (what != null)
                plan.Refuse(KwHammockWords.InTheWay(what));
        }

        private static string? Ropes(KwHammockPlan plan, KwHammockSling sling)
        {
            foreach ((Vector3 from, Vector3 to) in sling.Ropes())
            {
                Vector3 a = plan.World(from), b = plan.World(to);
                string? what = Name(Physics.OverlapCapsuleNonAlloc(a + (b - a).normalized * RopeFree, b, RopeRoom, touching, KwHammockAim.Solid, QueryTriggerInteraction.Ignore));
                if (what != null)
                    return what;
            }
            return null;
        }

        private static string? Post(KwHammockPlan plan, KwHammockMount end)
        {
            if (!end.Post)
                return null;
            Vector3 foot = plan.World(end.Base + new Vector3(0f, 0f, 0.15f)), top = plan.World(end.Base + new Vector3(0f, 0f, end.Peg + KwHammockParts.Cap));
            return Name(Physics.OverlapCapsuleNonAlloc(foot, top, PostRoom, touching, KwHammockAim.Solid, QueryTriggerInteraction.Ignore));
        }

        // What the first thing touched is called ("Wood wall", "The ground"); null with none.
        private static string? Name(int count)
        {
            for (int i = 0; i < count; i++)
            {
                Collider? collider = touching[i];
                if (collider == null)
                    continue;
                Piece? piece = collider.GetComponentInParent<Piece>();
                if (piece != null && Localization.instance != null)
                    return Localization.instance.Localize(piece.m_name);
                return collider.GetComponent<Heightmap>() != null ? "The ground" : "Something";
            }
            return null;
        }

        // The game's building rules at both ends and the origin.
        private static void Places(KwHammockPlan plan, bool flash)
        {
            KwHammockSpec spec = plan.Spec!;
            foreach (Vector3 point in new[] { plan.World(spec.A.Base), plan.World(spec.B.Base), plan.At })
            {
                if (Location.IsInsideNoBuildLocation(point))
                    plan.Refuse("$msg_nobuildzone");
                else if (!PrivateArea.CheckAccess(point, 0f, flash, false))
                    plan.Refuse("$msg_privatezone");
            }
        }
    }
}
