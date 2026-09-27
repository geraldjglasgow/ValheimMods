using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;

namespace EarthWright.Protection
{
    /// <summary>
    /// One admin zone: a named circle on the map (X, Z, radius), optionally for one player only. Zones are authored by
    /// the server (EarthWright.Zones.yml and the <c>ew zone</c> commands) and pushed to every player.
    /// </summary>
    public sealed class AdminZone
    {
        public const float MinRadius = 5f;
        public const float MaxRadius = 200f;
        public const int MaxZones = 100;
        public const int MaxNameLength = 32;

        public string Name = "";
        public float X;
        public float Z;
        public float Radius = 20f;

        /// <summary>A character name, or empty for every player.</summary>
        public string Player = "";

        /// <summary>The circle lies wholly inside the zone.</summary>
        public bool Contains(Disc disc) => Distance(disc) + disc.Radius <= Radius;

        /// <summary>The circle reaches into the zone.</summary>
        public bool Touches(Disc disc) => Distance(disc) < Radius + disc.Radius;

        /// <summary>
        /// The zone is meant for this player: it names nobody, or this player. An unknown name (null: the owner of the
        /// ground could not find the sending player) counts as allowed, leaving the decision to the sender's check.
        /// </summary>
        public bool Allows(string playerName)
        {
            return string.IsNullOrEmpty(Player) || playerName == null || string.Equals(Player, playerName, StringComparison.OrdinalIgnoreCase);
        }

        public bool NameIs(string name) => string.Equals(Name, name, StringComparison.OrdinalIgnoreCase);

        public string Describe()
        {
            string text = string.Format(CultureInfo.InvariantCulture, "{0}  ({1:0}, {2:0})  r {3:0} m", Name, X, Z, Radius);
            return string.IsNullOrEmpty(Player) ? text : text + "  [" + Player + "]";
        }

        /// <summary>Letters, digits, '-' and '_', at most 32 characters: one console word and safe in YAML.</summary>
        public static bool ValidName(string name)
        {
            return !string.IsNullOrEmpty(name) && name.Length <= MaxNameLength && name.All(c => char.IsLetterOrDigit(c) || c == '-' || c == '_');
        }

        public static float ClampRadius(float radius) => Mathf.Clamp(radius, MinRadius, MaxRadius);

        private float Distance(Disc disc)
        {
            float dx = disc.Center.x - X;
            float dz = disc.Center.z - Z;
            return Mathf.Sqrt(dx * dx + dz * dz);
        }
    }

    /// <summary>The zones as a flat list of strings, five per zone: the form the standing Charter article carries.</summary>
    public static class ZoneWire
    {
        private const int Fields = 5;

        public static List<string> Encode(IEnumerable<AdminZone> zones)
        {
            List<string> list = new List<string>();
            foreach (AdminZone zone in zones)
            {
                list.Add(zone.Name);
                list.Add(zone.X.ToString("R", CultureInfo.InvariantCulture));
                list.Add(zone.Z.ToString("R", CultureInfo.InvariantCulture));
                list.Add(zone.Radius.ToString("R", CultureInfo.InvariantCulture));
                list.Add(zone.Player ?? "");
            }
            return list;
        }

        public static List<AdminZone> Decode(List<string> list)
        {
            List<AdminZone> zones = new List<AdminZone>();
            for (int i = 0; list != null && i + Fields <= list.Count; i += Fields)
            {
                zones.Add(new AdminZone
                {
                    Name = list[i],
                    X = Number(list[i + 1]),
                    Z = Number(list[i + 2]),
                    Radius = AdminZone.ClampRadius(Number(list[i + 3])),
                    Player = list[i + 4] ?? "",
                });
            }
            return zones;
        }

        private static float Number(string text)
        {
            return float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out float value) ? value : 0f;
        }
    }
}
