using System;
using System.Threading;
using HarmonyLib;
using UnityEngine;

namespace HighSeat
{
    /// <summary>
    /// When the server's sends skip the game's copies: <see cref="SteamDirect"/> (a package handed to Steam without the
    /// socket's copy), <see cref="RouteBroadcast"/> (a broadcast written once for every player) and
    /// <see cref="NestedWrite"/> (a package written into another without a copy). Each is on only on the server
    /// (dedicated or hosting), while Big server sync is on, and while its check passes (<see cref="SendChecks"/>: the
    /// game's code it stands in for is the code it was checked against and no other mod patches it); a player's game is
    /// never touched. Checked on the main thread from ZNet.Update when a world starts and every
    /// <see cref="CheckSeconds"/> s after (a mod can patch later); each change is logged once. One that throws is off for
    /// the rest of the session (the world), logged once, and a new world starts afresh; so are all three if the checks
    /// themselves throw. The flags belong to the world they were checked for, so they read false as soon as it closes.
    /// </summary>
    [HarmonyPatch(typeof(ZNet), nameof(ZNet.Update))]
    internal static class NetGate
    {
        public enum Feature
        {
            Direct,
            Broadcast,
            Nested,
        }

        private const float CheckSeconds = 5f;
        private static readonly string[] Names =
        {
            "sends straight to Steam", "broadcasts written once", "nested packages without a copy",
        };
        private static readonly bool[] On = new bool[3];
        private static readonly bool[] Failed = new bool[3];
        private static readonly string[] Said = new string[3];
        private static ZNet _session;
        private static ZNet _broken;
        private static float _nextCheck;
        private static int _mainThread = -1;

        public static bool Direct => On[(int)Feature.Direct] && ReferenceEquals(ZNet.instance, _session);
        public static bool Nested => On[(int)Feature.Nested] && ReferenceEquals(ZNet.instance, _session);

        /// <summary>Also only on the main thread, where the game sends: the broadcast writes one shared package.</summary>
        public static bool Broadcast => On[(int)Feature.Broadcast] && ReferenceEquals(ZNet.instance, _session) &&
            Thread.CurrentThread.ManagedThreadId == _mainThread;

        private static void Postfix(ZNet __instance)
        {
            if (ReferenceEquals(__instance, _broken))
            {
                return;
            }
            try
            {
                Tick(__instance);
            }
            catch (Exception e)
            {
                _broken = __instance;
                Array.Clear(On, 0, On.Length);
                Log($"the send checks failed, so the game's own code sends for the rest of this session: {e}");
            }
        }

        /// <summary>Turns <paramref name="feature"/> off for the rest of the session, logged once. Never throws.</summary>
        public static void Fail(Feature feature, Exception e)
        {
            int i = (int)feature;
            On[i] = false;
            if (Failed[i])
            {
                return;
            }
            Failed[i] = true;
            Log($"{Names[i]} failed, so the game's own code sends for the rest of this session: {e}");
        }

        private static void Tick(ZNet net)
        {
            if (!ReferenceEquals(net, _session))
            {
                Begin(net);
            }
            if (!net.IsServer() || !Settings.BigServerSync.Value)
            {
                Array.Clear(On, 0, On.Length);
                return;
            }
            float now = Time.unscaledTime;
            if (now < _nextCheck)
            {
                return;
            }
            _nextCheck = now + CheckSeconds;
            Decide(Feature.Direct, SendChecks.Direct());
            Decide(Feature.Broadcast, SendChecks.Broadcast());
            Decide(Feature.Nested, SendChecks.Nested());
        }

        // A new world: everything off until checked for it, failures forgotten. ZNet.Update runs on the main thread.
        private static void Begin(ZNet net)
        {
            _session = net;
            _mainThread = Thread.CurrentThread.ManagedThreadId;
            _nextCheck = 0f;
            Array.Clear(On, 0, On.Length);
            Array.Clear(Failed, 0, Failed.Length);
            Array.Clear(Said, 0, Said.Length);
        }

        // On when nothing stands against it and it has not failed this session; each change of state is logged once.
        private static void Decide(Feature feature, string against)
        {
            int i = (int)feature;
            if (Failed[i])
            {
                On[i] = false;
                return;
            }
            string state = against ?? "on";
            if (state != Said[i])
            {
                Said[i] = state;
                HighSeat.Log.LogInfo(against == null ? $"{Names[i]}: on" : $"{Names[i]}: left to the game, {against}");
            }
            On[i] = against == null;
        }

        private static void Log(string text)
        {
            try
            {
                HighSeat.Log.LogError(text);
            }
            catch (Exception)
            {
                // Already off; only the log line is lost.
            }
        }
    }
}
