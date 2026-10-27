using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// A launched ship's patched holes as its ZDO keeps them (<see cref="Key"/>), plain text the owner writes and every peer
    /// reads: one patch a ';'-separated entry, "id,radius", the hole's own number (<see cref="KwHullHole.Id"/>, kept as the
    /// hole widens) and its radius when it was patched. An entry that does not read is skipped. And what the patches mean
    /// for the holes, plain: which are patched, which let the sea in.
    /// </summary>
    public static class KwHullPatchText
    {
        public const string Key = "kw_hull_patches";
        public static readonly int Hash = Key.GetStableHashCode();

        /// <summary>How much a hole may have grown past its patch's radius and still be covered (the text's rounding).</summary>
        public const float Slack = 0.005f;
        private static readonly CultureInfo Plain = CultureInfo.InvariantCulture;

        public static string Write(Dictionary<int, float> patches)
        {
            List<int> ids = new List<int>(patches.Keys);
            ids.Sort();
            StringBuilder text = new StringBuilder();
            foreach (int id in ids)
                text.Append(text.Length > 0 ? ";" : "").Append(id.ToString(Plain)).Append(',').Append(patches[id].ToString("0.###", Plain));
            return text.ToString();
        }

        public static Dictionary<int, float> Read(string? text)
        {
            Dictionary<int, float> patches = new Dictionary<int, float>();
            foreach (string entry in (text ?? "").Split(';'))
            {
                string[] f = entry.Split(',');
                if (f.Length == 2 && int.TryParse(f[0], NumberStyles.Integer, Plain, out int id) && float.TryParse(f[1], NumberStyles.Float, Plain, out float radius))
                    patches[id] = radius;
            }
            return patches;
        }

        /// <summary>Whether a hole has a patch on it that still covers it (it has not widened past the patch).</summary>
        public static bool Holds(Dictionary<int, float> patches, KwHullHole hole) =>
            patches.TryGetValue(hole.Id, out float radius) && hole.Radius <= radius + Slack;

        /// <summary>The holes split into the patched ones' numbers and the ones that leak (both lists filled anew).</summary>
        public static void Split(List<KwHullHole> holes, Dictionary<int, float> patches, HashSet<int> patched, List<KwHullHole> leaking)
        {
            patched.Clear();
            leaking.Clear();
            foreach (KwHullHole hole in holes)
            {
                if (Holds(patches, hole))
                    patched.Add(hole.Id);
                else
                    leaking.Add(hole);
            }
        }

        /// <summary>The patches still on a hole that is there and has not widened past them.</summary>
        public static Dictionary<int, float> Kept(List<KwHullHole> holes, Dictionary<int, float> patches)
        {
            Dictionary<int, float> kept = new Dictionary<int, float>();
            foreach (KwHullHole hole in holes)
            {
                if (Holds(patches, hole))
                    kept[hole.Id] = patches[hole.Id];
            }
            return kept;
        }

        /// <summary>A patch's reach from its hole's middle: the hole's furthest corner at the radius it was patched at, and a margin.</summary>
        public static float Reach(KwHullHole hole, float covering, float margin) =>
            KwHoleShape.Of(hole.Grown(Mathf.Max(covering, hole.Radius))).Reach + margin;
    }
}
