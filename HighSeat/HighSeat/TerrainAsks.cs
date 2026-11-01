using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace HighSeat
{
    /// <summary>
    /// How many zones the server may ask of the game's terrain builder and make, on the server while it runs creatures,
    /// shared by every pass that makes zones round players: their ground (<see cref="PlayerGround"/>) and the game's own
    /// ghost zones that generate new land round them (<see cref="GhostZones"/>). The builder is one thread that keeps only
    /// its last 16 finished heightmaps, and a zone being made takes its heightmap from there or waits for it behind
    /// everything queued: asked for every missing zone round 50 players spread over the world, it queued over a thousand,
    /// threw finished ones away before they were taken, and a zone made then waited 4-40 s (measured 2026-11-01). So at
    /// most MaxAsked zones wait at a time, none are added while the builder's queue is MaxQueue long, and a tick (ten a
    /// second) makes at most MaxBuilds zones, the rest once BudgetMs is spent. A zone no pass asked about in the last tick
    /// (made, generated, or out of every player's area) stops counting.
    /// </summary>
    internal static class TerrainAsks
    {
        private const int MaxAsked = 8;
        private const int MaxQueue = 8;
        private const int MaxBuilds = 4;
        private const double BudgetMs = 3;

        private static readonly double TicksPerMs = Stopwatch.Frequency / 1000.0;
        private static readonly HashSet<Vector2s> Asked = new HashSet<Vector2s>();
        private static readonly HashSet<Vector2s> Wanted = new HashSet<Vector2s>();
        private static readonly Predicate<Vector2s> Unwanted = zone => !Wanted.Contains(zone);
        private static int _room;
        private static int _builds;
        private static long _start;

        /// <summary>The start of a tick of the game's ZoneSystem (from <see cref="ZoneKeeping"/>, before its ghost zones).</summary>
        public static void BeginTick()
        {
            Asked.RemoveWhere(Unwanted);
            Wanted.Clear();
            _room = MaxQueue - BuilderQueue();
            _builds = 0;
            _start = Stopwatch.GetTimestamp();
        }

        /// <summary>True when the zone may be made now: asked before (its heightmap may be ready), or a new ask fits.</summary>
        public static bool MayMake(Vector2s zone)
        {
            if (Asked.Contains(zone))
            {
                Wanted.Add(zone);
                return MayBuild();
            }
            if (!MayBuild() || Asked.Count >= MaxAsked || _room <= 0)
            {
                return false;
            }
            Asked.Add(zone);
            Wanted.Add(zone);
            _room--;
            return true;
        }

        /// <summary>One zone made this tick.</summary>
        public static void Made() => _builds++;

        private static bool MayBuild()
        {
            if (_builds == 0)
            {
                return true;
            }
            return _builds < MaxBuilds && (Stopwatch.GetTimestamp() - _start) / TicksPerMs < BudgetMs;
        }

        // What the terrain builder still has to build, everyone's asks together.
        private static int BuilderQueue()
        {
            HeightmapBuilder builder = HeightmapBuilder.instance;
            if (builder == null)
            {
                return 0;
            }
            lock (builder.m_lock)
            {
                return builder.m_toBuild.Count;
            }
        }
    }
}
