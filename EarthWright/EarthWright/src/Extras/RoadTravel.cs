using EarthWright.Core;
using HarmonyLib;
using UnityEngine;

namespace EarthWright.Extras
{
    /// <summary>What the local player is standing on, for the road bonus.</summary>
    public enum RoadKind : byte
    {
        None = 0,
        Dirt = 1,
        Paved = 2,
    }

    /// <summary>
    /// The ground under the local player's feet, sampled four times a second from the paint mask of the terrain the player
    /// last stood on (red: dirt, blue: paved). Standing on a building or anything but terrain counts as no road; while
    /// airborne the last sample is kept, so a jump in the middle of a road keeps the bonus. Movement and stamina are the
    /// player's own client's business in Valheim, so only the local player is sampled.
    /// </summary>
    public static class RoadTravel
    {
        private const float Painted = 0.5f;
        private static float nextSample;

        public static RoadKind Current { get; private set; }

        /// <summary>The bonus share (0..1) for the ground under the local player now.</summary>
        public static float Bonus
        {
            get
            {
                if (Current == RoadKind.None || ExtrasSettings.PavedRoadBonus == null || !GeneralSettings.Enabled.Value)
                    return 0f;
                float percent = Current == RoadKind.Paved ? ExtrasSettings.PavedRoadBonus.Value : ExtrasSettings.DirtRoadBonus.Value;
                return Mathf.Clamp01(percent / 100f);
            }
        }

        internal static void Update()
        {
            if (Time.time < nextSample)
                return;
            nextSample = Time.time + 0.25f;
            Player player = Player.m_localPlayer;
            if (player == null)
                Current = RoadKind.None;
            else if (player.IsOnGround())
                Current = Sample(player);
        }

        private static RoadKind Sample(Player player)
        {
            Collider ground = player.GetLastGroundCollider();
            Heightmap map = ground != null ? ground.GetComponent<Heightmap>() : null;
            if (map == null || map.m_paintMask == null)
                return RoadKind.None;
            map.WorldToVertexMask(player.transform.position, out int x, out int y);
            Color paint = map.GetPaintMask(x, y);
            if (paint.b > Painted)
                return RoadKind.Paved;
            return paint.r > Painted ? RoadKind.Dirt : RoadKind.None;
        }

        /// <summary>The local player's sprint speed factor on the current ground.</summary>
        public static float SpeedFactor(Player player) => ReferenceEquals(player, Player.m_localPlayer) ? 1f + Bonus : 1f;
    }

    /// <summary>
    /// Marks the local player's sprint stamina check (<c>Player.CheckRun</c>), whose only stamina use is the sprint
    /// drain, so <see cref="RoadStaminaPatch"/> lowers exactly that use and nothing else.
    /// </summary>
    [HarmonyPatch(typeof(Player), nameof(Player.CheckRun))]
    public static class RoadSprintCheckPatch
    {
        internal static bool InSprintCheck;

        [HarmonyPrefix]
        public static void Prefix(Player __instance) => InSprintCheck = ReferenceEquals(__instance, Player.m_localPlayer);

        [HarmonyFinalizer]
        public static void Finalizer() => InSprintCheck = false;
    }

    /// <summary>Sprint stamina on a road costs less by the road's bonus.</summary>
    [HarmonyPatch(typeof(Player), nameof(Player.UseStamina))]
    public static class RoadStaminaPatch
    {
        [HarmonyPrefix]
        public static void Prefix(ref float v)
        {
            if (RoadSprintCheckPatch.InSprintCheck)
                v *= 1f - RoadTravel.Bonus;
        }
    }
}
