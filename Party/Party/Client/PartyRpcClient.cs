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
        /// <summary>The longest a report is held back while nothing changed, so a member who just came online catches up.</summary>
        private const float HeartbeatSeconds = 1.5f;

        private static float vitalsTimer;
        private static float sinceSent = HeartbeatSeconds;
        private static SentVitals last;

        public static void RegisterRpcs()
        {
            ZRoutedRpc rpc = ZRoutedRpc.instance;
            rpc.Register<string>(PartyRpcServer.RpcReply, (s, text) => Guard.Run("party reply", () => PartyCommandOutput.Print(text)));
            rpc.Register<string>(PartyPublisher.Rpc, (s, text) => Guard.Run("party roster", () => PartyClientState.ApplyRoster(text)));
            rpc.Register<string, int>(PartyRpcServer.RpcInvitePrompt, (s, name, timeout) => Guard.Run("party invite prompt", () => InvitePromptUI.Show(name, timeout)));
            rpc.Register<string, string>(PartyRpcServer.RpcChatDeliver, (s, name, text) => Guard.Run("party chat deliver", () => PartyChatState.OnDeliver(name, text)));
            rpc.Register<string, Vector3>(PartyRpcServer.RpcPingDeliver, (s, name, pos) => Guard.Run("party ping deliver", () => PartyPing.OnDeliver(name, pos)));
            rpc.Register<long, ZPackage>(PartyRpcServer.RpcVitalsDeliver,
                (s, id, pkg) => Guard.Run("party vitals deliver", static (member, report) => PartyClientState.ApplyVitals(member, report), id, pkg));
            rpc.Register<string, Vector3>(PartyRpcServer.RpcDeathDeliver, (s, name, pos) => Guard.Run("party death deliver", () => PartyDeathNotice.OnDeliver(name, pos)));
        }

        /// <summary>
        /// Self-reports this player's own vitals at most <see cref="PartyConfig.VitalsUpdatesPerSecond"/> times a
        /// second, and only when another member is online to see them. A report that matches the last one sent is
        /// skipped until the heartbeat is due, so a resting player costs one message every
        /// <see cref="HeartbeatSeconds"/>.
        /// </summary>
        public static void Tick(float deltaTime)
        {
            Player local = Player.m_localPlayer;
            if (local == null || !PartyClientState.InParty || ZRoutedRpc.instance == null)
                return;
            vitalsTimer += deltaTime;
            sinceSent += deltaTime;
            float interval = 1f / Mathf.Max(1, PartyConfig.VitalsUpdatesPerSecond.Value);
            if (vitalsTimer < interval)
                return;
            vitalsTimer = 0f;
            if (PartyClientState.AnyOtherOnline(Identity.LocalPlayerId))
                Report(local);
        }

        private static void Report(Player local)
        {
            float health = Safe(local.GetHealth(), local.GetMaxHealth());
            float stamina = Safe(local.GetStamina(), local.GetMaxStamina());
            float eitr = Safe(local.GetEitr(), local.GetMaxEitr());
            int ailments = Ailments.Mask(local);
            Vector3 position = local.transform.position;
            if (sinceSent < HeartbeatSeconds && last.Matches(health, stamina, eitr, ailments, position))
                return;
            last = new SentVitals(health, stamina, eitr, ailments, position);
            sinceSent = 0f;
            ZPackage pkg = VitalsWire.Write(health, stamina, eitr, ailments, position, true);
            ZRoutedRpc.instance.InvokeRoutedRPC(PartyRpcServer.RpcVitalsReport, pkg);
        }

        private static float Safe(float value, float max) => max > 0f ? Mathf.Clamp01(value / max) : 0f;

        /// <summary>The last report sent. Bars change below half a percent and positions below half a metre are not news.</summary>
        private readonly struct SentVitals
        {
            private const float BarStep = 0.005f;
            private const float MoveStepSqr = 0.25f;

            private readonly float health, stamina, eitr;
            private readonly int ailments;
            private readonly Vector3 position;

            public SentVitals(float health, float stamina, float eitr, int ailments, Vector3 position)
            {
                this.health = health;
                this.stamina = stamina;
                this.eitr = eitr;
                this.ailments = ailments;
                this.position = position;
            }

            public bool Matches(float h, float s, float e, int a, Vector3 p) =>
                a == ailments && Mathf.Abs(h - health) < BarStep && Mathf.Abs(s - stamina) < BarStep &&
                Mathf.Abs(e - eitr) < BarStep && (p - position).sqrMagnitude < MoveStepSqr;
        }
    }
}
