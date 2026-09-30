using System.Collections.Generic;
using UnityEngine;

namespace OpenKeep.Homestead
{
    /// <summary>
    /// The choice of bed after death, on the dying player's own client. It opens at death when Bed Choice Seconds is
    /// above 0, the world has a map and there are two beds or more; the nearest is already the spawn point
    /// (<see cref="BedRespawn.Choose"/>). The respawn the game asked for is moved to the end of the choice, so the game
    /// wakes the player in the nearest bed on its own if nothing else happens. A click on a bed makes it the spawn
    /// point and ends the choice; the time running out, the map key or Escape end it with the bed already chosen. At
    /// the end the respawn is asked for again with what is left of the wait for that bed (<see cref="BedWait"/>), so a
    /// near bed wakes the player at once. Active only while the same game and the same player exist and the player is
    /// still dead, so a quit, a respawn or a revival from elsewhere ends it without a call.
    /// </summary>
    public static class BedChoice
    {
        private static readonly List<Vector3> beds = new List<Vector3>();
        private static bool open;
        private static Game game;
        private static Player player;
        private static float deadline;

        public static bool Active => open && game != null && game == Game.instance && player != null
            && player == Player.m_localPlayer && player.IsDead();

        /// <summary>The beds to choose from, nearest to the death first.</summary>
        public static List<Vector3> Beds => beds;

        public static int SecondsLeft => Mathf.CeilToInt(Mathf.Max(0f, deadline - Time.time));

        public static bool TryOpen(Player dead, List<Vector3> candidates, Vector3 deathPoint)
        {
            float seconds = BedSettings.BedChoiceSeconds.Value;
            if (seconds <= 0f || candidates.Count < 2 || Minimap.instance == null || Game.m_noMap)
                return false;
            beds.Clear();
            beds.AddRange(candidates);
            open = true;
            game = Game.instance;
            player = dead;
            deadline = Time.time + seconds;
            BedWait.Schedule(seconds);
            BedChoiceMap.Opened(Minimap.instance, deathPoint);
            Plugin.Log.LogInfo($"OpenKeep: choosing a bed on the map, {beds.Count} beds, {seconds:0.#} s");
            return true;
        }

        public static void Tick()
        {
            if (Time.time >= deadline)
                Confirm();
        }

        /// <summary>A click at <paramref name="point"/> on the map: the bed nearest to it within <paramref name="radius"/> metres is chosen.</summary>
        public static void ClickAt(Vector3 point, float radius)
        {
            int index = -1;
            for (int i = 0; i < beds.Count; i++)
            {
                float metres = BedPoints.MapDistance(beds[i], point);
                if (metres < radius && (index < 0 || metres < BedPoints.MapDistance(beds[index], point)))
                    index = i;
            }
            if (index < 0)
                return;
            BedRespawn.Prefer(beds[index]);
            Confirm();
        }

        /// <summary>Ends the choice with the bed chosen so far and asks for the respawn after what is left of its wait.</summary>
        public static void Confirm()
        {
            if (!Active)
                return;
            Close();
            BedWait.Schedule();
        }

        /// <summary>Ends the choice without a new respawn request (the game's own is running).</summary>
        public static void Close()
        {
            if (!open)
                return;
            open = false;
            BedChoiceMap.Closed();
        }
    }
}
