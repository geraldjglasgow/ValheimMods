using System.Collections.Generic;
using HarmonyLib;

namespace Lockstep
{
    /// <summary>
    /// Runs on the client that owns a dying boss (the only place OnDeath runs) and tells the server which
    /// players attacked it. The boss ZDO carries one flag per attacking player name, written by the game
    /// in Character.RPC_Damage. Prefix because OnDeath destroys the object at its end.
    /// </summary>
    [HarmonyPatch(typeof(Character), nameof(Character.OnDeath))]
    public static class KillReporter
    {
        [HarmonyPrefix]
        public static void Prefix(Character __instance)
        {
            if (__instance.IsPlayer() || string.IsNullOrEmpty(__instance.m_defeatSetGlobalKey))
                return;
            ZNetView view = __instance.m_nview;
            if (view == null || !view.IsValid() || !view.IsOwner())
                return;
            if (Chain.ByKey(__instance.m_defeatSetGlobalKey) == null)
                return;

            ZDO zdo = view.GetZDO();
            List<string> attackers = new List<string>();
            foreach (ZNet.PlayerInfo player in ZNet.instance.GetPlayerList())
            {
                if (zdo.GetBool(ZDOVars.s_attackers + player.m_name))
                    attackers.Add(player.m_name);
            }
            Lockstep.Log.LogInfo($"{__instance.m_name} died, attackers: {(attackers.Count > 0 ? string.Join(", ", attackers) : "none recorded")}.");
            ZRoutedRpc.instance.InvokeRoutedRPC(ProgressServer.RpcBossDefeated,
                __instance.m_defeatSetGlobalKey, string.Join("\n", attackers), __instance.transform.position);
        }
    }
}
