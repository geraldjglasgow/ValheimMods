using HarmonyLib;
using Party.Client;

namespace Party.Hooks
{
    /// <summary>Friendly fire: extends vanilla's own PvP check in <c>Character.RPC_Damage</c> on the victim's client.</summary>
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
