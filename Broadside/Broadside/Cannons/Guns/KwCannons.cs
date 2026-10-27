using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// The ship cannons' shots (<see cref="KwShipCannons"/>), breech-loaders all. Each fires a ball
    /// (<see cref="KwCannonballs"/>, its ammunition) and burns black powder with it (<see cref="KwBlackPowder"/>, the shot's
    /// charge: no powder, no shot), raising the gunner's Crossbows skill: the bronze cannon's stone shot (Black Forest), a
    /// little softer than the small iron cannon's iron ball (Swamp), the small cannon's of black metal (Plains) and the
    /// large flametal cannon's (Ashlands), each harder-hitting than the last and burning more powder: one, one, two, three
    /// a shot. A ball flies in an arc as the game's catapult stone does, wearing its
    /// own model, hits hard and blunt (harder on pieces and ships), and bursts where it lands in a copy of the siege bomb's
    /// explosion, without the crater it digs (the dynamite's waits out a fuse first; the catapult's is only dust); into the
    /// sea it splashes and goes on under, slowed hard, its burst left above water (<see cref="KwShotWater"/>); the muzzle
    /// bangs, flashes and smokes with small copies of the game's bomb blasts. The gunner fires by drawing the T-handle in
    /// the breech door out on its cord, the shot leaving partway through the pull (<see cref="KwCannonFiring"/>), the
    /// barrel kicks back through its trunnion band and runs out again, and he reloads at the breech
    /// (<see cref="KwCannonWork"/>).
    /// </summary>
    public static class KwCannons
    {
        public const string Bronze = "cannon_bronze", SmallIron = "cannon_small_iron", Small = "cannon_small", Large = "cannon_large";

        private const string Ball = "Catapult_Ammo_Projectile", Blast = "siegebomb_explosion", Bang = "sfx_bombdynamite_explosion";

        /// <summary>
        /// Times a ball's own hit is as hard on a building piece or a ship as on a creature (<see cref="KwShot.Structures"/>):
        /// with the burst, ten iron balls from a small iron cannon sink the game's longship (1000 health; every cannon's
        /// damage lowered a quarter twice on 2026-10-26) (CLAUDE.md, "Cannons").
        /// </summary>
        public const float OnStructures = 1.6f;

        // A stone ball on one powder: the small iron cannon's flight and reload, 50 blunt (the least a heavy shot is, so it
        // still holes a Broadside hull and goes through it; 80 on a piece or a ship) and a 2.5 m burst of 15: a hit on a
        // ship 95 to the iron ball's 103.5, so about 22 stone shots sink what 20 iron ones do (CANNONS.md, "Damage").
        public static KwShot BronzeShot()
        {
            KwShot shot = Shot(Bronze, KwCannonballs.Stone, 1, KwShipHitEnergy.HeavyBlunt, Burst(2.5f, 15f, 0f, 20f));
            (shot.Speed, shot.Gravity, shot.Spread, shot.Push, shot.Stagger, shot.Reload) = (48f, 5f, 1f, 60f, 1.5f, 8f);
            shot.EffectScale = 0.35f;
            return shot;
        }

        // An iron ball on one powder: slow and dropping, 50.6 blunt (81 on a piece or a ship) and a 2.5 m burst of 22.5, every 8 seconds.
        public static KwShot SmallIronShot()
        {
            KwShot shot = Shot(SmallIron, KwCannonballs.Iron, 1, 50.625f, Burst(2.5f, 22.5f, 0f, 20f));
            (shot.Speed, shot.Gravity, shot.Spread, shot.Push, shot.Stagger, shot.Reload) = (48f, 5f, 1f, 60f, 1.5f, 8f);
            shot.EffectScale = 0.35f;
            return shot;
        }

        // An iron ball on two powder: faster and flatter, 95.6 blunt (153 on a piece or a ship) and a 3 m burst of 45, every 7 seconds.
        public static KwShot SmallShot()
        {
            KwShot shot = Shot(Small, KwCannonballs.Iron, 2, 95.625f, Burst(3f, 45f, 0f, 30f));
            (shot.Speed, shot.Gravity, shot.Spread, shot.Push, shot.Stagger, shot.Reload) = (56f, 4.5f, 0.8f, 80f, 2f, 7f);
            shot.EffectScale = 0.42f;
            return shot;
        }

        // A flametal ball on three powder: 185.6 blunt (297 on a piece or a ship) and a 4 m burst of 78.75 blunt and 33.75 fire (it
        // sets alight), every 10 seconds.
        public static KwShot LargeShot()
        {
            KwShot shot = Shot(Large, KwCannonballs.Flametal, 3, 185.625f, Burst(4f, 78.75f, 33.75f, 50f));
            (shot.Speed, shot.Gravity, shot.Spread, shot.Push, shot.Stagger, shot.Reload) = (62f, 5.5f, 0.6f, 120f, 3f, 10f);
            shot.Effects[1] = "fx_dynamite_explosion";
            shot.EffectScale = 0.45f;
            return shot;
        }

        // One ball a pull and so much powder, the catapult stone's flight in the ball's model, a blunt hit (harder on pieces
        // and ships) and a burst; the muzzle's bang and its flash and smoke (the siege bomb's blast, drawn small).
        private static KwShot Shot(string name, string ball, int powder, float blunt, KwBurst burst)
        {
            KwShot shot = new KwShot(name)
            {
                Projectile = Ball,
                Wears = ball,
                Life = 6f,
                Damage = new HitData.DamageTypes { m_blunt = blunt },
                Structures = OnStructures,
                Burst = burst,
                Finish = NoCrater,
                Dives = true,
            };
            shot.Ammo.Add(ball);
            shot.Charge.Add((KwBlackPowder.Name, powder));
            shot.Effects.Clear();
            shot.Effects.AddRange(new[] { Bang, "fx_siegebomb_explosion" });
            return shot;
        }

        // The siege bomb's blast digs a crater where it meets the ground; a ship's cannon leaves the ground as it was.
        private static void NoCrater(GameObject projectile)
        {
            Projectile? flight = projectile.GetComponent<Projectile>();
            Aoe? area = flight != null && flight.m_spawnOnHit != null ? flight.m_spawnOnHit.GetComponent<Aoe>() : null;
            if (area != null)
                area.m_spawnOnHitTerrain = null;
        }

        private static KwBurst Burst(float radius, float blunt, float fire, float push) => new KwBurst
        {
            Area = Blast,
            Radius = radius,
            Damage = new HitData.DamageTypes { m_blunt = blunt, m_fire = fire },
            Push = push,
        };
    }
}
