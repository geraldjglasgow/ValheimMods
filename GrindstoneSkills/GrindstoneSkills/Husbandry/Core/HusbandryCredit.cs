using HarmonyLib;
using PatchGuard;
using UnityEngine;

namespace GrindstoneSkills
{
    /// <summary>
    /// Husbandry experience earned on another machine: a creature's owner knows who killed it, but only the killer's
    /// client can raise the killer's skill. The owner sends a routed RPC (<see cref="Keys.RpcHusbandryCredit"/>) to that
    /// player's machine alone (<see cref="PlayerIds.PeerOf"/>, Everybody when it cannot tell); only the client whose local
    /// player has that ID acts on it, as with the cook credit. An offline player loses it. A hive's owner sends one Honey
    /// credit per honey a harvest hands out (<see cref="ExtraHoney"/>); a client without the Honey kind ignores it.
    /// Registered in a ZNet.Awake postfix, where the game creates a fresh ZRoutedRpc for each session.
    /// </summary>
    public static class HusbandryCredit
    {
        public enum Kind
        {
            Butchering = 1,
            Honey = 2,
        }

        [HarmonyPatch(typeof(ZNet), nameof(ZNet.Awake))]
        private static class NetAwake
        {
            [HarmonyPostfix]
            private static void Postfix() =>
                ZRoutedRpc.instance?.Register<long, int, string>(Keys.RpcHusbandryCredit, Receive);
        }

        /// <summary>Sends a credit of <paramref name="kind"/> for <paramref name="creaturePrefab"/> to the player with this ID.</summary>
        public static void Send(long playerId, Kind kind, string creaturePrefab)
        {
            if (playerId == 0L || ZRoutedRpc.instance == null)
                return;
            ZRoutedRpc.instance.InvokeRoutedRPC(PlayerIds.PeerOf(playerId), Keys.RpcHusbandryCredit, playerId, (int)kind, creaturePrefab ?? "");
        }

        private static void Receive(long sender, long playerId, int kind, string creaturePrefab) =>
            Guard.Run("husbandry credit", () => Credit(playerId, (Kind)kind, creaturePrefab));

        private static void Credit(long playerId, Kind kind, string creaturePrefab)
        {
            Player player = Player.m_localPlayer;
            if (player == null || player.GetPlayerID() != playerId)
                return;
            if (kind == Kind.Butchering)
                HusbandryXp.RaiseForCreature(player, HusbandryExperienceSettings.Butchering.Value, creaturePrefab);
            else if (kind == Kind.Honey)
                HusbandryXp.Raise(player, Mathf.Max(0f, HusbandryExperienceSettings.Honey.Value));
        }
    }
}
