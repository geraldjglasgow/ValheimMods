using EliteCreaturesReborn.Display;
using HarmonyLib;
using PatchGuard;

namespace EliteCreaturesReborn.Patches
{
    /// <summary>
    /// Keeps the world tier box under the minimap current (<see cref="TierHud"/>). The HUD updates every frame whether
    /// or not the inventory is open, which is what a box always in view needs; the box touches its text only when the
    /// tier changes. Never on a dedicated server, which has no HUD.
    /// </summary>
    [HarmonyPatch(typeof(Hud), "Update")]
    public static class HudTierPatch
    {
        private static void Postfix() => Guard.Run("Hud world tier", TierHud.Refresh);
    }
}
