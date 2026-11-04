using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// One dock section's shape as its ZDO saves it (<see cref="Key"/>), so every peer and every reload builds the same:
    /// <c>pier1|length|width|joined|seed|l,r;l,r;...</c>. Its length and deck width in metres; joined 1 when its start meets
    /// another dock (no pilings there: that dock's stand under the joint); a seed for its boards' joints and grain; and, at
    /// each station (evenly spaced, at most <see cref="Spacing"/> apart, from its start to its end), how far below the deck's
    /// top the left and the right piling's foot is, found by casts down to the ground when it was built (0: no piling). The
    /// piece stands at its start, on the centre line at the deck's top, facing along it; its material is its prefab's
    /// (<see cref="KwPierKinds.OfPrefab"/>). Plain data.
    /// </summary>
    public sealed class KwPierSpec
    {
        /// <summary>The ZDO string a dock section saves its shape in.</summary>
        public const string Key = "bs_pier";

        /// <summary>The most a section's stations (and so its pilings) are apart.</summary>
        public const float Spacing = 2f;

        private const string Head = "pier1";
        private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

        public KwPierSpec(float length, float width, bool joined, int seed, Vector2[] depths)
        {
            (Length, Width, Joined, Seed, Depths) = (length, width, joined, seed, depths);
        }

        public float Length { get; }
        public float Width { get; }
        public bool Joined { get; }
        public int Seed { get; }

        /// <summary>Each station's left (x) and right (y) piling foot below the deck's top; 0 for none.</summary>
        public Vector2[] Depths { get; }

        public int Stations => Depths.Length;

        /// <summary>How far along the section a station stands.</summary>
        public float StationZ(int i) => Stations < 2 ? 0f : Length * i / (Stations - 1);

        /// <summary>How many stations a section of this length has: one every <see cref="Spacing"/> at most, both ends included.</summary>
        public static int StationsFor(float length) => Mathf.Max(1, Mathf.CeilToInt(length / Spacing - 0.001f)) + 1;

        public override string ToString()
        {
            List<string> depths = new List<string>(Depths.Length);
            foreach (Vector2 d in Depths)
                depths.Add(d.x.ToString("F2", Inv) + "," + d.y.ToString("F2", Inv));
            return string.Join("|", Head, Length.ToString("F2", Inv), Width.ToString("F2", Inv), Joined ? "1" : "0", Seed.ToString(Inv), string.Join(";", depths));
        }

        /// <summary>A section's shape from its saved text; false (and nothing built) for anything else.</summary>
        public static bool TryParse(string? text, out KwPierSpec spec)
        {
            spec = null!;
            string[] parts = (text ?? "").Split('|');
            if (parts.Length != 6 || parts[0] != Head || !Number(parts[1], out float length) || !Number(parts[2], out float width)
                || !int.TryParse(parts[4], NumberStyles.Integer, Inv, out int seed) || length < 0.5f || width < 0.5f)
                return false;
            Vector2[]? depths = Feet(parts[5]);
            if (depths == null || depths.Length < 2)
                return false;
            spec = new KwPierSpec(length, width, parts[3] == "1", seed, depths);
            return true;
        }

        // "l,r;l,r;..." as each station's two depths; null when any is unreadable.
        private static Vector2[]? Feet(string text)
        {
            string[] stations = text.Split(';');
            Vector2[] depths = new Vector2[stations.Length];
            for (int i = 0; i < stations.Length; i++)
            {
                string[] pair = stations[i].Split(',');
                if (pair.Length != 2 || !Number(pair[0], out float left) || !Number(pair[1], out float right))
                    return null;
                depths[i] = new Vector2(left, right);
            }
            return depths;
        }

        private static bool Number(string text, out float value) => float.TryParse(text, NumberStyles.Float, Inv, out value) && !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
