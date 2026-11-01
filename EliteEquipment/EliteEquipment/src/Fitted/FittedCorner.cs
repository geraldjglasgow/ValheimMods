using System.Collections.Generic;
using UnityEngine;

namespace EliteEquipment.Fitted
{
    /// <summary>
    /// A corner of a piece cut out of the leggings (a strap, the belt, a trim): its mesh-space position and normal, its rest position
    /// in metres and its skin weights. A corner made where a cut crosses an edge is mixed from the edge's two ends, the
    /// weights too (the four strongest bones kept). The weights are mixed in two small buffers rather than a dictionary
    /// sorted per corner (a build mixes a few thousand); the leggings are only made on the main thread.
    /// </summary>
    internal struct FittedCorner
    {
        // The two mixed weights' bones and summed shares, in the order first met (a's bones, then b's).
        private static readonly int[] MixBones = new int[8];
        private static readonly float[] MixShares = new float[8];
        private static int mixCount;

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

        /// <summary>
        /// The four strongest of both weights' bones by their summed shares, scaled to sum to one: the strongest first,
        /// equal shares in the order met, the total added up in double precision (as a sort and sum of them would).
        /// </summary>
        private static BoneWeight Mix(BoneWeight a, BoneWeight b, float t)
        {
            mixCount = 0;
            Add(a, 1f - t);
            Add(b, t);
            SortStrongest();
            int kept = Mathf.Min(mixCount, 4);
            double sum = 0.0;
            for (int i = 0; i < kept; i++)
                sum += MixShares[i];
            float total = (float)sum;
            var weight = new BoneWeight();
            if (kept > 0) { weight.boneIndex0 = MixBones[0]; weight.weight0 = MixShares[0] / total; }
            if (kept > 1) { weight.boneIndex1 = MixBones[1]; weight.weight1 = MixShares[1] / total; }
            if (kept > 2) { weight.boneIndex2 = MixBones[2]; weight.weight2 = MixShares[2] / total; }
            if (kept > 3) { weight.boneIndex3 = MixBones[3]; weight.weight3 = MixShares[3] / total; }
            return weight;
        }

        private static void Add(BoneWeight w, float share)
        {
            One(w.boneIndex0, w.weight0 * share);
            One(w.boneIndex1, w.weight1 * share);
            One(w.boneIndex2, w.weight2 * share);
            One(w.boneIndex3, w.weight3 * share);
        }

        private static void One(int bone, float weight)
        {
            if (!(weight > 0f))
                return;
            for (int i = 0; i < mixCount; i++)
            {
                if (MixBones[i] == bone)
                {
                    MixShares[i] += weight;
                    return;
                }
            }
            MixBones[mixCount] = bone;
            MixShares[mixCount++] = weight;
        }

        /// <summary>The mixed bones strongest first; equal shares keep the order they were met in.</summary>
        private static void SortStrongest()
        {
            for (int i = 1; i < mixCount; i++)
            {
                int bone = MixBones[i];
                float share = MixShares[i];
                int j = i - 1;
                for (; j >= 0 && MixShares[j] < share; j--)
                {
                    MixBones[j + 1] = MixBones[j];
                    MixShares[j + 1] = MixShares[j];
                }
                MixBones[j + 1] = bone;
                MixShares[j + 1] = share;
            }
        }
    }
}
