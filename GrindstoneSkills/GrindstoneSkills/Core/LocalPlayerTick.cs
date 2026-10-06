using HarmonyLib;
using UnityEngine;

namespace GrindstoneSkills
{
    /// <summary>
    /// The one Player.Update postfix behind every feature that ticks on the local player's own client, in this order:
    /// Sailing experience, herd watching, the published custom skill levels, the Wind Call and lookout keys, Defense's
    /// regeneration and its HUD icons. Every other player's Update (each other player this client has loaded, and every
    /// player on a dedicated server) returns at the first line. Nothing allocates: each feature keeps its own timer and
    /// runs its guarded work, through a static lambda, only when that timer fires or its key is pressed.
    /// </summary>
    [HarmonyPatch(typeof(Player), nameof(Player.Update))]
    public static class LocalPlayerTick
    {
        [HarmonyPostfix]
        private static void Postfix(Player __instance)
        {
            if (!ReferenceEquals(__instance, Player.m_localPlayer))
                return;
            float dt = Time.deltaTime;
            SailingXp.Tick(__instance, dt);
            HerdWatch.Tick(__instance, dt);
            CustomSkillLevels.Tick(__instance, dt);
            WindCallInput.Tick(__instance);
            LookoutInput.Tick(__instance);
            Recovery.Tick(__instance, dt);
            DefenseEffects.Tick(dt);
        }
    }
}
