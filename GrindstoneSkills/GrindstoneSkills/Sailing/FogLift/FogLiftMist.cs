using UnityEngine;

namespace GrindstoneSkills
{
    /// <summary>
    /// The Mistlands mist parting around a ship with its fog lifted, on the clients aboard it (<see cref="FogLiftView"/>).
    /// The game's mist (ParticleMist, local on each client) is never emitted inside a Demister's range and is pushed out
    /// of it by the Demister's force field: that is how the wisp light clears it. So while the fog is lifted the ship
    /// carries a local Demister of its own, a child object with a force field copied from the wisp's own (the Demister
    /// status effect's ball prefab, read once, never instantiated), reaching <see cref="Radius"/> m. It is never networked
    /// and is removed when the fog closes in or the player leaves the ship; it goes with the ship when the ship unloads.
    /// Without the wisp's force field (another mod removed it) the mist stays and the fog still thins.
    /// </summary>
    public static class FogLiftMist
    {
        /// <summary>Metres from the ship's centre the mist parts.</summary>
        public const float Radius = 20f;

        private static GameObject field;
        private static Ship on;
        private static ParticleSystemForceField source;
        private static bool looked;

        /// <summary>Puts the ship's Demister on <paramref name="ship"/>, or takes it away with null.</summary>
        public static void Follow(Ship ship)
        {
            if (ship == on)
                return;
            if (field != null)
                Object.Destroy(field);
            field = ship != null ? Create(ship) : null;
            on = ship;
        }

        private static GameObject Create(Ship ship)
        {
            ParticleSystemForceField wisp = Source();
            if (wisp == null)
                return null;
            GameObject holder = new GameObject("grindstone_foglift");
            holder.SetActive(false);
            holder.layer = wisp.gameObject.layer;
            holder.transform.SetParent(ship.transform, false);
            ParticleSystemForceField force = holder.AddComponent<ParticleSystemForceField>();
            Copy(wisp, force);
            force.endRange = Radius;
            holder.AddComponent<Demister>();
            holder.SetActive(true);
            return holder;
        }

        /// <summary>The wisp ball's force field, found once.</summary>
        private static ParticleSystemForceField Source()
        {
            if (looked || ObjectDB.instance == null || ObjectDB.instance.m_StatusEffects.Count == 0)
                return source;
            looked = true;
            foreach (StatusEffect effect in ObjectDB.instance.m_StatusEffects)
            {
                if (effect is SE_Demister demister && demister.m_ballPrefab != null)
                {
                    source = demister.m_ballPrefab.GetComponentInChildren<ParticleSystemForceField>(true);
                    if (source != null)
                        break;
                }
            }
            return source;
        }

        private static void Copy(ParticleSystemForceField from, ParticleSystemForceField to)
        {
            to.shape = from.shape;
            to.startRange = from.startRange;
            to.length = from.length;
            to.directionX = from.directionX;
            to.directionY = from.directionY;
            to.directionZ = from.directionZ;
            to.gravity = from.gravity;
            to.gravityFocus = from.gravityFocus;
            to.rotationSpeed = from.rotationSpeed;
            to.rotationAttraction = from.rotationAttraction;
            to.drag = from.drag;
            to.multiplyDragByParticleSize = from.multiplyDragByParticleSize;
            to.multiplyDragByParticleVelocity = from.multiplyDragByParticleVelocity;
        }
    }
}
