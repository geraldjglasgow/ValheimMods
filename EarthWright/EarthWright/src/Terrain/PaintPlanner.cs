namespace EarthWright.Terrain
{
    /// <summary>
    /// Plans the paint of a brush stroke on one heightmap: every paint cell under the paint footprint (the stroke's shape
    /// with the paint radius, cells centred half a metre off the vertices as <see cref="HeightView"/> describes) that
    /// passes the filters and, with the paint height check, is not above the stroke's reference height, is blended by
    /// PaintStrength times its footprint weight. The paint of a vertex set's entries goes through <see cref="AddCell"/>.
    /// </summary>
    public static class PaintPlanner
    {
        public static void Plan(HeightView view, TerrainEdit edit, StrokeParams p, EngineFilters filters, ChangeBuffer buffer)
        {
            PaintOp op = p.Stroke.Paint;
            bool snow = op == PaintOp.Cultivated && WorldGenerator.IsDeepnorth(p.Stroke.Center.x, p.Stroke.Center.z);
            bool heightCheck = edit.Has(EditFlags.PaintHeightCheck);
            p.PaintPrint.Extent(out float halfX, out float halfZ);
            view.RangeX(p.PaintPrint.CenterX - halfX, p.PaintPrint.CenterX + halfX, true, out int x0, out int x1);
            view.RangeZ(p.PaintPrint.CenterZ - halfZ, p.PaintPrint.CenterZ + halfZ, true, out int y0, out int y1);
            for (int y = y0; y <= y1; y++)
            {
                for (int x = x0; x <= x1; x++)
                    PlanCell(view, p, filters, heightCheck, snow, buffer, x, y);
            }
        }

        private static void PlanCell(HeightView view, StrokeParams p, EngineFilters filters, bool heightCheck, bool snow, ChangeBuffer buffer, int x, int y)
        {
            float cx = view.CellX(x);
            float cz = view.CellZ(y);
            float weight = p.PaintPrint.Weight(cx, cz) * p.PaintStrength;
            if (weight <= 0f)
                return;
            int i = y * view.Pitch + x;
            if (!filters.PassCell(view, i, cx, cz))
                return;
            if (heightCheck && view.Current(i) + view.OriginY > p.Stroke.Center.y)
                return;
            AddCell(view, i, p.Stroke.Paint, weight, p.Density, snow, buffer);
        }

        /// <summary>Records the paint of cell i when it changes anything.</summary>
        public static void AddCell(HeightView view, int i, PaintOp op, float weight, float density, bool snow, ChangeBuffer buffer)
        {
            UnityEngine.Color before = view.PaintAt(i);
            if (!PaintOps.Blend(op, before, view.OriginalAt(i), weight, density, snow, out UnityEngine.Color after, out bool forget))
                return;
            if (forget ? !view.PaintModifiedAt(i) : !EngineRecord.Differs(before, after))
                return;
            buffer.AddPaint(i, before, after, !forget);
        }
    }
}
