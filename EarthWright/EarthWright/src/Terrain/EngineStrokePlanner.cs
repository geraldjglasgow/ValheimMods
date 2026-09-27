namespace EarthWright.Terrain
{
    /// <summary>
    /// Plans a brush stroke on one heightmap: every height vertex under the height footprint (in world metres) that passes
    /// the filters gets its new height from the <see cref="HeightPlan"/>; the paint is planned by the
    /// <see cref="PaintPlanner"/> with the paint footprint. Only the vertices of this heightmap are visited, including its
    /// copy of the edge it shares with a neighbour, which the neighbour's owner computes the same way.
    /// </summary>
    public static class EngineStrokePlanner
    {
        public static void Plan(HeightView view, TerrainEdit edit, LimitContext limits, EngineFilters filters, HeightPlan heights, ChangeBuffer buffer)
        {
            StrokeParams p = StrokeParams.From(edit, limits.AdminEdit);
            if (!p.Valid)
                return;
            filters.Configure(edit, view);
            if (p.Stroke.Height != HeightOp.None)
            {
                heights.Configure(view, limits, p);
                PlanHeights(view, p, heights, filters, buffer);
            }
            if (p.Stroke.Paint != PaintOp.None)
                PaintPlanner.Plan(view, edit, p, filters, buffer);
        }

        private static void PlanHeights(HeightView view, StrokeParams p, HeightPlan plan, EngineFilters filters, ChangeBuffer buffer)
        {
            p.HeightPrint.Extent(out float halfX, out float halfZ);
            view.RangeX(p.HeightPrint.CenterX - halfX, p.HeightPrint.CenterX + halfX, false, out int x0, out int x1);
            view.RangeZ(p.HeightPrint.CenterZ - halfZ, p.HeightPrint.CenterZ + halfZ, false, out int y0, out int y1);
            for (int y = y0; y <= y1; y++)
            {
                for (int x = x0; x <= x1; x++)
                    PlanVertex(view, p, plan, filters, buffer, x, y);
            }
        }

        private static void PlanVertex(HeightView view, StrokeParams p, HeightPlan plan, EngineFilters filters, ChangeBuffer buffer, int x, int y)
        {
            float wx = view.VertexX(x);
            float wz = view.VertexZ(y);
            float weight = p.HeightPrint.Weight(wx, wz);
            if (weight <= 0f)
                return;
            int i = y * view.Pitch + x;
            if (!filters.PassVertex(view, i, wx, wz))
                return;
            float before = view.Current(i);
            float after = plan.After(x, y, i, before, weight, wx, wz);
            if (float.IsNaN(after))
                return;
            EngineRecord.Height(view, plan.Limits, buffer, i, wx, wz, before, after, plan.Op == HeightOp.Reset);
        }
    }
}
