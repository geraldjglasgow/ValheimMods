using HarmonyLib;
using Party.Client;

namespace Party.Hooks
{
    /// <summary>
    /// Friendly fire, per PLAN.md: a prefix on <c>Character.RPC_Damage</c> right where vanilla already short-circuits
    /// PvP, adding one more condition - attacker and victim are party members. This runs only on the victim's own
    /// client (the ZDO owner, which is where the game already routes this RPC), so no server round trip is needed,
    /// the same trust model vanilla PvP already uses.
    /// </summary>
    [HarmonyPatch(typeof(Character), "RPC_Damage")]
    public static class DamagePatch
    {
        [HarmonyPrefix]
        public static bool Prefix(Character __instance, HitData hit)
        {
            if (!PartyConfig.FriendlyFire.Value || !PartyClientState.InParty)
                return true;
            if (!(__instance is Player victim) || victim != Player.m_localPlayer)
                return true;
            if (!(hit.GetAttacker() is Player attacker))
                return true;
            long attackerId = attacker.GetPlayerID();
            long victimId = victim.GetPlayerID();
            if (attackerId == 0 || victimId == 0 || attackerId == victimId)
                return true;
            return !IsPartyMember(attackerId);
        }

        private static bool IsPartyMember(long id) => PartyClientState.Find(id) != null;
    }
}
