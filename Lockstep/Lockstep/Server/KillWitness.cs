using System;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace Lockstep
{
    /// <summary>
    /// Server side: a boss kill is credited only once the server's own copy of the world agrees a boss died. The owner's
    /// report names the boss's ZDO; the server accepts it when that ZDO is (or was, a moment ago) a boss of the reported
    /// stage and the sender owned it or stood near it. Credit then waits for the boss's ZDO to be destroyed, which the
    /// game does at the end of every death. The report goes out before the destroy on the same connection, so it
    /// normally arrives first and waits a frame; a destroy that came first is remembered for <see cref="Keep"/> seconds.
    /// </summary>
    internal static class KillWitness
    {
        /// <summary>Seconds a destroyed boss, or a report still waiting for its boss to be destroyed, is remembered.</summary>
        private const float Keep = 60f;

        /// <summary>A sender this close had the boss loaded; wider than any loaded area, so only a far-away sender fails.</summary>
        private const float NearRange = 300f;

        private struct Sighting
        {
            public int Prefab;
            public long Owner;
            public Vector3 Position;
            public float Time;
        }

        private sealed class Report
        {
            public string Key;
            public string Attackers;
            public float Time;
        }

        private static readonly Dictionary<ZDOID, Sighting> Destroyed = new Dictionary<ZDOID, Sighting>();
        private static readonly Dictionary<ZDOID, Report> Waiting = new Dictionary<ZDOID, Report>();
        private static readonly Dictionary<int, bool> BossPrefabs = new Dictionary<int, bool>();
        private static readonly List<ZDOID> Expired = new List<ZDOID>();

        /// <summary>Forgets everything when the world is left.</summary>
        internal static void Clear()
        {
            Destroyed.Clear();
            Waiting.Clear();
            BossPrefabs.Clear();
        }

        /// <summary>A new chain may make other prefabs bosses; reports already waiting keep their stage key.</summary>
        internal static void ChainChanged() => BossPrefabs.Clear();

        /// <summary>A kill report from the boss's owner: checked, then credited now (boss already gone) or on its destroy.</summary>
        internal static void Reported(long sender, ZDOID boss, Stage stage, string attackers)
        {
            Prune();
            string problem = Problem(sender, boss, stage, out Sighting seen, out bool gone);
            if (problem != null)
            {
                Lockstep.Log.LogWarning($"Refused a {stage.Name} kill report from peer {sender}: {problem}.");
            }
            else if (gone)
            {
                Destroyed.Remove(boss);
                ProgressServer.Credit(stage, attackers, seen.Position);
            }
            else
            {
                Waiting[boss] = new Report { Key = stage.Key, Attackers = attackers, Time = Time.time };
            }
        }

        /// <summary>Why the report cannot be true, or null when it can.</summary>
        private static string Problem(long sender, ZDOID boss, Stage stage, out Sighting seen, out bool gone)
        {
            gone = false;
            ZDO zdo = boss.IsNone() ? null : ZDOMan.instance.GetZDO(boss);
            if (zdo != null)
                seen = Sight(zdo);
            else if (!(gone = Destroyed.TryGetValue(boss, out seen)))
                return "the server knows no such object";
            if (!IsBossOf(seen.Prefab, stage))
                return "that object is not this stage's boss";
            if (seen.Owner != sender && !Near(sender, seen.Position))
                return "the sender neither owned the boss nor stood near it";
            return null;
        }

        /// <summary>A ZDO the server is about to destroy: a waiting report is credited, a boss is remembered for a late report.</summary>
        internal static void Destroying(ZDO zdo)
        {
            if (Waiting.TryGetValue(zdo.m_uid, out Report report))
            {
                Waiting.Remove(zdo.m_uid);
                Stage stage = Chain.ByKey(report.Key);
                if (stage != null)
                    ProgressServer.Credit(stage, report.Attackers, zdo.GetPosition());
            }
            else if (IsAnyBoss(zdo.GetPrefab()))
            {
                Prune();
                Destroyed[zdo.m_uid] = Sight(zdo);
            }
        }

        private static Sighting Sight(ZDO zdo) =>
            new Sighting { Prefab = zdo.GetPrefab(), Owner = zdo.GetOwner(), Position = zdo.GetPosition(), Time = Time.time };

        /// <summary>The stage's boss prefab, or any creature whose death sets the stage's key (a variant another mod adds).</summary>
        private static bool IsBossOf(int prefab, Stage stage) =>
            prefab == stage.BossPrefab.GetStableHashCode() || string.Equals(DefeatKey(prefab), stage.Key, StringComparison.OrdinalIgnoreCase);

        /// <summary>Whether the prefab is the boss of any stage; worked out once per prefab, cleared with the chain.</summary>
        private static bool IsAnyBoss(int prefab)
        {
            if (BossPrefabs.TryGetValue(prefab, out bool boss))
                return boss;
            if (ZNetScene.instance == null)
                return false;
            boss = false;
            foreach (Stage stage in Chain.Stages)
                boss |= IsBossOf(prefab, stage);
            BossPrefabs[prefab] = boss;
            return boss;
        }

        private static string DefeatKey(int prefab)
        {
            GameObject go = ZNetScene.instance != null ? ZNetScene.instance.GetPrefab(prefab) : null;
            Character character = go != null ? go.GetComponent<Character>() : null;
            return character != null ? character.m_defeatSetGlobalKey : null;
        }

        private static bool Near(long sender, Vector3 position)
        {
            Vector3? at = sender == ZDOMan.GetSessionID() ? ZNet.instance.GetReferencePosition() : ZNet.instance.GetPeer(sender)?.m_refPos;
            return at.HasValue && (at.Value - position).sqrMagnitude <= NearRange * NearRange;
        }

        /// <summary>Drops what is older than <see cref="Keep"/>; runs only on a report or a boss's destroy.</summary>
        private static void Prune()
        {
            float now = Time.time;
            Expired.Clear();
            foreach (KeyValuePair<ZDOID, Sighting> pair in Destroyed)
                if (now - pair.Value.Time > Keep) Expired.Add(pair.Key);
            foreach (KeyValuePair<ZDOID, Report> pair in Waiting)
            {
                if (now - pair.Value.Time <= Keep) continue;
                Expired.Add(pair.Key);
                Lockstep.Log.LogWarning($"A {pair.Value.Key} kill report expired: its boss was never destroyed, nobody was credited.");
            }
            foreach (ZDOID id in Expired)
            {
                Destroyed.Remove(id);
                Waiting.Remove(id);
            }
        }
    }

    /// <summary>Every destroyed object passes here on every peer; off the server it leaves on one test.</summary>
    [HarmonyPatch(typeof(ZDOMan), nameof(ZDOMan.HandleDestroyedZDO))]
    public static class ZdoDestroyedPatch
    {
        [HarmonyPrefix]
        public static void Prefix(ZDOMan __instance, ZDOID uid)
        {
            if (!ProgressServer.IsServer)
                return;
            try
            {
                ZDO zdo = __instance.GetZDO(uid);
                if (zdo != null)
                    KillWitness.Destroying(zdo);
            }
            catch (Exception e)
            {
                // never let a credit failure stop the game's own destroy
                Lockstep.Log.LogError($"Boss kill check failed: {e}");
            }
        }
    }
}
