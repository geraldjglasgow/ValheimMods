using System;
using System.Collections.Generic;
using EliteEquipment.Boots;
using UnityEngine;

namespace EliteEquipment.Fitted
{
    /// <summary>
    /// A chest's skinned mesh with its lower part drawn in over fitted leggings, for chests whose plates and cloth below
    /// the waist were shaped to lie on the game's baggy trousers (<see cref="ChestShape"/>). Nothing moves vertex by
    /// vertex, which crimps: the plates lean in, every plate vertex horizontally toward the hips' axis by an amount that
    /// grows with its depth below the waist at a rate set per direction round the body (the lean its plates can take
    /// before a tenth of them would pass inside the leggings and the gap; smoothed round the body), so a plate keeps its
    /// shape and thickness and only loses its flare. Plate vertices weighted to the hands (a fringe) take the weights of
    /// the nearest plate vertex that is not, so they follow the hips; then every plate vertex follows the leg under it
    /// more the lower it hangs (the body's own weights there mixed in, up to <see cref="FollowMost"/> in front and
    /// <see cref="FollowBehind"/> behind), the way a tasset strapped to the thigh does, so a striding leg does not pass
    /// through it while the backs of the plates hang loose (the user: "dangle a bit more, right now its kinda suctioned"). The front hanging cloth leaves the chest
    /// for a mesh of its own, the back one is left out (<see cref="ChestCloth"/>). The copy keeps every other vertex byte, the bind poses and bounds (<see cref="MeshCut"/>).
    /// </summary>
    internal static class ChestPull
    {
        private const float Settled = 0.03f;
        private const float Tenth = 0.1f;

        // How far below the waist a plate follows the thigh under it fully, and how much at most: in front (where a
        // striding leg would pass through it) most, behind a little, so the backs of the plates hang loose and dangle.
        private const float FollowDepth = 0.35f;
        private const float FollowMost = 0.75f;
        private const float FollowBehind = 0.2f;

        public static ChestParts Make(Mesh source, string[] bones, BodySurface body, BodyEnvelope envelope, float thickness, ChestShape shape)
        {
            Mesh[] parts = MeshCut.Cut(source, new bool[source.GetIndexCount(0) / 3], null);
            UnityEngine.Object.Destroy(parts[1]);
            Mesh copy = parts[0];
            copy.name = source.name + "_ee_fitted";
            var v = new ChestVertices(Array.ConvertAll(copy.vertices, BodySurface.ToRest), copy.uv, copy.boneWeights, BodySurface.UpperOf(copy.boneWeights, bones), envelope);
            v.Measure(thickness, shape);
            float most = LeanPlates(v);
            Reweight(v);
            Follow(v, body);
            ChestCloth.Hang(v, shape.ClothBottom);
            copy.vertices = Array.ConvertAll(v.Rest, BodySurface.FromRest);
            copy.boneWeights = v.Weights;
            Mesh cloth = ChestCloth.Split(copy, v, shape);
            copy.UploadMeshData(true);
            Plugin.Log.LogInfo($"EliteEquipment: {source.name} drawn in over fitted leggings: plates lean in by up to {most * 100f:0.0} cm; front cloth {(cloth != null ? "on its own, ending at " + shape.ClothBottom.ToString("0.00") + " m" : "not found")}");
            return new ChestParts(copy, cloth);
        }

        /// <summary>Every plate vertex moves in by its direction's lean rate times its depth below the waist; the most any moved.</summary>
        private static float LeanPlates(ChestVertices v)
        {
            float[] rate = Smooth(Rates(v), 3, 3);
            float most = 0f;
            for (int i = 0; i < v.Rest.Length; i++)
            {
                if (v.Kind[i] != ChestPart.Plate)
                    continue;
                float shift = rate[v.Sector(i)] * v.Depth(i);
                most = Mathf.Max(most, shift);
                v.Rest[i] -= v.Envelope.Outward(v.Rest[i]) * shift;
            }
            return most;
        }

        /// <summary>Per direction, the lean all but a tenth of its plate vertices can take; directions with none take the mean.</summary>
        private static float[] Rates(ChestVertices v)
        {
            var ratios = new List<float>[ChestVertices.Sectors];
            for (int s = 0; s < ratios.Length; s++)
                ratios[s] = new List<float>();
            for (int i = 0; i < v.Rest.Length; i++)
            {
                if (v.Kind[i] == ChestPart.Plate && v.Depth(i) > Settled)
                    ratios[v.Sector(i)].Add(Mathf.Max(0f, v.Excess[i]) / v.Depth(i));
            }
            var rate = new float[ratios.Length];
            float sum = 0f;
            int filled = 0;
            for (int s = 0; s < ratios.Length; s++)
            {
                ratios[s].Sort();
                rate[s] = ratios[s].Count > 0 ? ratios[s][(int)(ratios[s].Count * Tenth)] : -1f;
                sum += rate[s] >= 0f ? rate[s] : 0f;
                filled += rate[s] >= 0f ? 1 : 0;
            }
            for (int s = 0; s < rate.Length; s++)
                rate[s] = rate[s] >= 0f ? rate[s] : (filled > 0 ? sum / filled : 0f);
            return rate;
        }

        /// <summary>A box average round the body, <paramref name="width"/> directions each side, <paramref name="passes"/> times.</summary>
        private static float[] Smooth(float[] rate, int width, int passes)
        {
            for (int pass = 0; pass < passes; pass++)
            {
                var next = new float[rate.Length];
                for (int s = 0; s < rate.Length; s++)
                {
                    for (int k = -width; k <= width; k++)
                        next[s] += rate[(s + k + rate.Length) % rate.Length];
                    next[s] /= 2 * width + 1;
                }
                rate = next;
            }
            return rate;
        }

        /// <summary>Each plate vertex takes on the skin weights of the body vertex nearest it, more the deeper below the waist.</summary>
        private static void Follow(ChestVertices v, BodySurface body)
        {
            for (int i = 0; i < v.Rest.Length; i++)
            {
                if (v.Kind[i] != ChestPart.Plate)
                    continue;
                float behind = Mathf.Max(0f, -v.Envelope.Outward(v.Rest[i]).z);
                float t = Mathf.Clamp01(v.Depth(i) / FollowDepth) * Mathf.Lerp(FollowMost, FollowBehind, behind);
                int under = NearestLeg(body, v.Rest[i]);
                if (under >= 0 && t > 0f)
                    v.Weights[i] = BoneBlend.Mix(v.Weights[i], body.Weights[under], t);
            }
        }

        private static int NearestLeg(BodySurface body, Vector3 rest)
        {
            int best = -1;
            float bestDistance = float.MaxValue;
            for (int b = 0; b < body.Rest.Length; b++)
            {
                if (body.Upper[b] || body.Rest[b].y > LegRegion.Waist || body.Rest[b].y < BodyEnvelope.Low)
                    continue;
                float d = (body.Rest[b] - rest).sqrMagnitude;
                if (d < bestDistance)
                {
                    bestDistance = d;
                    best = b;
                }
            }
            return best;
        }

        /// <summary>Plate vertices weighted to the hands take the weights of the nearest plate vertex that is not.</summary>
        private static void Reweight(ChestVertices v)
        {
            var steady = new List<int>();
            for (int i = 0; i < v.Rest.Length; i++)
            {
                if (v.Kind[i] == ChestPart.Plate && !v.Upper[i])
                    steady.Add(i);
            }
            for (int i = 0; i < v.Rest.Length && steady.Count > 0; i++)
            {
                if (v.Kind[i] != ChestPart.Plate || !v.Upper[i])
                    continue;
                int nearest = steady[0];
                foreach (int j in steady)
                {
                    if ((v.Rest[j] - v.Rest[i]).sqrMagnitude < (v.Rest[nearest] - v.Rest[i]).sqrMagnitude)
                        nearest = j;
                }
                v.Weights[i] = v.Weights[nearest];
            }
        }
    }

    /// <summary>A chest drawn in: its own mesh without the hanging cloth, and the front cloth's mesh (null when it has none).</summary>
    internal sealed class ChestParts
    {
        public ChestParts(Mesh chest, Mesh cloth)
        {
            Chest = chest;
            Cloth = cloth;
        }

        public Mesh Chest { get; }
        public Mesh Cloth { get; }
    }
}
