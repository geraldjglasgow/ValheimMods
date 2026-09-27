using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

namespace EarthWright.Protection
{
    /// <summary>
    /// "Strict Dig Exceptions": the rule <see cref="Terrain.HeightLimits"/> asks whether the dig limit is lifted at a
    /// position. It is within the set radius of a listed object (ore deposits, mud piles, buried treasure) and where tar
    /// covers the ground. The engine asks on the machine that owns the terrain compiler, for every vertex it writes, so
    /// answers are cached per 2 m cell for five seconds (the objects themselves per zone, see <see cref="DigSites"/>).
    /// Never throws: a failure counts as "not lifted".
    /// </summary>
    public static class DigExceptions
    {
        private const float CellSize = 2f;
        private const float Lifetime = 5f;
        private const int MaxCells = 8192;

        private struct Answer
        {
            public float Until;
            public bool Lifted;
        }

        private static readonly Dictionary<long, Answer> cells = new Dictionary<long, Answer>();

        /// <summary>The game's objects and this cache belong to the main thread; another thread gets "not lifted".</summary>
        private static Thread mainThread;

        /// <summary>Called from the plugin's Awake, which runs on the main thread.</summary>
        public static void Initialize() => mainThread = Thread.CurrentThread;

        public static bool Lifts(Vector3 position)
        {
            if (Thread.CurrentThread != mainThread)
                return false;
            try
            {
                return ProtectionSettings.StrictDigExceptions.Value && Cached(position);
            }
            catch (Exception e)
            {
                Plugin.Log.LogError("EarthWright dig exceptions: " + e);
                return false;
            }
        }

        private static bool Cached(Vector3 position)
        {
            float now = Time.time;
            long key = ((long)Mathf.FloorToInt(position.x / CellSize) << 32) ^ (uint)Mathf.FloorToInt(position.z / CellSize);
            if (cells.TryGetValue(key, out Answer answer) && now < answer.Until && answer.Until - now <= Lifetime)
                return answer.Lifted;
            if (cells.Count >= MaxCells)
                cells.Clear();
            bool lifted = Evaluate(position);
            cells[key] = new Answer { Until = now + Lifetime, Lifted = lifted };
            return lifted;
        }

        private static bool Evaluate(Vector3 position)
        {
            if (DigSites.NearListed(position, ProtectionSettings.DigRadius.Value))
                return true;
            return ProtectionSettings.DigInTar.Value && DigSites.InTar(position);
        }
    }
}
