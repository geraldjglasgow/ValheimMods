using System.Collections;
using System.Collections.Generic;
using EliteCreaturesReborn.Config;
using EliteCreaturesReborn.Util;
using PatchGuard;
using UnityEngine;

namespace EliteCreaturesReborn.Hints
{
    /// <summary>
    /// Boss hints over the game's routed-RPC bus. The boss's owner, as it dies, asks the server - the one machine whose
    /// world knows where every location is - naming the boss and where it fell. The server takes that boss's altar nearest
    /// the fall (the one it was summoned at; the fall itself when no altar of its kind is near, as for a boss spawned by
    /// command), finds the target nearest that altar, and sends the compass direction between the two to every machine.
    /// Each player near the fall with hints on shows it a few seconds later, after the game's and the tier's messages. A
    /// world without the target (one generated before that place came to the game) says nothing.
    /// </summary>
    internal static class BossHintRpc
    {
        public const string Ask = "ecr_boss_hint_ask";
        public const string Hint = "ecr_boss_hint";

        /// <summary>Metres from the fall within which a player feels the pull: the fight, as for the boss trophies.</summary>
        private const float PlayerRange = 100f;

        /// <summary>Metres from the fall within which an altar counts as the one the boss was summoned at.</summary>
        private const float AltarReach = 500f;

        /// <summary>Seconds in which another fall of the same boss at the same place says nothing: a Twin's partner.</summary>
        private const double RepeatSeconds = 30.0;

        /// <summary>Seconds from the fall to the message, so it is not drawn over the tier's or the game's own.</summary>
        private const float ShowDelay = 6f;

        private static readonly Dictionary<string, double> LastSent = new Dictionary<string, double>();
        private static ZRoutedRpc? _registeredOn;

        /// <summary>Registers both handlers once per routed-RPC bus. Called at world start on every machine.</summary>
        public static void EnsureRegistered()
        {
            ZRoutedRpc bus = ZRoutedRpc.instance;
            if (bus == null || ReferenceEquals(bus, _registeredOn))
            {
                return;
            }
            _registeredOn = bus;
            LastSent.Clear();
            bus.Register<string, Vector3>(Ask, OnAsk);
            bus.Register<Vector3, int>(Hint, OnHint);
        }

        /// <summary>Owner side, as a boss dies: asks the server for its hint, when the boss leaves one.</summary>
        public static void Request(string boss, Vector3 fell)
        {
            if (ZRoutedRpc.instance == null || !BossHintTable.TryGet(boss, out _))
            {
                return;
            }
            EnsureRegistered();
            ZRoutedRpc.instance.InvokeRoutedRPC(Ask, boss, fell); // no target: the server, delivered locally on a host
        }

        private static void OnAsk(long sender, string boss, Vector3 fell) =>
            Guard.Run("BossHintRpc.OnAsk", () => Resolve(boss, fell));

        private static void Resolve(string boss, Vector3 fell)
        {
            ZoneSystem zones = ZoneSystem.instance;
            if (ZNet.instance == null || !ZNet.instance.IsServer() || zones == null
                || !BossHintTable.TryGet(boss, out BossHintTable.Entry entry))
            {
                return;
            }
            Vector3 origin = Origin(zones, entry.Altar, fell);
            if (!zones.FindClosestLocation(entry.Target, origin, out ZoneSystem.LocationInstance target))
            {
                Log.Info($"boss hint: this world has no {entry.Target}, so {boss} leaves no hint");
                return;
            }
            if (!Repeat(boss, origin))
            {
                int sector = BossHintTable.Sector(origin, target.m_position);
                ZRoutedRpc.instance.InvokeRoutedRPC(ZRoutedRpc.Everybody, Hint, fell, sector);
            }
        }

        private static Vector3 Origin(ZoneSystem zones, string altar, Vector3 fell)
        {
            bool found = zones.FindClosestLocation(altar, fell, out ZoneSystem.LocationInstance near);
            return found && Utils.DistanceXZ(near.m_position, fell) <= AltarReach ? near.m_position : fell;
        }

        // Server side. True when this boss's hint went out from the same zone moments ago; otherwise stamps it now.
        private static bool Repeat(string boss, Vector3 origin)
        {
            string key = boss + "@" + ZoneSystem.GetZone(origin);
            double now = ZNet.instance.GetTimeSeconds();
            if (LastSent.TryGetValue(key, out double last) && now - last < RepeatSeconds)
            {
                return true;
            }
            LastSent[key] = now;
            return false;
        }

        private static void OnHint(long sender, Vector3 fell, int sector) =>
            Guard.Run("BossHintRpc.OnHint", () => Show(fell, sector));

        // A dedicated server has no HUD and skips this, as does a player far from the fall or with hints off.
        private static void Show(Vector3 fell, int sector)
        {
            Player player = Player.m_localPlayer;
            if (MessageHud.instance == null || player == null || !Configuration.BossHints.Value
                || Utils.DistanceXZ(player.transform.position, fell) > PlayerRange)
            {
                return;
            }
            string text = $"You feel a presence pulling you... {BossHintTable.Word(sector)}";
            MessageHud.instance.StartCoroutine(ShowLater(text));
        }

        private static IEnumerator ShowLater(string text)
        {
            yield return new WaitForSeconds(ShowDelay);
            if (MessageHud.instance != null)
            {
                MessageHud.instance.ShowMessage(MessageHud.MessageType.Center, text);
            }
        }
    }
}
