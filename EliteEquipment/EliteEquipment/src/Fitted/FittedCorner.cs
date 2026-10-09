using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace EliteEquipment.Fitted
{
    /// <summary>
    /// A corner of a piece cut out of the leggings (a strap, the belt, a trim): its mesh-space position and normal, its rest position
    /// in metres and its skin weights. A corner made where a cut crosses an edge is mixed from the edge's two ends, the
    /// weights too (the four strongest bones kept).
    /// </summary>
    internal struct FittedCorner
    {
        public Vector3 Position;
        public Vector3 Normal;
        public Vector3 Rest;
        public BoneWeight Weight;

        public static FittedCorner Lerp(FittedCorner a, FittedCorner b, float t) => new FittedCorner
        {
            Position = Vector3.Lerp(a.Position, b.Position, t),
            Normal = Vector3.Lerp(a.Normal, b.Normal, t).normalized,
            Rest = Vector3.Lerp(a.Rest, b.Rest, t),
            Weight = Mix(a.Weight, b.Weight, t),
        };

        /// <summary>The part of a convex polygon on the side of the plane (rest space) its normal points to.</summary>
        public static List<FittedCorner> Clip(List<FittedCorner> polygon, Vector3 normal, float distance)
        {
            var kept = new List<FittedCorner>();
            for (int i = 0; i < polygon.Count; i++)
            {
                FittedCorner a = polygon[i], b = polygon[(i + 1) % polygon.Count];
                float da = Vector3.Dot(a.Rest, normal) - distance, db = Vector3.Dot(b.Rest, normal) - distance;
                if (da >= 0f)
                    kept.Add(a);
                if (da >= 0f != db >= 0f)
                    kept.Add(Lerp(a, b, da / (da - db)));
            }
            return kept;
        }

        private static BoneWeight Mix(BoneWeight a, BoneWeight b, float t)
        {
            var sums = new Dictionary<int, float>();
            Add(sums, a, 1f - t);
            Add(sums, b, t);
            KeyValuePair<int, float>[] top = sums.OrderByDescending(kv => kv.Value).Take(4).ToArray();
            float total = top.Sum(kv => kv.Value);
            var weight = new BoneWeight();
            if (top.Length > 0) { weight.boneIndex0 = top[0].Key; weight.weight0 = top[0].Value / total; }
            if (top.Length > 1) { weight.boneIndex1 = top[1].Key; weight.weight1 = top[1].Value / total; }
            if (top.Length > 2) { weight.boneIndex2 = top[2].Key; weight.weight2 = top[2].Value / total; }
            if (top.Length > 3) { weight.boneIndex3 = top[3].Key; weight.weight3 = top[3].Value / total; }
            return weight;
        }

        private static void Add(Dictionary<int, float> sums, BoneWeight w, float share)
        {
            One(sums, w.boneIndex0, w.weight0 * share);
            One(sums, w.boneIndex1, w.weight1 * share);
            One(sums, w.boneIndex2, w.weight2 * share);
            One(sums, w.boneIndex3, w.weight3 * share);
        }

        private static void One(Dictionary<int, float> sums, int bone, float weight)
        {
            if (weight > 0f)
                sums[bone] = (sums.TryGetValue(bone, out float sum) ? sum : 0f) + weight;
        }
    }
}
