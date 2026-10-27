using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// A ship's sails by kind in words: their areas and which winds each sails in, for the blueprint table's figures
    /// (<see cref="KwBpFiguresText"/>), and how its sails draw in the wind it has now, for the helm's card
    /// (<see cref="KwShipCard"/>).
    /// </summary>
    public static class KwSailWords
    {
        private static readonly KwSailKind[] Along = { KwSailKind.Gaff, KwSailKind.Triangle, KwSailKind.Jib };

        /// <summary>The square and the fore-and-aft sails' areas: "square 120 m², fore and aft 40 m²"; "none".</summary>
        public static string Areas(KwSailMix mix)
        {
            List<string> parts = new List<string>();
            if (mix.SquareArea > 0.05f)
                parts.Add($"square {N(mix.SquareArea)} m²");
            if (mix.AlongArea > 0.05f)
                parts.Add($"fore and aft {N(mix.AlongArea)} m²");
            return parts.Count > 0 ? string.Join(", ", parts) : "none";
        }

        /// <summary>
        /// Which winds each kind sails in, and the closest to the wind the ship sails (<see cref="KwBpHandling.Closest"/>):
        /// "square abeam to astern, fore and aft 30° to astern; sails to 45° off the wind".
        /// </summary>
        public static string Winds(KwSailMix mix, float closest)
        {
            List<string> parts = new List<string>();
            if (mix.Has(KwSailKind.Square))
                parts.Add("square abeam to astern");
            float along = 180f;
            foreach (KwSailKind kind in Along)
                along = mix.Has(kind) ? Mathf.Min(along, KwSailPolar.Closest(kind)) : along;
            if (along < 180f)
                parts.Add($"fore and aft {N(along)}° to astern");
            if (parts.Count == 0)
                return "no sail pulls it";
            return string.Join(", ", parts) + (closest < 180f ? $"; sails to {N(closest)}° off the wind" : "");
        }

        /// <summary>
        /// The helm's line for the wind now: where it comes from off the bow and how much of their best the sails draw,
        /// each kind's when the ship has both: "Wind from starboard, 45° off the bow: sails drawing 10% (square 0%, fore and aft 62%)".
        /// </summary>
        public static string Now(KwShipWind wind, Vector3 forward)
        {
            KwApparentWind air = wind.Now(forward);
            KwSailMix mix = wind.Mix;
            string from = air.Angle < 10f ? "right ahead" : air.Angle > 170f ? "dead astern" : $"from {(air.FromStarboard ? "starboard" : "port")}, {N(air.Angle)}° off the bow";
            string line = $"Wind {from}: sails drawing {Share(mix.Drive(air.Angle, false), mix.Best)}";
            if (!mix.Has(KwSailKind.Square) || mix.AlongArea <= 0.05f)
                return line;
            return line + $" (square {Share(KwSailPolar.Drive(KwSailKind.Square, air.Angle), 0.70f)}, fore and aft {Share(AlongNow(mix, air.Angle), AlongBest(mix))})";
        }

        // The fore-and-aft kinds' push at an angle, the best of those the ship has.
        private static float AlongNow(KwSailMix mix, float angle)
        {
            float best = 0f;
            foreach (KwSailKind kind in Along)
                best = mix.Has(kind) ? Mathf.Max(best, KwSailPolar.Drive(kind, angle)) : best;
            return best;
        }

        // The most push any of the ship's fore-and-aft kinds gives, at any angle.
        private static float AlongBest(KwSailMix mix)
        {
            float best = 0f;
            for (int degrees = 0; degrees <= 180; degrees++)
                best = Mathf.Max(best, AlongNow(mix, degrees));
            return best;
        }

        private static string Share(float now, float best) => best > 0f ? $"{N(100f * now / best)}%" : "0%";

        private static string N(float value) => value.ToString("F0", CultureInfo.InvariantCulture);
    }
}
