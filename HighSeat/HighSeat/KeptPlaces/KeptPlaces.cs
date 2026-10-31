using System;
using System.Collections.Generic;
using UnityEngine;

namespace HighSeat
{
    /// <summary>
    /// Kept places on the server: the busiest places (<see cref="BusyPlaces"/>) and fresh death spots
    /// (<see cref="DeathSpots"/>) keep their ground and still objects loaded while no player is there, creatures and
    /// everything else that moves unloaded as today. A player arriving there finds the ground and buildings in, so the
    /// creatures start at once; otherwise the server builds a player's ground one zone a pass after a jump and makes
    /// nothing where it is missing. Two calls drive it: <see cref="AddStill"/> from <see cref="PlayerAreas"/>
    /// (ZNetScene.CreateDestroyObjects, 30 times a second) adds the held objects to the game's near list
    /// (<see cref="KeptSectors"/>), and <see cref="KeepGround"/> from <see cref="ZoneKeeping"/> (ZoneSystem.UpdateTTL's
    /// prefix, ten times a second) keeps their ground (<see cref="KeptGround"/>) and, every TrackSeconds, counts where
    /// the players are and chooses the places again (<see cref="KeptZones"/>; at once for a new tombstone). A place a
    /// player is in costs nothing extra: their own area already loads it, and it stays loaded when they leave. Only on
    /// the server while it runs creatures and the setting is on. While the server is over its frame budget, and for a
    /// calm time after (<see cref="KeptCalm"/>), nothing is kept: the places are let go one zone a pass, objects first
    /// and ground after, as the game lets an area go. A failure here stops kept places for the session and leaves the
    /// game's own loading running; a new world starts from nothing.
    /// </summary>
    internal static class KeptPlaces
    {
        private const float TrackSeconds = 10f;

        private static ZDOMan _world;
        private static bool _broken;
        private static bool _rebuild;
        private static float _counted;

        /// <summary>True while the server takes note of new places: it runs creatures and the setting is on.</summary>
        public static bool Noticing => Ready() && Settings.KeptPlaces.Value;

        /// <summary>
        /// Adds to <paramref name="near"/> the still objects of every kept place in a sector not in
        /// <paramref name="visited"/> yet, marking those sectors visited. Creatures and other moving objects are left out.
        /// </summary>
        public static void AddStill(List<ZDO> near, HashSet<ZoneSystem.SectorIndex> visited)
        {
            try
            {
                if (!Ready())
                {
                    return;
                }
                if (!KeptCalm.Working())
                {
                    KeptZones.LetGo();
                    _rebuild = true;
                }
                KeptSectors.Add(near, visited);
            }
            catch (Exception e)
            {
                Fail(e);
            }
        }

        /// <summary>Pokes, or builds, the ground of every kept place, so the game does not let it go.</summary>
        public static void KeepGround(ZoneSystem zones)
        {
            try
            {
                if (!Ready())
                {
                    return;
                }
                bool working = KeptCalm.Working();
                if (Settings.KeptPlaces.Value)
                {
                    Count(working);
                }
                KeptGround.Keep(zones, working);
            }
            catch (Exception e)
            {
                Fail(e);
            }
        }

        /// <summary>Asks for the kept zones to be chosen again at the next ground pass (a new death spot).</summary>
        public static void Changed() => _rebuild = true;

        // On the server while it runs creatures and nothing failed; a new world starts from nothing.
        private static bool Ready()
        {
            if (!ServerRun.Active || ZDOMan.instance == null || ZoneSystem.instance == null || ZNetScene.instance == null)
            {
                return false;
            }
            if (!ReferenceEquals(_world, ZDOMan.instance))
            {
                Reset(ZDOMan.instance);
            }
            return !_broken;
        }

        // Every TrackSeconds: the players' time, the death spots, the busy places; the kept zones too when due or asked.
        private static void Count(bool working)
        {
            float now = Time.unscaledTime;
            bool due = now - _counted >= TrackSeconds;
            if (due)
            {
                BusyPlaces.Credit(Mathf.Min(now - _counted, TrackSeconds * 2f));
                _counted = now;
                DeathSpots.Expire();
                BusyPlaces.Pick();
            }
            if (working && (due || _rebuild))
            {
                _rebuild = false;
                KeptZones.Rebuild();
                KeptSectors.Sync();
            }
        }

        private static void Reset(ZDOMan world)
        {
            _world = world;
            _broken = false;
            _rebuild = false;
            _counted = Time.unscaledTime;
            KeptCalm.Reset();
            BusyPlaces.Reset();
            DeathSpots.Reset();
            KeptZones.Reset();
            KeptSectors.Reset();
        }

        // Logged once: from here on nothing is held, so the game unloads the places as it unloads any area.
        private static void Fail(Exception e)
        {
            _broken = true;
            HighSeat.Log.LogWarning($"kept places stopped for this session, the game's own loading goes on: {e}");
        }
    }
}
