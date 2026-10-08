using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace OpenKeep.Boots
{
    /// <summary>
    /// Where the workshop cut each game leggings in two (ValheimAssets <c>Assets/Gear/SeparatedLegArmor/NativeSplit_v001</c>,
    /// with its Bronze v004, Leather v002 and Troll v002 paint), as coordinates only: no mesh, texture or pixel of the
    /// game's is shipped. The embedded <c>assets/boots/native_split.txt</c> names the boots triangles of the leggings'
    /// mesh, the texel row of the body paint where the boots begin, and the paint edits that carry the trousers down to
    /// the ankle, each a texel taken from elsewhere in the game's own paint. Read once, on first use.
    /// </summary>
    public static class NativeSplit
    {
        private const string Resource = "OpenKeep.assets.boots.native_split.txt";
        private static Dictionary<string, SplitData> sets;

        /// <summary>A set's split by its key (<see cref="BootSet.Key"/>), or null when the workshop has none.</summary>
        public static SplitData For(string key)
        {
            if (sets == null)
                sets = Load();
            return sets.TryGetValue(key, out SplitData data) ? data : null;
        }

        private static Dictionary<string, SplitData> Load()
        {
            var found = new Dictionary<string, SplitData>();
            using (Stream stream = typeof(NativeSplit).Assembly.GetManifestResourceStream(Resource))
            {
                if (stream == null)
                {
                    Plugin.Log.LogWarning($"OpenKeep: {Resource} is missing; the leggings and boots keep the game's whole look");
                    return found;
                }
                using (var reader = new StreamReader(stream))
                    Read(reader, found);
            }
            return found;
        }

        private static void Read(StreamReader reader, Dictionary<string, SplitData> found)
        {
            SplitData current = null;
            string line;
            while ((line = reader.ReadLine()) != null)
            {
                string[] words = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                if (words.Length == 0 || words[0].StartsWith("#", StringComparison.Ordinal))
                    continue;
                if (words[0] == "faces" || words[0] == "paint")
                    current = Section(words, found);
                else if (current != null)
                    current.Edits.Add(new PaintEdit(words[0][0], Int(words[1]), Int(words[2]), Int(words[3]), Int(words[4]),
                        words.Length > 5 ? float.Parse(words[5], CultureInfo.InvariantCulture) : 1f));
            }
        }

        private static SplitData Section(string[] words, Dictionary<string, SplitData> found)
        {
            if (!found.TryGetValue(words[1], out SplitData data))
                found[words[1]] = data = new SplitData();
            if (words[0] == "faces")
                data.Boots = Faces(Int(words[2]), words[3]);
            else
            {
                data.PaintSize = Int(words[2]);
                data.PaintBoundary = Int(words[3]);
            }
            return data;
        }

        /// <summary>The boots triangles, <c>first-last</c> ranges joined by commas, as one flag per triangle.</summary>
        private static bool[] Faces(int total, string ranges)
        {
            var boots = new bool[total];
            foreach (string range in ranges.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
            {
                string[] ends = range.Split('-');
                if (ends.Length != 2 || ends[0].Length == 0)
                    continue;
                for (int face = Int(ends[0]); face <= Int(ends[1]) && face < total; face++)
                    boots[face] = true;
            }
            return boots;
        }

        private static int Int(string text) => int.Parse(text, CultureInfo.InvariantCulture);
    }

    /// <summary>One set's split: its boots triangles (null when the leggings have no mesh) and its body paint rows.</summary>
    public sealed class SplitData
    {
        /// <summary>One flag per triangle of the leggings' mesh: true for the boots.</summary>
        public bool[] Boots { get; set; }

        /// <summary>The paint's expected size in texels (square); 0 when the leggings paint nothing.</summary>
        public int PaintSize { get; set; }

        /// <summary>The first texel row, counted from the top, that belongs to the boots; the size for none.</summary>
        public int PaintBoundary { get; set; }

        public List<PaintEdit> Edits { get; } = new List<PaintEdit>();
    }

    /// <summary>
    /// A trousers paint edit, in texels from the top-left: <c>c</c> takes the colour of the game's texel at (sx, sy),
    /// <c>f</c> takes it and makes the texel opaque, <c>b</c> blends towards it by <see cref="Weight"/> after every c and f.
    /// </summary>
    public readonly struct PaintEdit
    {
        public PaintEdit(char kind, int x, int y, int sourceX, int sourceY, float weight)
        {
            Kind = kind;
            X = x;
            Y = y;
            SourceX = sourceX;
            SourceY = sourceY;
            Weight = weight;
        }

        public char Kind { get; }
        public int X { get; }
        public int Y { get; }
        public int SourceX { get; }
        public int SourceY { get; }
        public float Weight { get; }
    }
}
