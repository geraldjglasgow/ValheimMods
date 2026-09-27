using HarmonyLib;
using UnityEngine;

namespace GrindstoneSkills
{
    /// <summary>
    /// The adrenaline bonus. The game adds adrenaline for blocks and parries inside Humanoid.BlockAttack and takes some
    /// away (Player.m_nonBlockDamageAdrenaline) for a hit that was not blocked, inside Character.RPC_Damage; both call
    /// Player.AddAdrenaline on the player's own client. While a hit is reaching the local player
    /// (<see cref="IncomingHit"/>), a gain grows by the bonus and a loss shrinks by it.
    /// </summary>
    [HarmonyPatch(typeof(Player), nameof(Player.AddAdrenaline))]
    public static class AdrenalinePerk
    {
        [HarmonyPrefix]
        private static void Prefix(Player __instance, ref float v)
        {
            if (!IncomingHit.Active || !DefenseSkill.IsLocal(__instance))
                return;
            float share = DefenseSkill.LocalShare(DefenseGuardSettings.Adrenaline.Value);
            v *= v > 0f ? 1f + share : 1f - Mathf.Clamp01(share);
        }
    }
}
