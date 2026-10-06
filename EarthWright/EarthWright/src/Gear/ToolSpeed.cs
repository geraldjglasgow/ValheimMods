using HarmonyLib;

namespace EarthWright.Gear
{
    /// <summary>
    /// Faster movement while a terrain tool is in the local player's hand ("Movement Speed"): the game's jog and sprint
    /// speed factors are multiplied. Walking (the walk toggle), crouching and swimming keep their speeds. Movement is
    /// simulated by the player's own client in Valheim, so only the local player is changed; others see the result.
    /// </summary>
    public static class ToolSpeed
    {
        /// <summary>The multiplier for this player now: the setting while the local player holds a terrain tool, else 1.</summary>
        public static float Factor(Player player)
        {
            if (!ReferenceEquals(player, Player.m_localPlayer) || !HeldTool.Active || GearSettings.MovementSpeed == null)
                return 1f;
            return GearSettings.MovementSpeed.Value;
        }
    }

    /// <summary>Running (the game's normal movement) is faster by "Movement Speed" while a terrain tool is held.</summary>
    [HarmonyPatch(typeof(Player), nameof(Player.GetJogSpeedFactor))]
    public static class ToolJogSpeedPatch
    {
        [HarmonyPostfix]
        public static void Postfix(Player __instance, ref float __result) => __result *= ToolSpeed.Factor(__instance);
    }
}
