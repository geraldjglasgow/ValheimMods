using PatchGuard;
using UnityEngine;

namespace GrindstoneSkills
{
    /// <summary>
    /// Sailing experience, once a second on each player's own client. Aboard a ship that someone steers, the distance
    /// the ship moved over the water since the last second earns experience: the full rate for the helmsman, the crew
    /// share for everyone else. The distance is measured flat, so bobbing earns nothing, and a jump of more than
    /// <see cref="MaxStep"/> metres in a second (a teleport, a ship that just loaded) earns nothing either. A ship nobody
    /// steers earns nothing. The level every client publishes for <see cref="HelmSpeed"/> is <see cref="CustomSkillLevels"/>.
    /// </summary>
    public static class SailingXp
    {
        private const float Interval = 1f;
        private const float MaxStep = 60f;

        private static float timer;
        private static Ship lastShip;
        private static Vector3 lastPosition;

        /// <summary>Every frame for the local player (<see cref="LocalPlayerTick"/>).</summary>
        public static void Tick(Player player, float dt)
        {
            timer += dt;
            if (timer < Interval)
                return;
            timer = 0f;
            Guard.Run("sailing tick", static p => Earn(p), player);
        }

        private static void Earn(Player player)
        {
            Ship ship = Ship.GetLocalShip();
            Vector3 position = ship != null ? ship.transform.position : Vector3.zero;
            float moved = ship != null && ship == lastShip ? Flat(position - lastPosition) : 0f;
            lastShip = ship;
            lastPosition = position;
            if (!SailingSkill.Active || moved <= 0f || moved > MaxStep)
                return;
            float perKilometre = Mathf.Max(0f, SailingSettings.HelmExperience.Value) * Share(player, ship);
            if (perKilometre > 0f)
                player.RaiseSkill(SailingSkill.Type, moved / 1000f * perKilometre);
        }

        /// <summary>1 at the helm, the crew share aboard a ship someone else steers, 0 when nobody steers.</summary>
        private static float Share(Player player, Ship ship)
        {
            Player helmsman = Helm.Helmsman(ship);
            if (helmsman == null)
                return 0f;
            return helmsman == player ? 1f : Mathf.Clamp01(SailingSettings.CrewShare.Value / 100f);
        }

        private static float Flat(Vector3 offset) => new Vector2(offset.x, offset.z).magnitude;
    }
}
