using System.Collections.Generic;
using UnityEngine;

namespace EliteEquipment.Fitted
{
    /// <summary>Skin weights mixed: two sets of up to four bones each, the four strongest kept and summed to one.</summary>
    internal static class BoneBlend
    {
        /// <summary><paramref name="a"/> moved <paramref name="t"/> of the way to <paramref name="b"/>.</summary>
        public static BoneWeight Mix(BoneWeight a, BoneWeight b, float t)
        {
            var sums = new Dictionary<int, float>();
            Add(sums, a, 1f - t);
            Add(sums, b, t);
            var strongest = new List<KeyValuePair<int, float>>(sums);
            strongest.Sort((x, y) => y.Value.CompareTo(x.Value));
            float total = 0f;
            for (int i = 0; i < strongest.Count && i < 4; i++)
                total += strongest[i].Value;
            var mixed = new BoneWeight();
            for (int i = 0; i < strongest.Count && i < 4; i++)
                Set(ref mixed, i, strongest[i].Key, total > 0f ? strongest[i].Value / total : 0f);
            return mixed;
        }

        private static void Add(Dictionary<int, float> sums, BoneWeight w, float share)
        {
            Put(sums, w.boneIndex0, w.weight0 * share);
            Put(sums, w.boneIndex1, w.weight1 * share);
            Put(sums, w.boneIndex2, w.weight2 * share);
            Put(sums, w.boneIndex3, w.weight3 * share);
        }

        private static void Put(Dictionary<int, float> sums, int bone, float weight)
        {
            if (weight > 0f)
                sums[bone] = (sums.TryGetValue(bone, out float sum) ? sum : 0f) + weight;
        }

        private static void Set(ref BoneWeight w, int slot, int bone, float weight)
        {
            switch (slot)
            {
                case 0: w.boneIndex0 = bone; w.weight0 = weight; break;
                case 1: w.boneIndex1 = bone; w.weight1 = weight; break;
                case 2: w.boneIndex2 = bone; w.weight2 = weight; break;
                default: w.boneIndex3 = bone; w.weight3 = weight; break;
            }
        }
    }
}
