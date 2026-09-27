using System;
using System.Collections.Generic;
using System.Linq;
using EarthWright.Terrain;

namespace EarthWright.Clearing
{
    /// <summary>
    /// One <c>ew terrain &lt;op&gt; [radius] [value] [options]</c> request as typed: the operation, the two optional
    /// numbers, and the options (filters, paint, footprint). <see cref="Error"/> is set when something could not be read.
    /// </summary>
    public sealed class TerrainRequest
    {
        public string Op = "";
        public float? Radius;
        public float? Value;
        public float? Width;
        public float? BandMin;
        public float? BandMax;
        public float Share = 1f;
        public float Hardness = 1f;
        public bool SkipPieces;
        public bool Unlimited;
        public bool AtBrush;
        /// <summary>The typed shape=; null: a circle, or the brush's own shape with at=brush.</summary>
        public BrushShape? Shape;
        public PaintOp Paint = PaintOp.None;
        public readonly List<string> Ignore = new List<string>();
        public readonly List<string> Include = new List<string>();
        public string Error;

        /// <summary>Picks the same random share on every compiler the edit touches.</summary>
        public readonly int Seed = UnityEngine.Random.Range(int.MinValue, int.MaxValue);

        public bool HasBand => BandMin.HasValue && BandMax.HasValue;

        public bool HasObjectFilter => Ignore.Count > 0 || Include.Count > 0;
    }

    /// <summary>
    /// Reads a <see cref="TerrainRequest"/> from console arguments. Plain numbers are the radius, then the value; a
    /// "min:max" pair is the height band; "unlimited" lifts the height limits; everything else is key=value.
    /// </summary>
    public static class TerrainRequestParser
    {
        private static readonly Dictionary<string, Func<TerrainRequest, string, bool>> options =
            new Dictionary<string, Func<TerrainRequest, string, bool>>(StringComparer.OrdinalIgnoreCase)
            {
                ["share"] = (r, v) => Unit(v, x => r.Share = x),
                ["hardness"] = (r, v) => Unit(v, x => r.Hardness = x),
                ["width"] = SetWidth,
                ["skip"] = (r, v) => r.SkipPieces = string.Equals(v, "pieces", StringComparison.OrdinalIgnoreCase),
                ["band"] = SetBand,
                ["ignore"] = (r, v) => AddNames(r.Ignore, v),
                ["include"] = (r, v) => AddNames(r.Include, v),
                ["paint"] = SetPaint,
                ["shape"] = SetShape,
                ["at"] = SetAt,
            };

        public static TerrainRequest Parse(string[] args, int opIndex)
        {
            TerrainRequest request = new TerrainRequest { Op = args[opIndex].ToLowerInvariant() };
            for (int i = opIndex + 1; i < args.Length && request.Error == null; i++)
                Read(request, args[i]);
            return request;
        }

        private static void Read(TerrainRequest request, string token)
        {
            int eq = token.IndexOf('=');
            if (eq > 0)
            {
                Option(request, token.Substring(0, eq), token.Substring(eq + 1));
                return;
            }
            if (string.Equals(token, "unlimited", StringComparison.OrdinalIgnoreCase))
                request.Unlimited = true;
            else if (token.IndexOf(':') >= 0)
                SetBandOrFail(request, token);
            else if (CommandInput.TryFloat(token, out float number))
                Positional(request, number);
            else
                request.Error = $"'{token}' is not a number, a min:max band or an option (see 'ew terrain help')";
        }

        private static void Option(TerrainRequest request, string key, string value)
        {
            if (!options.TryGetValue(key, out Func<TerrainRequest, string, bool> apply))
                request.Error = $"unknown option '{key}' (see 'ew terrain help')";
            else if (!apply(request, value))
                request.Error = $"cannot use '{value}' for {key}= (see 'ew terrain help')";
        }

        private static void Positional(TerrainRequest request, float number)
        {
            if (!request.Radius.HasValue)
                request.Radius = number;
            else if (!request.Value.HasValue)
                request.Value = number;
            else
                request.Error = "at most two numbers: the radius, then the value";
        }

        private static void SetBandOrFail(TerrainRequest request, string text)
        {
            if (!SetBand(request, text))
                request.Error = $"'{text}' is not a height band like 20:35";
        }

        private static bool SetBand(TerrainRequest request, string text)
        {
            if (!CommandInput.TryRange(text, out float min, out float max))
                return false;
            request.BandMin = min;
            request.BandMax = max;
            return true;
        }

        private static bool SetWidth(TerrainRequest request, string value)
        {
            if (!CommandInput.TryFloat(value, out float width) || width <= 0f)
                return false;
            request.Width = width;
            return true;
        }

        private static bool SetPaint(TerrainRequest request, string value)
        {
            if (!Enum.TryParse(value, true, out PaintOp paint) || !Enum.IsDefined(typeof(PaintOp), paint))
                return false;
            request.Paint = paint;
            return true;
        }

        /// <summary>Only circle and square: the other shapes need a second size the command does not take.</summary>
        private static bool SetShape(TerrainRequest request, string value)
        {
            if (!Enum.TryParse(value, true, out BrushShape shape) || (shape != BrushShape.Circle && shape != BrushShape.Square))
                return false;
            request.Shape = shape;
            return true;
        }

        private static bool SetAt(TerrainRequest request, string value)
        {
            request.AtBrush = string.Equals(value, "brush", StringComparison.OrdinalIgnoreCase);
            return request.AtBrush || string.Equals(value, "player", StringComparison.OrdinalIgnoreCase);
        }

        private static bool Unit(string text, Action<float> set)
        {
            if (!CommandInput.TryFloat(text, out float value) || value < 0f || value > 1f)
                return false;
            set(value);
            return true;
        }

        private static bool AddNames(List<string> names, string text)
        {
            names.AddRange(text.Split(',').Select(n => n.Trim()).Where(n => n.Length > 0));
            return names.Count > 0;
        }
    }
}
