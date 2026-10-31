using System;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace HighSeatPlus
{
    /// <summary>
    /// Building support checked on change (PLAN.md, design 8). WearNTearUpdater calls WearNTear.UpdateWear on about 50
    /// pieces a frame so each comes round about once a second; on the piece's owner it calls UpdateSupport, whose value
    /// the owner writes into the ZDO for everyone else. UpdateSupport has a cache of its own: the pieces under the centre
    /// of mass that held it last time. When none of them moved or changed and the best of them is at least the piece's
    /// own support it returns at once; otherwise it runs the full search (an overlap box per collider bound, a component
    /// lookup per collider found, a closest point or a raycast per supporter). A piece held by the ground with nothing as
    /// strong under it, or touching anything that is not a piece, never passes that cache, so it runs the full search
    /// every second whether anything near it changed or not. Those pieces are what this skips; every other piece keeps
    /// the game's check untouched, which for a piece resting on pieces is that cheap cache check already.
    /// <para>
    /// After the game ran its check, <see cref="SupportScan"/> decides whether the piece qualifies and makes its
    /// <see cref="SupportRecord"/>; before the next check the record decides whether anything UpdateSupport reads has
    /// changed. If nothing has, the call is skipped, which leaves exactly what the call would have left: the same
    /// support, the same cache, no ZDO write. Every input of UpdateSupport, and what makes the game's check run again:
    /// </para>
    /// <list type="bullet">
    /// <item>The game's cache (m_supportColliders, m_supportWearNTear, m_supportPositions, m_supportValue): checked on
    /// every skip with UpdateSupport's own tests (collider there, piece there and supporting, collider not moved, same
    /// GetSupport, which reads the owner's value, the ZDO on other peers or full support for a piece nobody owns). So a
    /// supporter's support changing, its owner changing or it being removed runs the game's check, as it would anyway.</item>
    /// <item>A cache clear from outside the check (RPC_ClearCachedSupport when a neighbour is placed, RPC_HealthChanged on
    /// damage or repair, the ground's own clear after a terrain edit, all through WearNTear.ClearCachedSupport): drops the
    /// record (<see cref="SupportCacheCleared"/>).</item>
    /// <item>The just-placed flag (m_clearCachedSupport, whose check clears the caches round the new piece): never
    /// skipped while set.</item>
    /// <item>The piece's boxes (m_bounds) and colliders: fixed after its first check (the game never updates the boxes
    /// either). Its position and rotation (the centre of mass), its material (its full support) and its last support
    /// value (which decides the ZDO write): compared on every skip.</item>
    /// <item>What the boxes find (layers piece, Default, static_solid, Default_small and terrain, 128 colliders at most):
    /// rigidbody and trigger colliders (players, creatures, items, ships) are skipped by the game and matter only by
    /// filling the buffer, so a box holding more than 64 gets no record. A non-piece (rock, tree, stump, location wall,
    /// mine rock area) gives full support and an empty cache whatever else is there: the record holds while one of
    /// those found is still alive, enabled, active and unmoved (a felled tree, a mined area, an unloaded zone end it).
    /// The ground gives full support and leaves the pieces under the centre of mass cached: the ground colliders found
    /// are kept the same way; a ground rebuild under the boxes (Heightmap.RebuildCollisionMesh or Clear: a terrain edit,
    /// a zone load) and any piece collider that could join the cache (a piece appearing, WearNTear.Start; a worn or
    /// broken look switching, RPC_HealthChanged; a door swinging, Door.SetState) raise the columns under them
    /// (<see cref="SupportCells"/>). Other pieces change an anchored piece's result only through the cache above.</item>
    /// <item>Ownership: UpdateSupport runs only on the owner and nothing in a record depends on who owns what, except
    /// through GetSupport in the cache, which is read live. Health is not read by UpdateSupport at all.</item>
    /// </list>
    /// <para>
    /// Safety: every record runs the game's check again after about <see cref="SafetySeconds"/> (8 to 12 s, spread by
    /// the piece), and a recheck that finds a different support is counted and logged (<see cref="SupportStats"/>). A
    /// piece that qualified but could not get a record (no anchor after all, a crowded box) is looked at again only
    /// after <see cref="RetrySeconds"/>; until then, and for every piece without a record, the game's check runs as
    /// always. No setting: it changes no result. Any error puts every piece back on the game's check for the session.
    /// </para>
    /// </summary>
    [HarmonyPatch(typeof(WearNTear), nameof(WearNTear.UpdateSupport))]
    internal static class SupportSkip
    {
        public const float SafetySeconds = 10f;
        private const float RetrySeconds = 10f;
        private const int Skipping = 0;
        private const int Running = 1;
        private const int Recheck = 2;

        private static readonly Dictionary<int, SupportRecord> Records = new Dictionary<int, SupportRecord>();
        private static bool _broken;

        public static int Count => Records.Count;

        private static bool Prefix(WearNTear __instance, out int __state)
        {
            __state = Running;
            if (_broken)
            {
                return true;
            }
            try
            {
                __state = Decide(__instance);
            }
            catch (Exception e)
            {
                Fail(e);
                __state = Running;
            }
            return __state != Skipping;
        }

        private static void Postfix(WearNTear __instance, int __state)
        {
            if (__state == Skipping || _broken)
            {
                return;
            }
            try
            {
                After(__instance, __state == Recheck);
            }
            catch (Exception e)
            {
                Fail(e);
            }
        }

        private static int Decide(WearNTear piece)
        {
            if (!Records.TryGetValue(piece.GetInstanceID(), out SupportRecord record) || !record.Valid)
            {
                SupportStats.Plain++;
                return Running;
            }
            if (Time.time >= record.ExpiresAt)
            {
                record.Valid = false;
                SupportStats.Rechecked++;
                return Recheck;
            }
            if (!record.Holds(piece))
            {
                record.Valid = false;
                SupportStats.AfterChange++;
                return Running;
            }
            SupportStats.Skipped++;
            return Skipping;
        }

        // Right after the game's own check: a new record when the piece qualifies, nothing otherwise.
        private static void After(WearNTear piece, bool recheck)
        {
            Records.TryGetValue(piece.GetInstanceID(), out SupportRecord record);
            if ((record != null && !Release(piece, record, recheck)) || !SupportScan.Worth(piece))
            {
                return;
            }
            if (SupportCells.Settling(piece.m_bounds))
            {
                return;
            }
            record ??= Add(piece);
            if (!SupportScan.Look(piece) || !SupportScan.Fill(piece, record))
            {
                record.RetryAt = Time.time + RetrySeconds;
                return;
            }
            record.Valid = true;
            record.ExpiresAt = Time.time + Interval(piece);
        }

        // The old record lets go of what it held, after counting a safety recheck that found a change nothing announced;
        // false while a piece that could not have a record waits before it is looked at again.
        private static bool Release(WearNTear piece, SupportRecord record, bool recheck)
        {
            if (recheck && !piece.m_support.Equals(record.Support))
            {
                SupportStats.Miss(piece, record.Support);
            }
            record.Clear();
            return Time.time >= record.RetryAt;
        }

        private static SupportRecord Add(WearNTear piece)
        {
            SupportRecord record = new SupportRecord();
            Records[piece.GetInstanceID()] = record;
            return record;
        }

        // SafetySeconds, 0.8 to 1.2 times, the same for a piece every time, so pieces made at once recheck apart.
        private static float Interval(WearNTear piece)
        {
            uint spread = ((uint)piece.GetInstanceID() * 2654435761u) >> 16;
            return SafetySeconds * (0.8f + 0.4f * (spread & 0xFFFF) / 65535f);
        }

        /// <summary>The game cleared the piece's cache outside its check: the next check is the game's.</summary>
        public static void Drop(WearNTear piece)
        {
            if (Records.TryGetValue(piece.GetInstanceID(), out SupportRecord record))
            {
                record.Valid = false;
            }
        }

        public static void Forget(WearNTear piece) => Records.Remove(piece.GetInstanceID());

        public static void Reset() => Records.Clear();

        /// <summary>Something went wrong: every piece goes back to the game's check for the rest of the session.</summary>
        public static void Fail(Exception e)
        {
            if (_broken)
            {
                return;
            }
            _broken = true;
            HighSeatPlus.Log.LogWarning($"building support is back to the game's own check every second after an error: {e}");
        }
    }
}
