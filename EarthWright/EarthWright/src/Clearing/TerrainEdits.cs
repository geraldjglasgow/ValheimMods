using System.Collections.Generic;
using EarthWright.Brush;
using EarthWright.Terrain;
using UnityEngine;

namespace EarthWright.Clearing
{
    /// <summary>
    /// Turns an <c>ew terrain</c> request into a privileged edit. Brush-like operations (level, raise, lower, min, max,
    /// offset, remove, reset, paint) become a stroke around the player (or the brush with at=brush) with a hard edge
    /// and no step limit; the level target is the player's feet unless a value is given. With include=/ignore= the
    /// stroke's result is computed here (<see cref="Engine.Estimate"/>) and sent as explicit vertices, minus the ones the
    /// object filter drops. The band and slope operations are explicit vertices from the start (<see cref="VertexEdits"/>).
    /// </summary>
    public static class TerrainEdits
    {
        public const string Source = "command:terrain";
        private const float DefaultRadius = 10f;

        private static readonly Dictionary<string, HeightOp> strokeOps = new Dictionary<string, HeightOp>
        {
            ["level"] = HeightOp.Level, ["raise"] = HeightOp.Raise, ["lower"] = HeightOp.Lower,
            ["min"] = HeightOp.SetMin, ["max"] = HeightOp.SetMax, ["offset"] = HeightOp.Offset,
            ["remove"] = HeightOp.RemoveSurface, ["reset"] = HeightOp.Reset, ["paint"] = HeightOp.None,
        };

        /// <summary>The edit for the request, or null with the reason in <paramref name="error"/>.</summary>
        public static TerrainEdit Build(TerrainRequest request, Player player, out string error)
        {
            if (request.Op == "slope")
                return VertexEdits.Slope(request, player, out error);
            if (request.Op == "band")
                return VertexEdits.Band(request, Footprint(request, player), out error);
            if (strokeOps.TryGetValue(request.Op, out HeightOp op))
                return Stroke(request, player, op, out error);
            error = $"unknown operation '{request.Op}' (see 'ew terrain help')";
            return null;
        }

        /// <summary>The flags every edit of the request carries: privileged (checked by the server), and the typed filters.</summary>
        public static EditFlags Flags(TerrainRequest request)
        {
            EditFlags flags = EditFlags.Privileged;
            if (request.Unlimited)
                flags |= EditFlags.IgnoreLimits;
            if (request.SkipPieces)
                flags |= EditFlags.SkipUnderPieces;
            return flags;
        }

        /// <summary>
        /// The footprint: a circle around the player, or with at=brush the brush itself (its centre, target height,
        /// shape, second size and rotation; a typed radius or shape= replaces the brush's own).
        /// </summary>
        public static BrushStroke Footprint(TerrainRequest request, Player player)
        {
            bool brush = request.AtBrush && BrushAim.Tracking;
            float reference = brush ? BrushState.TargetHeight : player.transform.position.y;
            Vector3 center = brush ? BrushState.Center : player.transform.position;
            BrushStroke stroke = new BrushStroke
            {
                Center = new Vector3(center.x, reference, center.z), Shape = request.Shape ?? BrushShape.Circle,
                Radius = request.Radius ?? DefaultRadius, Hardness = request.Hardness, MaxStep = 0f, RandomShare = request.Share, Seed = request.Seed,
            };
            if (brush)
                CopyBrush(stroke, request);
            stroke.Radius = Mathf.Clamp(stroke.Radius, 0.5f, Mathf.Min(ClearingSettings.AdminMaxRadius, EngineSettings.MaxRadiusValue));
            return stroke;
        }

        private static void CopyBrush(BrushStroke stroke, TerrainRequest request)
        {
            stroke.Shape = request.Shape ?? BrushState.Shape;
            stroke.Radius = request.Radius ?? BrushState.Radius;
            stroke.Radius2 = BrushState.Radius2;
            stroke.Rotation = BrushState.Rotation;
        }

        private static TerrainEdit Stroke(TerrainRequest request, Player player, HeightOp op, out string error)
        {
            error = null;
            BrushStroke stroke = Footprint(request, player);
            SetOperation(stroke, request, op);
            if (op == HeightOp.None && stroke.Paint == PaintOp.None)
            {
                error = "paint needs paint=<dirt|paved|cultivated|grass|original|clearvegetation>";
                return null;
            }
            TerrainEdit edit = TerrainEdit.ForStroke(stroke, Source);
            edit.Flags = Flags(request);
            if (request.HasBand)
                edit.Flags |= EditFlags.HeightBand;
            if (!request.HasObjectFilter)
                return edit;
            return op == HeightOp.None ? VertexEdits.PaintOnly(request, stroke, out error) : Filtered(edit, request, out error);
        }

        private static void SetOperation(BrushStroke stroke, TerrainRequest request, HeightOp op)
        {
            stroke.Height = op;
            stroke.Style = LevelStyle.Instant;
            stroke.Target = request.Value ?? stroke.Center.y;
            stroke.Amount = request.Value ?? (op == HeightOp.Offset ? 0f : 1f);
            stroke.Paint = request.Paint != PaintOp.None ? request.Paint : op == HeightOp.Reset ? PaintOp.Original : PaintOp.None;
            stroke.BandMin = request.BandMin ?? 0f;
            stroke.BandMax = request.BandMax ?? 0f;
        }

        /// <summary>The stroke's own result as explicit vertices (weight 1), keeping only those the object filter keeps.</summary>
        private static TerrainEdit Filtered(TerrainEdit edit, TerrainRequest request, out string error)
        {
            BrushStroke stroke = edit.Stroke;
            ObjectFilter filter = ObjectFilter.For(request, stroke.Center, stroke.Reach);
            VertexSet set = new VertexSet { Mode = VertexMode.Targets };
            foreach (VertexChange change in Engine.Estimate(edit, withChanges: true).Changes)
            {
                if (filter.Keeps(change.X, change.Z))
                    set.Targets.Add(VertexEdits.Target(Mathf.RoundToInt(change.X), Mathf.RoundToInt(change.Z), change.After, 1f, stroke.Paint));
            }
            return VertexEdits.Finish(set, Flags(request), out error);
        }
    }
}
