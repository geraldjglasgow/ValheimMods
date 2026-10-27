using System.Globalization;
using System.Text;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// The figures of a ship (<see cref="KwBpFigures"/>) as the table's figures panel shows them (<see cref="KwBpFiguresPanel"/>),
    /// a line each in the game's rich text, the name in the panel's pale words and the figure after it at one indent
    /// (a long cost wraps under its figure): size, top speed (against a karve, and in km/h), the square and fore-and-aft
    /// sails' areas and the winds each sails in (<see cref="KwSailWords"/>), draught and waterline,
    /// freeboard, stability (the metacentric height and its word), weight, hull health, turning and the cost from the
    /// hammer, in the game's own item names.
    /// </summary>
    internal static class KwBpFiguresText
    {
        private const string Pale = "#DBCCAD", Indent = "100";

        public static string Lines(KwBpFigures f)
        {
            StringBuilder text = new StringBuilder();
            Line(text, "Size", $"{N(f.Length, 1)} m by {N(f.Beam, 1)} m, {N(f.Depth, 1)} m deep");
            Line(text, "Top speed", Speed(f.Handling));
            Line(text, "Sails", KwSailWords.Areas(f.Stats.Mix));
            Line(text, "Winds", KwSailWords.Winds(f.Stats.Mix, f.Handling.Closest));
            Line(text, "Draught", $"{N(f.Draught, 2)} m (waterline {N(f.Waterline, 2)} m up the hull)");
            Line(text, "Freeboard", $"{N(f.Freeboard, 2)} m");
            Line(text, "Stability", Stability(f.Stability));
            Line(text, "Weight", $"{N(f.Weight / 1000f, 1)} t empty");
            Line(text, "Hull health", N(f.Stats.Health, 0));
            Line(text, "Turning", Turning(f));
            Line(text, "Cost", KwBpCost.Words(f.Cost));
            if (!f.Hull)
                text.Append($"<color={Pale}>No planking: sized as a small boat.</color>");
            return text.ToString();
        }

        // A name, its figure from one indent on (wrapped lines too), and the line's end.
        private static void Line(StringBuilder text, string name, string figure) =>
            text.Append($"<color={Pale}>{name}</color><indent={Indent}>{figure}</indent>\n");

        private static string Speed(KwBpHandling handling)
        {
            if (handling.TopSpeed <= 0f)
                return $"no sail pulls it: rows at {N(handling.RowSpeed * 3.6f, 0)} km/h";
            return $"{N(handling.Relative * 100f, 0)}% of a karve's, {N(handling.TopSpeed * 3.6f, 0)} km/h";
        }

        private static string Stability(KwBpStability stability)
        {
            if (stability == KwBpStability.None)
                return "no hull under the water";
            string colour = stability.Word == "stiff" ? "#A8E08F" : stability.Word == "steady" ? "#FFFFFF" : stability.Word == "tender" ? "#FFB84D" : "#FF6B5B";
            string roll = stability.Period > 0f ? $" ({N(stability.Period, 1)} s roll)" : "";
            return $"GM {N(stability.Height, 2)} m, <color={colour}>{stability.Word}</color>{roll}";
        }

        private static string Turning(KwBpFigures f)
        {
            string rudder = f.Stats.Rudder < 0 ? "no rudder: a third of the push" : $"rudder {N(f.Stats.Steering * 100f, 0)}%";
            return f.Handling.Circle > 0f ? $"{N(f.Handling.Circle, 0)} m circle ({rudder})" : $"under oars only ({rudder})";
        }

        public static string N(float value, int decimals) => value.ToString("F" + decimals, CultureInfo.InvariantCulture);

        /// <summary>A header line in the game's orange.</summary>
        public static string Header(string text) => $"<color=#FFB83D>{text}</color>";

        /// <summary>A line in the panel's pale words.</summary>
        public static string Quiet(string text) => $"<color={Pale}>{text}</color>";

        /// <summary>Kept apart from the game's rich text: a file name with tags in it shows as it is.</summary>
        public static string Plain(string text) => "<noparse>" + (text ?? "").Replace("</noparse>", "") + "</noparse>";

        public static Color Note => new Color(1f, 0.72f, 0.24f);
    }
}
