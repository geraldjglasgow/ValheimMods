using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEngine;

namespace Broadside
{
    /// <summary>A hole in one of a launched ship's hull parts: the part (its index among the ship's blueprint pieces),
    /// where it is on the part's surface and which way that surface faces there (the ship's frame), and its radius.</summary>
    public readonly struct KwHullHole
    {
        public KwHullHole(int part, Vector3 place, Vector3 normal, float radius)
        {
            Part = part;
            Place = place;
            Normal = normal;
            Radius = radius;
        }

        public int Part { get; }
        public Vector3 Place { get; }
        public Vector3 Normal { get; }
        public float Radius { get; }

        /// <summary>The hole's opening, in square metres.</summary>
        public float Area => Mathf.PI * Radius * Radius;

        /// <summary>
        /// The hole's own number, from its part and its place to the millimetre as the ZDO's text keeps them, the same on
        /// every peer and kept as it widens: the seed of its ragged outline (<see cref="KwHoleShape"/>).
        /// </summary>
        public int Id => unchecked(Part * 73856093 ^ Mathf.RoundToInt(Place.x * 1000f) * 19349663
            ^ Mathf.RoundToInt(Place.y * 1000f) * 83492791 ^ Mathf.RoundToInt(Place.z * 1000f) * 668265263);

        /// <summary>The hole as drawn and cut: its number and its size (a widened hole is cut again).</summary>
        public string Key => Id.ToString(CultureInfo.InvariantCulture) + ":" + Radius.ToString("0.###", CultureInfo.InvariantCulture);

        public KwHullHole Grown(float radius) => new KwHullHole(Part, Place, Normal, radius);
    }

    /// <summary>
    /// A launched ship's holes and its hull parts' wear as the ship's ZDO keeps them, plain text the owner writes and every
    /// peer reads: <see cref="HolesKey"/> one hole a ';'-separated entry, "part,x,y,z,nx,ny,nz,radius"; <see cref="WearKey"/>
    /// each worn part's damage since its last hole, "part:damage". An entry that does not read is skipped.
    /// </summary>
    public static class KwHullHoleText
    {
        public const string HolesKey = "kw_holes", WearKey = "kw_hull_wear";
        public static readonly int HolesHash = HolesKey.GetStableHashCode(), WearHash = WearKey.GetStableHashCode();
        private static readonly CultureInfo Plain = CultureInfo.InvariantCulture;

        public static string Write(List<KwHullHole> holes)
        {
            StringBuilder text = new StringBuilder();
            foreach (KwHullHole h in holes)
            {
                if (text.Length > 0)
                    text.Append(';');
                text.Append(h.Part.ToString(Plain)).Append(',').Append(Join(h.Place, "0.###")).Append(',').Append(Join(h.Normal, "0.##"))
                    .Append(',').Append(h.Radius.ToString("0.###", Plain));
            }
            return text.ToString();
        }

        public static List<KwHullHole> Read(string? text)
        {
            List<KwHullHole> holes = new List<KwHullHole>();
            foreach (string entry in (text ?? "").Split(';'))
            {
                string[] f = entry.Split(',');
                if (f.Length != 8 || !int.TryParse(f[0], NumberStyles.Integer, Plain, out int part) || !Numbers(f, 1, out float[] n))
                    continue;
                holes.Add(new KwHullHole(part, new Vector3(n[0], n[1], n[2]), new Vector3(n[3], n[4], n[5]).normalized, n[6]));
            }
            return holes;
        }

        public static string WriteWear(Dictionary<int, float> wear)
        {
            StringBuilder text = new StringBuilder();
            foreach (KeyValuePair<int, float> part in wear)
            {
                if (text.Length > 0)
                    text.Append(';');
                text.Append(part.Key.ToString(Plain)).Append(':').Append(part.Value.ToString("0.#", Plain));
            }
            return text.ToString();
        }

        public static Dictionary<int, float> ReadWear(string? text)
        {
            Dictionary<int, float> wear = new Dictionary<int, float>();
            foreach (string entry in (text ?? "").Split(';'))
            {
                string[] f = entry.Split(':');
                if (f.Length == 2 && int.TryParse(f[0], NumberStyles.Integer, Plain, out int part) && float.TryParse(f[1], NumberStyles.Float, Plain, out float damage))
                    wear[part] = damage;
            }
            return wear;
        }

        private static string Join(Vector3 v, string format) =>
            v.x.ToString(format, Plain) + "," + v.y.ToString(format, Plain) + "," + v.z.ToString(format, Plain);

        // The fields from one on, all numbers.
        private static bool Numbers(string[] fields, int from, out float[] numbers)
        {
            numbers = new float[fields.Length - from];
            for (int i = from; i < fields.Length; i++)
            {
                if (!float.TryParse(fields[i], NumberStyles.Float, Plain, out numbers[i - from]))
                    return false;
            }
            return true;
        }
    }
}
