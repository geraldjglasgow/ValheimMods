using HarmonyLib;
using UnityEngine;

namespace Wayfare.QuickJumps
{
    /// <summary><c>Player.UpdateTeleport(float)</c> prefix (private; the game calls it in the fixed update of a living
    /// player on its owner's client): a long jump of the local player is hurried by <see cref="JumpTiming"/> before the
    /// game adds this tick to its timer. A sea gate's crew hold, another prefix here, replaces the game's step while it
    /// runs; <see cref="JumpTiming"/> stands aside then.</summary>
    [HarmonyPatch(typeof(Player), nameof(Player.UpdateTeleport))]
    public static class JumpTimingPatch
    {
        [HarmonyPrefix]
        public static void Prefix(Player __instance, float dt)
        {
            if (__instance != Player.m_localPlayer || !__instance.m_teleporting || !__instance.m_distantTeleport)
                return;
            JumpTiming.Hurry(__instance, dt);
        }
    }

    /// <summary><c>Player.TeleportTo(Vector3, Quaternion, bool)</c> postfix: a long jump of the local player has started
    /// (the game returns true only on the owner's client once the jump is set up), so <see cref="JumpScreen"/> decides
    /// now whether it needs the teleport screen.</summary>
    [HarmonyPatch(typeof(Player), nameof(Player.TeleportTo))]
    public static class JumpStartPatch
    {
        [HarmonyPostfix]
        public static void Postfix(Player __instance, Vector3 pos, bool distantTeleport, bool __result)
        {
            if (__result && distantTeleport && __instance == Player.m_localPlayer)
                JumpScreen.Started(pos);
        }
    }

    /// <summary><c>Hud.UpdateBlackScreen(Player, float)</c> prefix (private): during a jump into an area that was
    /// already loaded (<see cref="JumpScreen"/>) the screen does what the game does when nothing holds it, fade out,
    /// instead of fading to black with the teleport swirl. A quick jump that loads goes black faster, then the game's
    /// update runs. Every other case is the game's.</summary>
    [HarmonyPatch(typeof(Hud), nameof(Hud.UpdateBlackScreen))]
    public static class JumpScreenPatch
    {
        [HarmonyPrefix]
        public static bool Prefix(Hud __instance, Player player, float dt)
        {
            if (JumpScreen.Clear(player))
            {
                JumpScreen.FadeOut(__instance, dt);
                return false;
            }
            if (JumpScreen.Loading(player))
                JumpScreen.FadeInFast(__instance, dt);
            return true;
        }
    }
}
