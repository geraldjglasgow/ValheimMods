using System.Collections.Generic;
using EliteCrafting.Core;
using EliteCrafting.Rules;
using EliteCrafting.Text;
using UnityEngine;

namespace EliteCrafting.Salvage
{
    /// <summary>
    /// <c>salvage.stations</c> (salvage.md section 7, check 9): station prefab names resolved to the game's station
    /// names, and the game's own local in-range test (<c>CraftingStation.HaveBuildStationInRange</c>), as vanilla
    /// crafting does. A name matching no piece prefab is warned about once (modpacks) and never matches. Local client.
    /// </summary>
    internal static class SalvageStations
    {
        private static readonly Dictionary<string, string?> Resolved = new Dictionary<string, string?>(System.StringComparer.Ordinal);

        /// <summary>Drops the resolved names (rules changed).</summary>
        public static void Forget() => Resolved.Clear();

        /// <summary>True when the list is empty or one of its stations is in its own build range of the player.</summary>
        public static bool InRange(IReadOnlyList<string> stations, Vector3 position)
        {
            if (stations.Count == 0)
            {
                return true;
            }
            foreach (string prefab in stations)
            {
                string? name = StationName(prefab);
                if (name != null && CraftingStation.HaveBuildStationInRange(name, position) != null)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>The first listed station's display name, for <c>salvage_station</c>.</summary>
        public static string FirstName(IReadOnlyList<string> stations)
        {
            string? name = stations.Count > 0 ? StationName(stations[0]) : null;
            return name != null ? Words.Localize(name) : stations.Count > 0 ? stations[0] : "";
        }

        private static string? StationName(string prefab)
        {
            if (Resolved.TryGetValue(prefab, out string? known))
            {
                return known;
            }
            if (ZNetScene.instance == null)
            {
                return null;   // no world yet: ask again next time, no cache, no warning
            }
            GameObject? go = ZNetScene.instance.GetPrefab(prefab);
            CraftingStation? station = go != null ? go.GetComponent<CraftingStation>() : null;
            if (station == null)
            {
                Log.Warn($"economy: salvage.stations: '{prefab}' matches no station prefab; it never counts as in range");
            }
            Resolved[prefab] = station?.m_name;
            return station?.m_name;
        }
    }
}
