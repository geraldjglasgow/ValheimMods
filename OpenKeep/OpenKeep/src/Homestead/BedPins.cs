using System.Collections.Generic;
using UnityEngine;

namespace OpenKeep.Homestead
{
    /// <summary>
    /// Every bed of the local character on its map, with the game's bed icon. The game draws one bed icon, at the
    /// profile's spawn point; this adds one for each other known bed (<see cref="BedList"/>), and during the choice of
    /// bed for each bed to choose from even with Beds On Map off. The icons are the game's pins but not saved
    /// (<c>m_save</c> false), which the game's own pin clicks and removal skip; they are added to the pin list
    /// directly, because <c>Minimap.AddPin</c> would switch the bed icons back on in the player's map filter. Checked
    /// once a second, and at once when the spawn point moves, so the two icons never sit on the same bed for long.
    /// </summary>
    public static class BedPins
    {
        private const float CheckSeconds = 1f;

        private static readonly List<Minimap.PinData> pins = new List<Minimap.PinData>();
        private static Minimap owner;
        private static float nextCheck;
        private static Vector3? shownSpawn;

        public static void Refresh(Minimap map)
        {
            if (map != owner)
            {
                pins.Clear();   // those were on a map that is gone
                owner = map;
                nextCheck = 0f;
            }
            Vector3? spawn = SpawnPoint();
            if (Time.time < nextCheck && spawn == shownSpawn)
                return;
            nextCheck = Time.time + CheckSeconds;
            shownSpawn = spawn;
            List<Vector3> wanted = Wanted(spawn);
            if (Differs(map, wanted))
                Replace(map, wanted);
        }

        private static Vector3? SpawnPoint()
        {
            PlayerProfile profile = Game.instance != null ? Game.instance.GetPlayerProfile() : null;
            return profile != null && profile.HaveCustomSpawnPoint() ? profile.GetCustomSpawnPoint() : (Vector3?)null;
        }

        /// <summary>The beds to draw: all but the one the game draws itself.</summary>
        private static List<Vector3> Wanted(Vector3? spawn)
        {
            List<Vector3> beds;
            if (BedChoice.Active)
                beds = new List<Vector3>(BedChoice.Beds);
            else if (BedSettings.NearestBedRespawn.Value && BedSettings.BedsOnMap.Value)
                beds = BedList.Known();
            else
                beds = new List<Vector3>();
            if (spawn.HasValue)
                beds.RemoveAll(p => BedPoints.Same(p, spawn.Value));
            return beds;
        }

        private static bool Differs(Minimap map, List<Vector3> wanted)
        {
            if (wanted.Count != pins.Count)
                return true;
            for (int i = 0; i < pins.Count; i++)
            {
                if (!BedPoints.Same(pins[i].m_pos, wanted[i]) || !map.m_pins.Contains(pins[i]))
                    return true;
            }
            return false;
        }

        private static void Replace(Minimap map, List<Vector3> wanted)
        {
            foreach (Minimap.PinData pin in pins)
                map.RemovePin(pin);
            pins.Clear();
            foreach (Vector3 point in wanted)
                pins.Add(Add(map, point));
        }

        private static Minimap.PinData Add(Minimap map, Vector3 point)
        {
            Minimap.PinData pin = new Minimap.PinData
            {
                m_type = Minimap.PinType.Bed,
                m_name = "",
                m_pos = point,
                m_icon = map.GetSprite(Minimap.PinType.Bed),
                m_save = false,
            };
            map.m_pins.Add(pin);
            map.m_pinUpdateRequired = true;
            return pin;
        }
    }
}
