using System;
using System.Collections.Generic;
using System.Text;

namespace HighSeat
{
    /// <summary>
    /// The busiest places: where players spend their time. At every count (<see cref="KeptPlaces"/>) each player's zone -
    /// every ready player's, and the host's own on a hosted game - is credited with the time since the last count, and
    /// old time fades: a zone's time halves every HalfLifeHours of play (time with a player on, so an empty server
    /// forgets nothing). The busy places are the top Count zones with at least MinMinutes each; a zone inside a place
    /// already chosen (<see cref="KeptZones.PlaceRadius"/> round its zone) makes no other, and a place kept already counts
    /// Stickiness times its time, so two places nearly level do not swap back and forth (each swap loads one place and
    /// unloads another). A place a player stands in stays chosen: it costs nothing while they are there, and its objects
    /// stay loaded when they leave. At most MaxTracked zones are remembered; past that the least visited are forgotten,
    /// below ForgetSeconds first. Time is kept inflated - a second credited now weighs 2^(play / half-life) - so fading
    /// touches no zone, and the table is scaled back once that weight passes Rescale. Kept in memory only: after a
    /// restart the places are found again as players play.
    /// </summary>
    internal static class BusyPlaces
    {
        private const int Count = 4;
        private const double HalfLifeHours = 24.0;
        private const double MinMinutes = 10.0;
        private const double Stickiness = 1.25;
        private const int MaxTracked = 4096;
        private const double ForgetSeconds = 60.0;
        private const double Rescale = 1e9;

        private static readonly Dictionary<Vector2s, double> Inflated = new Dictionary<Vector2s, double>();
        private static readonly List<Vector2s> Chosen = new List<Vector2s>();
        private static readonly List<Vector2s> Before = new List<Vector2s>();
        private static readonly List<Vector2s> Keys = new List<Vector2s>();
        private static double _play;

        /// <summary>The busy places, busiest first: the zone at the middle of each.</summary>
        public static List<Vector2s> Places => Chosen;

        private static double Weight => Math.Pow(2.0, _play / (HalfLifeHours * 3600.0));

        public static void Reset()
        {
            Inflated.Clear();
            Chosen.Clear();
            Before.Clear();
            _play = 0.0;
        }

        /// <summary>Credits every player's zone with <paramref name="seconds"/>.</summary>
        public static void Credit(float seconds)
        {
            double amount = seconds * Weight;
            bool anyone = false;
            foreach (ZNetPeer peer in ZNet.instance.GetPeers())
            {
                if (peer.IsReady())
                {
                    Add(ZoneSystem.GetZone(peer.GetRefPos()), amount);
                    anyone = true;
                }
            }
            if (!ZNet.instance.IsDedicated() && Player.m_localPlayer != null)
            {
                Add(ZoneSystem.GetZone(Player.m_localPlayer.transform.position), amount);
                anyone = true;
            }
            if (anyone)
            {
                _play += seconds;
            }
            Tidy();
        }

        private static void Add(Vector2s zone, double amount)
        {
            Inflated.TryGetValue(zone, out double had);
            Inflated[zone] = had + amount;
        }

        // Scales the table back once the weight is large; past MaxTracked forgets the least visited zones, down to three
        // quarters of it.
        private static void Tidy()
        {
            double weight = Weight;
            if (weight > Rescale)
            {
                ScaleAll(1.0 / weight);
                _play = 0.0;
                weight = 1.0;
            }
            if (Inflated.Count <= MaxTracked)
            {
                return;
            }
            for (double cut = ForgetSeconds * weight; Inflated.Count > MaxTracked * 3 / 4; cut *= 2.0)
            {
                DropBelow(cut);
            }
        }

        private static void ScaleAll(double factor)
        {
            Keys.Clear();
            Keys.AddRange(Inflated.Keys);
            foreach (Vector2s zone in Keys)
            {
                Inflated[zone] *= factor;
            }
            Keys.Clear();
        }

        private static void DropBelow(double cut)
        {
            Keys.Clear();
            foreach (KeyValuePair<Vector2s, double> pair in Inflated)
            {
                if (pair.Value < cut)
                {
                    Keys.Add(pair.Key);
                }
            }
            foreach (Vector2s zone in Keys)
            {
                Inflated.Remove(zone);
            }
            Keys.Clear();
        }

        /// <summary>Chooses the busy places again.</summary>
        public static void Pick()
        {
            Before.Clear();
            Before.AddRange(Chosen);
            Chosen.Clear();
            double least = MinMinutes * 60.0 * Weight;
            while (Chosen.Count < Count && Best(least, out Vector2s best))
            {
                Chosen.Add(best);
            }
            if (Changed())
            {
                Report();
            }
        }

        // The busiest zone with at least the least time outside every chosen place; a place kept already counts
        // Stickiness times its time.
        private static bool Best(double least, out Vector2s best)
        {
            best = default;
            double top = 0.0;
            foreach (KeyValuePair<Vector2s, double> pair in Inflated)
            {
                double value = Before.Contains(pair.Key) ? pair.Value * Stickiness : pair.Value;
                if (pair.Value >= least && value > top && !InChosen(pair.Key))
                {
                    best = pair.Key;
                    top = value;
                }
            }
            return top > 0.0;
        }

        private static bool InChosen(Vector2s zone)
        {
            foreach (Vector2s place in Chosen)
            {
                int radius = KeptZones.PlaceRadius;
                if (Math.Abs(zone.x - place.x) <= radius && Math.Abs(zone.y - place.y) <= radius)
                {
                    return true;
                }
            }
            return false;
        }

        private static bool Changed()
        {
            if (Before.Count != Chosen.Count)
            {
                return true;
            }
            foreach (Vector2s place in Chosen)
            {
                if (!Before.Contains(place))
                {
                    return true;
                }
            }
            return false;
        }

        // Logged only when the places change, which is rare: time fades over a day.
        private static void Report()
        {
            StringBuilder text = new StringBuilder("kept places: busiest zones now");
            double weight = Weight;
            foreach (Vector2s place in Chosen)
            {
                text.Append($" ({place.x},{place.y}) {Inflated[place] / weight / 3600.0:0.0} h;");
            }
            if (Chosen.Count == 0)
            {
                text.Append(" none");
            }
            HighSeat.Log.LogInfo(text.ToString());
        }
    }
}
