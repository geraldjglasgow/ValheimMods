using UnityEngine;

namespace EarthWright.Terrain
{
    /// <summary>
    /// A brush stroke's values as the engine uses them: cut down to the owner's safety caps (section 3), clamped to their
    /// ranges, with the height and paint footprints built. Built fresh for every plan without touching the stroke, so the
    /// sender's estimate sees exactly what the owner will do and the sender's own stroke object is never changed.
    /// A stroke with a non-finite number is invalid and changes nothing. A class, so the hot loops pass a reference.
    /// </summary>
    public sealed class StrokeParams
    {
        public BrushStroke Stroke;
        public bool Valid;
        public float Amount;
        public float MaxStep;
        public float Strength;
        public float PaintStrength;
        public float Density;
        public float Share;
        public Footprint HeightPrint;
        public Footprint PaintPrint;

        /// <summary>The stroke's values; <paramref name="admin"/> (an approved admin edit) raises the caps to the admin ceilings.</summary>
        public static StrokeParams From(TerrainEdit edit, bool admin = false) => Fill(new StrokeParams(), edit, admin);

        /// <summary>The same values written into a kept object (the preview's, built again every frame).</summary>
        public static StrokeParams Fill(StrokeParams p, TerrainEdit edit, bool admin = false)
        {
            p.Stroke = edit.Stroke;
            p.Valid = false;
            BrushStroke s = edit.Stroke;
            if (s == null || !Finite(s))
                return p;
            float maxRadius = admin ? Mathf.Max(EngineSettings.MaxRadiusValue, EngineSettings.AdminMaxRadius) : EngineSettings.MaxRadiusValue;
            float radius = Mathf.Clamp(s.Radius, 0.05f, maxRadius);
            float radius2 = Mathf.Clamp(s.Radius2, 0f, maxRadius);
            float paintRadius = Mathf.Clamp(s.EffectivePaintRadius, 0.05f, maxRadius);
            bool grid = edit.Has(EditFlags.GridAligned);
            p.HeightPrint = Footprint.Create(s.Shape, s.Center, radius, radius2, s.Rotation, s.Hardness, grid);
            float paintHardness = Mathf.Max(Mathf.Clamp01(s.Hardness), EngineSettings.PaintHardnessValue);
            p.PaintPrint = Footprint.Create(s.Shape, s.Center, paintRadius, radius2 * paintRadius / radius, s.Rotation, paintHardness, grid);
            p.CapValues(s, admin);
            p.Valid = true;
            return p;
        }

        private void CapValues(BrushStroke s, bool admin)
        {
            float maxAmount = admin ? Mathf.Max(EngineSettings.MaxAmountValue, EngineSettings.AdminMaxAmount) : EngineSettings.MaxAmountValue;
            Amount = s.Height == HeightOp.Offset ? Mathf.Clamp(s.Amount, -maxAmount, maxAmount) : Mathf.Min(Mathf.Abs(s.Amount), maxAmount);
            float maxStep = EngineSettings.MaxStepValue;
            MaxStep = s.MaxStep > 0f ? Mathf.Min(s.MaxStep, maxStep) : maxStep;
            Strength = Mathf.Clamp01(s.Strength);
            PaintStrength = Mathf.Clamp01(s.PaintStrength);
            Density = Mathf.Clamp01(s.Density);
            Share = Mathf.Clamp01(s.RandomShare);
        }

        /// <summary>Every number of the stroke is a real number (a broken or hostile package cannot poison the terrain).</summary>
        private static bool Finite(BrushStroke s)
        {
            return Ok(s.Center.x) && Ok(s.Center.y) && Ok(s.Center.z) && Ok(s.Radius) && Ok(s.Radius2) && Ok(s.Rotation)
                && Ok(s.Hardness) && Ok(s.Target) && Ok(s.Amount) && Ok(s.MaxStep) && Ok(s.Strength) && Ok(s.PaintRadius)
                && Ok(s.PaintStrength) && Ok(s.Density) && Ok(s.BandMin) && Ok(s.BandMax) && Ok(s.RandomShare);
        }

        public static bool Ok(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
