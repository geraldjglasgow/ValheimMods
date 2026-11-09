using HarmonyLib;
using PatchGuard;

namespace Broadside
{
    /// <summary>
    /// A launched ship wrecked (sunk by its flooding, <see cref="KwShipFlood"/>, or its health run out to shots or blows) gives
    /// back what it cost in floating crates along it, as a sunk game ship gives back its own (a player's report, 2026-11-09:
    /// "Boats don't give back any mats when they sink"). The game destroys a wrecked piece on its owner only, once, with
    /// its health already 0 in its ZDO (<c>WearNTear.ApplyDamage</c>); a ship broken up with the hammer reaches the same
    /// <c>Destroy</c> with its health left, and has had its crates already (<see cref="KwShipBreakOwner"/>). A removal that
    /// drops nothing (<c>blockDrop</c>) and <c>kwship clear</c> (no <c>Destroy</c> at all) give nothing.
    /// </summary>
    [HarmonyPatch(typeof(WearNTear), "Destroy")]
    public static class KwShipWreck
    {
        [HarmonyPrefix]
        private static void Prefix(WearNTear __instance, bool blockDrop)
        {
            KwShipBody? body = blockDrop ? null : __instance.GetComponent<KwShipBody>();
            if (body != null)
                Guard.Run("Broadside ship wreck", Spill, __instance, body);
        }

        private static void Spill(WearNTear wear, KwShipBody body)
        {
            ZNetView view = wear.m_nview;
            if (view == null || !view.IsValid() || !view.IsOwner() || view.GetZDO().GetFloat(ZDOVars.s_health, wear.m_health) > 0f)
                return;
            KwShipBody.Step("Broadside ship wreck crates", () => Log.Info($"A wrecked ship left {KwShipBreakSpill.Wreck(body)} crates"));
        }
    }
}
