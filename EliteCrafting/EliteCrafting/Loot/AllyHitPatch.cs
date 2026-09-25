using HarmonyLib;

namespace EliteCrafting.Loot
{
    /// <summary>
    /// Sets <see cref="LootKeys.AllyHit"/> on a creature when a tamed or summoned ally hits it (drops.md section 2), so
    /// players who fight with wolves or skeletons still qualify for our drops. Runs where the game applies the damage:
    /// the damage RPC reaches every peer, but only the creature's ZDO owner acts, as vanilla does. A prefix, so the flag
    /// is in the ZDO before a killing blow reaches the death hook. Per hit it costs one ZDO bool read once the flag is set.
    /// </summary>
    [HarmonyPatch(typeof(Character), nameof(Character.RPC_Damage))]
    internal static class AllyHitPatch
    {
        private static void Prefix(Character __instance, HitData hit)
        {
            if (hit == null || !hit.HaveAttacker() || __instance.IsPlayer())
            {
                return;
            }
            ZNetView nview = __instance.m_nview;
            if (nview == null || !nview.IsOwner())
            {
                return;
            }
            ZDO zdo = nview.GetZDO();
            if (LootKeys.AllyHitSet(zdo))
            {
                return;
            }
            Character attacker = hit.GetAttacker();
            if (attacker != null && !attacker.IsPlayer() && LootKeys.IsPlayerAlly(attacker))
            {
                zdo.Set(LootKeys.AllyHitHash, true);
            }
        }
    }
}
