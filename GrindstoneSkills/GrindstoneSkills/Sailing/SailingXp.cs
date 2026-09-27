using HarmonyLib;
using PatchGuard;
using UnityEngine;

namespace GrindstoneSkills
{
    /// <summary>
    /// Sailing experience, and the level every client publishes, once a second on each player's own client.
    /// <list type="bullet">
    /// <item>The local player's Sailing level goes to their own ZDO whenever it changed, so the owner of a ship they
    /// steer can read it (<see cref="HelmSpeed"/>).</item>
    /// <item>Aboard a ship that someone steers, the distance the ship moved over the water since the last second earns
    /// experience: the full rate for the helmsman, the crew share for everyone else. The distance is measured flat, so
    /// bobbing earns nothing, and a jump of more than <see cref="MaxStep"/> metres in a second (a teleport, a ship
    /// that just loaded) earns nothing either. A ship nobody steers earns nothing.</item>
    /// </list>
    /// </summary>
    public static class SailingXp
    {
        private const float Interval = 1f;
        private const float MaxStep = 60f;

        private static float timer;
        private static Ship lastShip;
        private static Vector3 lastPosition;
        private static Player publishedFor;
        private static float published;

        [HarmonyPatch(typeof(Player), nameof(Player.Update))]
        private static class Tick
        {
            [HarmonyPostfix]
            private static void Postfix(Player __instance)
            {
                if (__instance != Player.m_localPlayer)
                    return;
                timer += Time.deltaTime;
                if (timer < Interval)
                    return;
                timer = 0f;
                Guard.Run("sailing tick", () =>
                {
                    Publish(__instance);
                    Earn(__instance);
                });
            }
        }

        private static void Publish(Player player)
        {
            float level = player.GetSkillLevel(SailingSkill.Type);
            ZNetView nview = player.m_nview;
            if ((publishedFor == player && published == level) || nview == null || !nview.IsValid() || !nview.IsOwner())
                return;
            nview.GetZDO().Set(Keys.SailingLevel, level);
            publishedFor = player;
            published = level;
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
