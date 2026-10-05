using System;
using System.Collections.Generic;
using OpenKeep.Core;
using UnityEngine;

namespace OpenKeep.Reach
{
    /// <summary>
    /// The containers within reach of the local player, nearest first: one list per frame, and the same list instance
    /// for as long as its members stay the same (the panels ask for every row every frame). Two steps. The candidates
    /// are the containers within the widest range plus a margin that the section 0 rules allow (switches, prefab
    /// table, privacy, the ward scan: <see cref="ContainerScan.IsAllowed"/>); they are found again every quarter
    /// second, and sooner when a container woke, the reach rules were applied, the local player changed, the player
    /// moved further than the margin or the widest range grew. Every frame the candidates are narrowed by the checks
    /// that change from moment to moment (still there, inventory read, not open by another player:
    /// <see cref="ContainerScan.IsReady"/>) and by each prefab's range from where the player stands now, so leaving a
    /// container's range or another player opening it counts at once. A refresh of the candidates always gives a new
    /// list, so <see cref="StorageIndex"/> counts afresh at least every quarter second even when nothing reported a change.
    /// </summary>
    public static class ReachChests
    {
        private const float RefreshSeconds = 0.25f;
        private const float Margin = 4f;

        private static readonly Comparison<KeyValuePair<float, Container>> Nearest = (a, b) => a.Key.CompareTo(b.Key);
        private static readonly List<KeyValuePair<float, Container>> scratch = new List<KeyValuePair<float, Container>>();

        private static List<Container> candidates = new List<Container>();
        private static Player candidatesFor;
        private static Vector3 candidatesFrom;
        private static float candidatesRange;
        private static float candidatesAt;
        private static int candidatesRegistered = -1;
        private static bool stale = true;
        private static bool refreshed;

        private static int frame = -1;
        private static List<Container> current = new List<Container>();

        /// <summary>The candidates are found again on the next call, in this frame too (the reach rules were applied).</summary>
        public static void Invalidate()
        {
            stale = true;
            frame = -1;
        }

        /// <summary>This frame's reachable containers, nearest first. A returned list is never changed afterwards.</summary>
        public static List<Container> List()
        {
            if (Time.frameCount == frame)
                return current;
            frame = Time.frameCount;
            Player player = Player.m_localPlayer;
            if (player == null)
            {
                stale = true;
                if (current.Count > 0)
                    current = new List<Container>();
                return current;
            }
            Vector3 position = player.transform.position;
            if (CandidatesExpired(player, position))
                FindCandidates(player, position);
            current = Narrow(position);
            return current;
        }

        private static bool CandidatesExpired(Player player, Vector3 position)
        {
            if (stale || player != candidatesFor || candidatesRegistered != ContainerScan.Registered)
                return true;
            if (Time.unscaledTime - candidatesAt >= RefreshSeconds || Vector3.Distance(position, candidatesFrom) > Margin)
                return true;
            return ReachRules.MaxRange() + Margin > candidatesRange;
        }

        private static void FindCandidates(Player player, Vector3 position)
        {
            candidatesRange = ReachRules.MaxRange() + Margin;
            candidates = ContainerScan.Allowed(position, candidatesRange);
            candidatesFor = player;
            candidatesFrom = position;
            candidatesAt = Time.unscaledTime;
            candidatesRegistered = ContainerScan.Registered;
            stale = false;
            refreshed = true;
        }

        /// <summary>The candidates in reach from here now, nearest first; the current list again when its members did not change.</summary>
        private static List<Container> Narrow(Vector3 position)
        {
            scratch.Clear();
            foreach (Container container in candidates)
                TryAdd(container, position);
            scratch.Sort(Nearest);
            if (!refreshed && SameAsCurrent())
                return current;
            refreshed = false;
            List<Container> list = new List<Container>(scratch.Count);
            foreach (KeyValuePair<float, Container> pair in scratch)
                list.Add(pair.Value);
            return list;
        }

        private static void TryAdd(Container container, Vector3 position)
        {
            try
            {
                if (container == null || !ContainerScan.IsReady(container))
                    return;
                float distance = Vector3.Distance(position, container.transform.position);
                if (distance <= ReachRules.RangeFor(container))
                    scratch.Add(new KeyValuePair<float, Container>(distance, container));
            }
            catch (Exception e)
            {
                Plugin.Log.LogDebug($"OpenKeep: container skipped, {e.GetType().Name}: {e.Message}");
            }
        }

        private static bool SameAsCurrent()
        {
            if (scratch.Count != current.Count)
                return false;
            for (int i = 0; i < scratch.Count; i++)
            {
                if (!ReferenceEquals(scratch[i].Value, current[i]))
                    return false;
            }
            return true;
        }
    }
}
