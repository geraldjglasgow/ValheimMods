using PatchGuard;
using UnityEngine;
using Party.Chat;
using Party.Commands;
using Party.Server;
using Party.UI;

namespace Party.Client
{
    /// <summary>Client side RPC registrations: everything the server pushes to a player about their own party.</summary>
    public static class PartyRpcClient
    {
        private const float VitalsIntervalSeconds = 1f;
        private static float vitalsTimer;

        public static void RegisterRpcs()
        {
            ZRoutedRpc rpc = ZRoutedRpc.instance;
            rpc.Register<string>(PartyRpcServer.RpcReply, (s, text) => Guard.Run("party reply", () => PartyCommandOutput.Print(text)));
            rpc.Register<string>(PartyPublisher.Rpc, (s, text) => Guard.Run("party roster", () => PartyClientState.ApplyRoster(text)));
            rpc.Register<string, int>(PartyRpcServer.RpcInvitePrompt, (s, name, timeout) => Guard.Run("party invite prompt", () => InvitePromptUI.Show(name, timeout)));
            rpc.Register<string, string>(PartyRpcServer.RpcChatDeliver, (s, name, text) => Guard.Run("party chat deliver", () => PartyChatState.OnDeliver(name, text)));
            rpc.Register<string, Vector3>(PartyRpcServer.RpcPingDeliver, (s, name, pos) => Guard.Run("party ping deliver", () => PartyPing.OnDeliver(name, pos)));
            rpc.Register<long, float, float, float, Vector3, bool>(PartyRpcServer.RpcVitalsDeliver,
                (s, id, hp, st, ei, pos, valid) => Guard.Run("party vitals deliver", () => PartyClientState.ApplyVitals(id, hp, st, ei, pos, valid)));
        }

        /// <summary>Called every frame from <see cref="PartyTicker"/>: self-reports this player's own vitals once a second.</summary>
        public static void Tick(float deltaTime)
        {
            Player local = Player.m_localPlayer;
            if (local == null || !PartyClientState.InParty || ZRoutedRpc.instance == null)
                return;
            vitalsTimer += deltaTime;
            if (vitalsTimer < VitalsIntervalSeconds)
                return;
            vitalsTimer = 0f;
            Report(local);
        }

        private static void Report(Player local)
        {
            float health = Safe(local.GetHealth(), local.GetMaxHealth());
            float stamina = Safe(local.GetStamina(), local.GetMaxStamina());
            float eitr = Safe(local.GetEitr(), local.GetMaxEitr());
            ZRoutedRpc.instance.InvokeRoutedRPC(PartyRpcServer.RpcVitalsReport, health, stamina, eitr, local.transform.position, true);
        }

        private static float Safe(float value, float max) => max > 0f ? Mathf.Clamp01(value / max) : 0f;
    }
}
