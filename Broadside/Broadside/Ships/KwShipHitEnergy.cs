using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// The numbers of a heavy shot through a launched ship (<see cref="KwShipHitRouter"/>), plain arithmetic.
    /// <list type="bullet">
    /// <item>Heavy: a projectile of at least <see cref="HeavyBlunt"/> blunt (a cannonball, a catapult's stone) or
    /// <see cref="HeavyPierce"/> pierce (a siege bolt); an arrow or a crossbow bolt never is.</item>
    /// <item>Its energy, in its structure damage's units: the damage after the ship's resistances × (its speed at impact ÷
    /// <see cref="Reference"/>)² × e^(−range ÷ <see cref="Slowing"/>): the game's ball keeps its speed, a real one is
    /// slowed by the air, so a close shot carries more, and a faster, heavier gun's ball further.</item>
    /// <item>What a wall it goes through takes (<see cref="Wall"/>): planking <see cref="Planking"/>, a deck
    /// <see cref="Decking"/>, a cabin's wall <see cref="CabinWall"/>, each 5% more a biome tier of its wood and more the
    /// slanter it strikes (÷ the cosine, at most <see cref="Slant"/> times). Less than that left, it stops in the wall.</item>
    /// <item>What the other kinds take when nobody registered a taker for them (<see cref="Default"/>).</item>
    /// </list>
    /// A small iron cannon's ball (108 on a structure, 48 m/s) carries 96 at 5 m, 69 at 20 m and 28 at 60 m: through a
    /// Meadows ship and out within about 10 m, into it and stopped in the far side at 20 m, stopped in the near side at 60 m.
    /// </summary>
    public static class KwShipHitEnergy
    {
        public const float HeavyBlunt = 50f, HeavyPierce = 300f;
        public const float Reference = 48f, Slowing = 45f;
        public const float Planking = 35f, Decking = 22f, CabinWall = 25f, PerTier = 0.05f, Slant = 3f;

        /// <summary>Whether a projectile's damage makes it a heavy shot.</summary>
        public static bool Heavy(HitData.DamageTypes damage) => damage.m_blunt >= HeavyBlunt || damage.m_pierce >= HeavyPierce;

        /// <summary>A hit's physical damage once a structure's resistances are taken off.</summary>
        public static float Structure(HitData hit, HitData.DamageModifiers resistances)
        {
            HitData copy = hit.Clone();
            copy.ApplyResistance(resistances, out _);
            return Mathf.Max(0f, copy.GetTotalPhysicalDamage());
        }

        /// <summary>The energy a shot starts with: its structure damage, by its speed at impact and the range it flew.</summary>
        public static float Start(float damage, float speed, float range)
        {
            float fast = speed > 0.1f ? Mathf.Clamp(speed / Reference, 0.3f, 1.6f) : 1f;
            return damage * fast * fast * Mathf.Exp(-Mathf.Max(0f, range) / Slowing);
        }

        /// <summary>
        /// What a wall of a kind takes from a shot going through it: its own toughness, by its biome tier (0 to 6) and how
        /// slant the shot strikes it (the surface's normal and the flight).
        /// </summary>
        public static float Wall(string kind, float tier, Vector3 normal, Vector3 way)
        {
            float tough = kind == KwShipHitRouter.Deck ? Decking : kind == KwShipHitRouter.Cabin ? CabinWall : Planking;
            float square = Mathf.Abs(Vector3.Dot(normal.normalized, way.normalized));
            return tough * (1f + PerTier * Mathf.Max(0f, tier)) / Mathf.Max(1f / Slant, square);
        }

        /// <summary>What a kind with no taker takes from the energy it meets.</summary>
        public static float Default(string kind, float energy)
        {
            float takes = kind switch
            {
                KwShipHitRouter.Mast => 60f,
                KwShipHitRouter.Yard => 25f,
                KwShipHitRouter.Cap => 20f,
                KwShipHitRouter.Bowsprit => 40f,
                KwShipHitRouter.Rudder => 30f,
                KwShipHitRouter.Fitting => 15f,
                KwShipHitRouter.Sail => 0f,
                _ => Planking,
            };
            return Mathf.Min(energy, takes);
        }

        /// <summary>The speed a shot leaves with when it keeps a share of its energy (energy goes with the speed squared).</summary>
        public static float SpeedLeft(float speed, float left, float energy) => energy > 0f ? speed * Mathf.Sqrt(Mathf.Clamp01(left / energy)) : 0f;
    }
}
