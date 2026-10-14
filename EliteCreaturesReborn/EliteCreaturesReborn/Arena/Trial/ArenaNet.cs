using System.Collections.Generic;
using EliteCreaturesReborn.Patches;
using HarmonyLib;
using PatchGuard;
using UnityEngine;

namespace EliteCreaturesReborn.Arena.Trial
{
    /// <summary>
    /// The trial's messages and the stop that may come from anywhere (features/arena.md section 4), over the game's
    /// routed-RPC bus, as the raids send theirs (<see cref="Raids.RaidNet"/>). A message - Hel waking, the boss she sends,
    /// the end - goes from the core's owner to the players of the trial only (<see cref="TrialPlayers"/>), each its own
    /// client, as a word key with up to three words to put in it, so every player reads it in their own language; a
    /// dedicated server has no screen and never shows one. `elite arena stop` typed where the core is not loaded goes to
    /// the server, which holds every ZDO and passes it on to the core's owner. Registered on every machine at world
    /// start, so nothing arrives before its handler.
    /// </summary>
    internal static class ArenaNet
    {
        private static ZRoutedRpc? _registeredOn;

        /// <summary>Registers the handlers once per routed-RPC bus (a new world builds a new bus).</summary>
        public static void EnsureRegistered()
        {
            ZRoutedRpc bus = ZRoutedRpc.instance;
            if (bus == null || ReferenceEquals(bus, _registeredOn))
            {
                return;
            }
            _registeredOn = bus;
            bus.Register<string, string, string, string>(ArenaKeys.MessageRpc, OnMessage);
            bus.Register(ArenaKeys.StopFindRpc, OnStopFind);
            bus.Register<ZPackage>(ArenaKeys.RewardRpc, Rewards.RewardSend.OnReward);
            bus.Register<bool, int>(ArenaKeys.RepairedRpc, AnvilUse.OnAnswer);
            bus.Register<Vector3, float>(ArenaKeys.FlareRpc, OnFlare);
        }

        /// <summary>Every player of the trial sees a burst of cold soul-fire at <paramref name="at"/> (a boss cache, a
        /// repair at Hel's Anvil). Any machine holding the core.</summary>
        public static void Flare(ZDO core, Vector3 at, float size)
        {
            EnsureRegistered();
            if (ZRoutedRpc.instance == null)
            {
                return;
            }
            List<long> players = TrialPlayers.All(core);
            for (int i = 0; i < players.Count; i++)
            {
                ZRoutedRpc.instance.InvokeRoutedRPC(players[i], ArenaKeys.FlareRpc, at, size);
            }
        }

        /// <summary>One player reads the message under <paramref name="key"/>.</summary>
        public static void TellOne(long peer, string key, string first = "", string second = "", string third = "")
        {
            EnsureRegistered();
            if (ZRoutedRpc.instance != null)
            {
                ZRoutedRpc.instance.InvokeRoutedRPC(peer, ArenaKeys.MessageRpc, key, first, second, third);
            }
        }

        /// <summary>Core's owner: every player of the trial reads the message under <paramref name="key"/>.</summary>
        public static void Tell(ZDO core, string key, string first = "", string second = "", string third = "")
        {
            EnsureRegistered();
            if (ZRoutedRpc.instance == null)
            {
                return;
            }
            List<long> players = TrialPlayers.All(core);
            for (int i = 0; i < players.Count; i++)
            {
                ZRoutedRpc.instance.InvokeRoutedRPC(players[i], ArenaKeys.MessageRpc, key, first, second, third);
            }
        }

        /// <summary>Any machine: ends the running trial, wherever its core is owned.</summary>
        public static void AskStop()
        {
            EnsureRegistered();
            ArenaCore? core = ArenaCore.Current;
            ZDO? zdo = core != null ? core.Zdo : null;
            if (zdo != null)
            {
                Stop(zdo);
            }
            else if (ZRoutedRpc.instance != null)
            {
                ZRoutedRpc.instance.InvokeRoutedRPC(ArenaKeys.StopFindRpc); // to the server
            }
        }

        private static void Stop(ZDO core)
        {
            if (core.HasOwner() && ZRoutedRpc.instance != null)
            {
                ZRoutedRpc.instance.InvokeRoutedRPC(core.GetOwner(), core.m_uid, ArenaKeys.StopRpc);
            }
        }

        private static void OnFlare(long sender, Vector3 at, float size) =>
            Guard.Run("ArenaNet.OnFlare", () => ArenaPortalLook.Flare(at, size));

        private static void OnMessage(long sender, string key, string first, string second, string third) =>
            Guard.Run("ArenaNet.OnMessage", () => ArenaWords.Show(key, first, second, third));

        // On the server: the core it keeps (it holds every ZDO), told to stop by its owner.
        private static void OnStopFind(long sender) => Guard.Run("ArenaNet.OnStopFind", () =>
        {
            ZDO? core = ZNet.instance != null && ZNet.instance.IsServer() ? ArenaCoreKeeper.KnownCore() : null;
            if (core != null)
            {
                Stop(core);
            }
        });
    }

    /// <summary>World start, on every machine: the trial's messages are registered before any trial can send one.</summary>
    [HarmonyPatch(typeof(ZoneSystem), "Start")]
    internal static class ArenaNetPatch
    {
        private static void Postfix() => SafeCall.Run("ZoneSystem.Start arena messages", ArenaNet.EnsureRegistered);
    }
}
