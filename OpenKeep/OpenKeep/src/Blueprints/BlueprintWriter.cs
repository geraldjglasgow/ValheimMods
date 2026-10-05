using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

namespace OpenKeep.Blueprints
{
    /// <summary>
    /// Writes a saved build as blueprint JSON in the layout DevBridge's blueprint.py writes (one piece per row), so the
    /// file also works there: besides OpenKeep's "relief" and "water", the site carries the "level_half",
    /// "clear_radius" and "stand" blueprint.py needs.
    /// </summary>
    public static class BlueprintWriter
    {
        public static void Write(Blueprint bp, string path)
        {
            StringBuilder text = new StringBuilder();
            text.Append("{\n");
            text.Append(" \"name\": ").Append(Quote(bp.Name)).Append(",\n");
            text.Append(" \"description\": ").Append(Quote(bp.Description ?? "")).Append(",\n");
            text.Append(" \"source\": \"OpenKeep: openkeep blueprint save\",\n");
            text.Append(" \"site\": ").Append(Site(bp)).Append(",\n");
            text.Append(" \"pieces\": [\n  ");
            text.Append(string.Join(",\n  ", bp.Pieces.Select(Row)));
            text.Append("\n ]\n}\n");
            File.WriteAllText(path, text.ToString(), new UTF8Encoding(false));
        }

        private static string Site(Blueprint bp)
        {
            List<string> parts = new List<string>
            {
                "\"level_half\": " + Num(LevelHalf(bp)),
                "\"clear_radius\": " + Num(Mathf.Ceil(Far(bp) + 1.5f)),
                "\"stand\": [0, " + Num(bp.PieceBounds.yMin - 3f) + "]",
            };
            if (bp.HasWater)
                parts.Add("\"water\": {\"floor\": " + Num(bp.WaterFloor) + ", \"depth\": " + Num(bp.WaterDepth) + "}");
            if (bp.Relief != null)
                parts.Add("\"relief\": " + Relief(bp.Relief));
            return "{\n  " + string.Join(",\n  ", parts) + "\n }";
        }

        private static string Relief(Relief r)
        {
            string offsets = string.Join(", ", r.Offsets.Select(Num));
            return $"{{\"x\": {Num(r.X0)}, \"z\": {Num(r.Z0)}, \"width\": {r.Width}, \"depth\": {r.Depth}, \"offsets\": [{offsets}]}}";
        }

        /// <summary>blueprint.py's level square: over the pieces that stand on the ground, plus half a metre.</summary>
        private static float LevelHalf(Blueprint bp)
        {
            float half = 0f;
            foreach (BlueprintPiece p in bp.Pieces.Where(p => p.Y < 1f))
                half = Mathf.Max(half, Mathf.Max(Mathf.Abs(p.X), Mathf.Abs(p.Z)));
            return half + 0.5f;
        }

        private static float Far(Blueprint bp)
        {
            float far = 0f;
            foreach (BlueprintPiece p in bp.Pieces)
                far = Mathf.Max(far, Mathf.Sqrt(p.X * p.X + p.Z * p.Z));
            return far;
        }

        private static string Row(BlueprintPiece p)
        {
            return $"[{Quote(p.Prefab)}, {Num(p.X)}, {Num(p.Y)}, {Num(p.Z)}, {Num(p.Yaw)}]";
        }

        private static string Num(float value)
        {
            float rounded = Mathf.Abs(value) < 0.0005f ? 0f : value;
            return rounded.ToString("0.###", CultureInfo.InvariantCulture);
        }

        private static string Quote(string text)
        {
            StringBuilder s = new StringBuilder("\"");
            foreach (char c in text)
            {
                if (c == '"' || c == '\\')
                    s.Append('\\').Append(c);
                else if (c < ' ')
                    s.Append("\\u").Append(((int)c).ToString("x4"));
                else
                    s.Append(c);
            }
            return s.Append('"').ToString();
        }
    }
}
