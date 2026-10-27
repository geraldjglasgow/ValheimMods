using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// A cannonball flown ahead for the shot arcs (<see cref="KwShotPath"/>), step for step as the game's projectile flies
    /// it (<c>Projectile.FixedUpdate</c>: its gravity added to its velocity, then the move; no drag) and, once it crosses
    /// the sea's surface (the game's own, waves and all), as <see cref="KwShotWater"/> flies it on under: its speed kept for
    /// its first metre under, then taken off by the water (<see cref="KwWaterFlight"/>), a little buoyancy against its
    /// gravity, done once slow or deep. Plain numbers, no game object.
    /// </summary>
    public sealed class KwBall
    {
        /// <summary>Metres over the sea's level the waves never reach: above it the sea is not looked at.</summary>
        public const float Waves = 5f;

        private float top, path, dragged;

        public KwBall(Vector3 at, Vector3 velocity, float gravity, float dt) => (At, Last, Velocity, Gravity, Dt) = (at, at, velocity, gravity, dt);

        public Vector3 At { get; private set; }

        /// <summary>Where it was a step ago.</summary>
        public Vector3 Last { get; private set; }

        public Vector3 Velocity { get; private set; }
        public float Gravity { get; private set; }
        public float Dt { get; private set; }

        /// <summary>Steps flown above the water (the path's points are taken every few).</summary>
        public int Count { get; set; }

        /// <summary>Whether it is under the water; where it went in; whether the water has stopped it.</summary>
        public bool Under { get; private set; }
        public Vector3 Entered { get; private set; }
        public bool Done { get; private set; }

        /// <summary>The sea's level, where the waves are measured from.</summary>
        public static float Sea => ZoneSystem.instance != null ? ZoneSystem.instance.m_waterLevel : 30f;

        /// <summary>One physics step: gravity, then the move.</summary>
        public Vector3 Step()
        {
            Last = At;
            Velocity += Vector3.down * (Gravity * Dt);
            At += Velocity * Dt;
            return At;
        }

        /// <summary>This ball made a copy of another, as it is now (to fly a few steps ahead of it).</summary>
        public void Copy(KwBall other)
        {
            (At, Last, Velocity, Gravity, Dt, Count) = (other.At, other.Last, other.Velocity, other.Gravity, other.Dt, other.Count);
            (Under, Entered, Done, top, path, dragged) = (other.Under, other.Entered, other.Done, other.top, other.path, other.dragged);
        }

        /// <summary>One step and the water after it, as the flight takes it; where it is then.</summary>
        public Vector3 Fly()
        {
            Step();
            Water();
            return At;
        }

        /// <summary>After a step: its way into the sea and under it (<see cref="KwShotWater"/>); true while it is under.</summary>
        public bool Water()
        {
            if (Under)
            {
                Swim(Last, At);
                return true;
            }
            if (At.y > Sea + Waves)
                return false;
            float surface = Floating.GetLiquidLevel(At);
            if (At.y >= surface)
                return false;
            float share = Last.y > At.y ? Mathf.Clamp01((Last.y - surface) / (Last.y - At.y)) : 0f;
            Entered = Vector3.Lerp(Last, At, share);
            (Under, top, path, dragged) = (true, surface, 0f, 0f);
            Swim(Entered, At);
            return true;
        }

        // Under the water: the path it has gone, its speed dragged down through the middle of its coming step, buoyancy
        // against gravity; done once slow or deep (KwShotWater.Swim).
        private void Swim(Vector3 from, Vector3 at)
        {
            path += Vector3.Distance(from, at);
            float ahead = Mathf.Max(dragged, path + 0.5f * Velocity.magnitude * Dt);
            Velocity = Velocity * KwWaterFlight.Kept(dragged, ahead) + Vector3.up * (Gravity * KwWaterFlight.Buoyancy * Dt);
            dragged = ahead;
            Done = KwWaterFlight.Done(Velocity.magnitude, top - at.y);
        }
    }

    /// <summary>
    /// What a ball fired from a cannon makes of what it meets, as the game's projectile does (<c>Projectile.OnHit</c> and
    /// <c>IsValidTarget</c>, the pass-through of <see cref="MannedWeapons.WeaponShot"/>): it passes through the body it was
    /// fired from (the ship) and the cannon itself, and through a character it spares (its shooter; while his PvP is off,
    /// anyone neither an enemy nor provokable, so friends and tame creatures); a creature it would hurt, a ship or a piece
    /// is a target; anything else (terrain, a rock, a tree) the ground.
    /// </summary>
    public sealed class KwShotPass
    {
        private readonly Rigidbody? carrier;
        private readonly Transform? weapon;
        private readonly Player? shooter;

        public KwShotPass(Rigidbody? carrier, Transform? weapon, Player? shooter) => (this.carrier, this.weapon, this.shooter) = (carrier, weapon, shooter);

        /// <summary>What the ball makes of a collider: none (it passes), a target, or the ground.</summary>
        public KwStrikeKind Kind(Collider? collider)
        {
            if (collider == null || (carrier != null && collider.attachedRigidbody == carrier) || (weapon != null && collider.transform.IsChildOf(weapon)))
                return KwStrikeKind.None;
            GameObject? hit = Projectile.FindHitObject(collider);
            IDestructible? struck = hit != null ? hit.GetComponent<IDestructible>() : null;
            if (struck is Character character)
                return Spared(character) ? KwStrikeKind.None : KwStrikeKind.Target;
            return struck is WearNTear ? KwStrikeKind.Target : KwStrikeKind.Ground;
        }

        // The shooter himself, and while his PvP is off anyone neither an enemy nor provokable (the game's IsValidTarget).
        private bool Spared(Character character)
        {
            if (shooter == null)
                return false;
            if (character == shooter)
                return true;
            BaseAI? ai = character.GetBaseAI();
            bool foe = BaseAI.IsEnemy(shooter, character) || (ai != null && ai.IsAggravatable());
            return !foe && !shooter.IsPVPEnabled();
        }
    }
}
