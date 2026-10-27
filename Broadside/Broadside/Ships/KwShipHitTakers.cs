using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// The router's own takers (<see cref="KwShipHitRouter"/>), on the ship's owner:
    /// <list type="bullet">
    /// <item>A wall (the hull's planking and transoms, a deck, a cabin's sides, walls and roof): a shot with more energy
    /// than the wall takes (<see cref="KwShipHitEnergy.Wall"/>) goes through, holing it there (<see cref="KwHullHoles.Pierce"/>,
    /// an exit wider than an entry); one with less stops in it, its blow worn into the part as before
    /// (<see cref="KwHullHoles.Take"/>: the whole blow at the entry, what it had left in a far wall). The ship's health
    /// takes the whole shot at its entry, <see cref="ExitShare"/> of it out of the far side, <see cref="DeckShare"/> through a
    /// deck inside, <see cref="StopShare"/> stopped in a far wall.</item>
    /// <item>A fitting: <see cref="FittingTakes"/> of the energy and <see cref="FittingShare"/> of the health.</item>
    /// <item>A piece built aboard (a rider): its own wear hit by the game's way, the shot's structure blow by its share
    /// left; it takes <see cref="RiderTakes"/> and a third of the rest.</item>
    /// <item>A player or creature aboard: hit the game's way with the shot's blow on a body, by its share left, when the
    /// shooter may hurt it (the game's projectile rules); a body barely slows the shot.</item>
    /// <item>A railing: <see cref="KwShipRails.Take"/>.</item>
    /// </list>
    /// </summary>
    public static class KwShipHitTakers
    {
        public const float ExitShare = 0.6f, DeckShare = 0.3f, StopShare = 0.4f, FittingShare = 0.1f;
        public const float FittingTakes = 15f, RiderTakes = 20f, BodyTakes = 8f;

        public static void Install()
        {
            foreach (string wall in new[] { KwShipHitRouter.Hull, KwShipHitRouter.Deck, KwShipHitRouter.Cabin })
                KwShipHitRouter.Register(wall, Wall);
            KwShipHitRouter.Register(KwShipHitRouter.Fitting, Fitting);
            KwShipHitRouter.Register(KwShipHitRouter.Rider, Rider);
            KwShipHitRouter.Register(KwShipHitRouter.Character, Body);
            KwShipHitRouter.Register(KwShipHitRouter.Rail, KwShipRails.Take);
        }

        // Through a wall with a hole, or stopped in it; its slant from the drawn face there (a side's box is a straight
        // chord inside a curved hull, so its own face can lie at another slant).
        private static float Wall(KwShipHitRouter ship, KwShipHit hit)
        {
            Vector3 facing = Facing(hit);
            KwShipWallHit wall = WallHit(hit.Kind, Tier(ship, hit.Piece), ship.Face(hit.Point, facing), hit.Direction, hit.Energy, hit.Damage, hit.Entry);
            if (wall.Through)
                ship.Holes?.Pierce(hit.Point, facing, wall.Radius);
            else
                ship.Holes?.Take(hit.Point, facing, wall.Wear, ship.Wear != null ? ship.Wear.m_health : 1000f, true);
            ship.Health(hit, wall.Share);
            return wall.Takes;
        }

        /// <summary>The way the hole's part is looked up by: the struck face's own (as the trace classed it), else the flight.</summary>
        public static Vector3 Facing(KwShipHit hit) => hit.Normal.sqrMagnitude > 0.5f ? hit.Normal : hit.Direction;

        /// <summary>
        /// What a wall of a kind and tier does to a shot striking it (its surface's normal, the flight, the energy left, the
        /// shot's structure damage, whether it is the first thing struck): through it, with the hole's radius, or stopped
        /// in it, with the blow worn into it; the energy it takes and the ship's health share.
        /// </summary>
        public static KwShipWallHit WallHit(string kind, float tier, Vector3 normal, Vector3 way, float energy, float damage, bool entry)
        {
            float takes = KwShipHitEnergy.Wall(kind, tier, normal, way);
            if (energy > takes)
                return new KwShipWallHit(true, takes, entry ? 1f : kind == KwShipHitRouter.Deck ? DeckShare : ExitShare,
                    KwHullDamage.Through(damage, !entry && kind != KwShipHitRouter.Deck), 0f);
            return new KwShipWallHit(false, energy, entry ? 1f : StopShare, 0f, entry ? damage : energy);
        }

        private static float Fitting(KwShipHitRouter ship, KwShipHit hit)
        {
            ship.Health(hit, FittingShare);
            return Takes(KwShipHitRouter.Fitting, hit.Energy);
        }

        /// <summary>What a fitting, a piece built aboard or a body takes from the energy it meets.</summary>
        public static float Takes(string kind, float energy) => Mathf.Min(energy, kind switch
        {
            KwShipHitRouter.Fitting => FittingTakes,
            KwShipHitRouter.Rider => RiderTakes + energy / 3f,
            _ => BodyTakes + energy / 10f,
        });

        // A piece built aboard: its own wear hit, the game's way, to its owner.
        private static float Rider(KwShipHitRouter ship, KwShipHit hit)
        {
            WearNTear? wear = hit.Collider != null ? hit.Collider.GetComponentInParent<WearNTear>() : null;
            if (wear != null && wear != ship.Wear)
                wear.Damage(Blow(hit, hit.Share));
            return Takes(KwShipHitRouter.Rider, hit.Energy);
        }

        // A player or creature aboard, hit the game's way, to its owner, when the shooter may hurt it.
        private static float Body(KwShipHitRouter ship, KwShipHit hit)
        {
            Character? body = hit.Collider != null ? hit.Collider.GetComponentInParent<Character>() : null;
            if (body != null && Hurts(Shooter(hit.Attacker), body))
                body.Damage(Blow(hit, ship.Plain * hit.Share));
            return Takes(KwShipHitRouter.Character, hit.Energy);
        }

        // The shot's blow at a thing on its path: its damage and push by a share, at the point, along the flight.
        private static HitData Blow(KwShipHit hit, float share)
        {
            HitData blow = hit.Shot.Clone();
            blow.m_damage.Modify(share);
            blow.m_pushForce *= share;
            (blow.m_point, blow.m_dir, blow.m_hitCollider) = (hit.Point, hit.Direction, hit.Collider);
            return blow;
        }

        // As the game's projectile decides whom it may hit (Projectile.IsValidTarget): with no shooter known here, no player.
        private static bool Hurts(Character? shooter, Character body)
        {
            if (shooter == null)
                return !body.IsPlayer();
            if (shooter == body)
                return false;
            bool enemy = BaseAI.IsEnemy(shooter, body) || (body.GetBaseAI() != null && body.GetBaseAI().IsAggravatable() && shooter.IsPlayer());
            return enemy || (shooter.IsPlayer() && shooter.IsPVPEnabled());
        }

        private static Character? Shooter(ZDOID attacker)
        {
            GameObject? found = attacker != ZDOID.None && ZNetScene.instance != null ? ZNetScene.instance.FindInstance(attacker) : null;
            return found != null ? found.GetComponent<Character>() : null;
        }

        /// <summary>A blueprint piece's biome tier, 0 (Meadows) to 6 (Ashlands); 0 with none.</summary>
        public static float Tier(KwShipHitRouter ship, int piece)
        {
            if (ship.Blueprint == null || piece < 0 || piece >= ship.Blueprint.Pieces.Count)
                return 0f;
            List<KwBiomeTier> tiers = new List<KwBiomeTier>(KwBiomeTiers.All);
            return Mathf.Max(0, tiers.IndexOf(KwBiomeTiers.Of(ship.Blueprint.Pieces[piece].Data)));
        }
    }

    /// <summary>What a wall did to a shot (<see cref="KwShipHitTakers.WallHit"/>).</summary>
    public readonly struct KwShipWallHit
    {
        public KwShipWallHit(bool through, float takes, float share, float radius, float wear) =>
            (Through, Takes, Share, Radius, Wear) = (through, takes, share, radius, wear);

        /// <summary>Whether the shot went through (else it stopped in the wall).</summary>
        public bool Through { get; }

        /// <summary>The energy the wall took.</summary>
        public float Takes { get; }

        /// <summary>The ship's health share of the shot's damage.</summary>
        public float Share { get; }

        /// <summary>Through: the hole's radius.</summary>
        public float Radius { get; }

        /// <summary>Stopped: the blow worn into the part (the old rule's damage).</summary>
        public float Wear { get; }
    }
}
