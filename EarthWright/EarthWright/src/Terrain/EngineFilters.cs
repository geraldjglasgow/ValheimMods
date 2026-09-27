using System;
using UnityEngine;

namespace EarthWright.Terrain
{
    /// <summary>
    /// The per-point filters of a brush stroke: only points whose height lies in a band, only a random share of the
    /// points, not under building pieces. Configured once per plan. The band and the share depend only on synced inputs
    /// (heights, whole-metre world coordinates, the stroke's seed), so both owners of a shared edge pick the same points;
    /// the building probe reads the physics scene of the machine planning (the owner has the area loaded).
    /// </summary>
    public sealed class EngineFilters
    {
        private bool band;
        private float bandMin;
        private float bandMax;
        private bool share;
        private float shareValue;
        private int seed;
        private Func<float, float, float, bool> underPiece;

        public void Configure(TerrainEdit edit, HeightView view)
        {
            BrushStroke s = edit.Stroke;
            band = s != null && edit.Has(EditFlags.HeightBand);
            if (band)
            {
                bandMin = Mathf.Min(s.BandMin, s.BandMax);
                bandMax = Mathf.Max(s.BandMin, s.BandMax);
            }
            shareValue = s != null ? Mathf.Clamp01(s.RandomShare) : 1f;
            share = shareValue < 1f;
            seed = s != null ? s.Seed : 0;
            underPiece = view.UnderPiece;
        }

        /// <summary>Height vertex i at world (wx, wz) passes every filter.</summary>
        public bool PassVertex(HeightView view, int i, float wx, float wz) => Pass(view, i, wx, wz, wx, wz);

        /// <summary>Paint cell i, centred at world (cx, cz), passes every filter (band: the height of its vertex; share: its corner vertex).</summary>
        public bool PassCell(HeightView view, int i, float cx, float cz) => Pass(view, i, cx, cz, cx - 0.5f * view.Scale, cz - 0.5f * view.Scale);

        private bool Pass(HeightView view, int i, float px, float pz, float vx, float vz)
        {
            if (share && VertexHash.Unit(Mathf.RoundToInt(vx), Mathf.RoundToInt(vz), seed) >= shareValue)
                return false;
            if (!band && underPiece == null)
                return true;
            float height = view.Current(i) + view.OriginY;
            if (band && (height < bandMin || height > bandMax))
                return false;
            return underPiece == null || !underPiece(px, height, pz);
        }
    }
}
