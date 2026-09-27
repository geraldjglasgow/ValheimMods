namespace EarthWright.Terrain
{
    /// <summary>
    /// Plans one edit on one heightmap into a change buffer, the one entry point Apply (owner) and Estimate (sender)
    /// share: brush strokes through the stroke planners, vertex sets through the vertex planner. Pure: it reads only the
    /// view, the limit context and the synced settings, and writes only the buffer. Main thread only (shared scratch).
    /// </summary>
    public static class EnginePlanner
    {
        private static readonly EngineFilters filters = new EngineFilters();
        private static readonly HeightPlan heights = new HeightPlan();

        public static void Plan(HeightView view, TerrainEdit edit, LimitContext limits, ChangeBuffer buffer)
        {
            buffer.Clear();
            if (view == null || edit == null)
                return;
            if (edit.Kind == EditKind.Stroke && edit.Stroke != null)
                EngineStrokePlanner.Plan(view, edit, limits, filters, heights, buffer);
            else if (edit.Kind == EditKind.Vertices && edit.Vertices != null)
                EngineVertexPlanner.Plan(view, edit, limits, buffer);
        }

        /// <summary>Writes a plan into the view's compiler arrays (owner only; a view of a heightmap without compiler has none).</summary>
        public static void Write(HeightView view, ChangeBuffer buffer)
        {
            if (view.Level == null || view.Paint == null)
                return;
            for (int n = 0; n < buffer.HeightCount; n++)
            {
                HeightChange c = buffer.Heights[n];
                view.Level[c.Index] = c.NewLevel;
                view.Smooth[c.Index] = c.NewSmooth;
                view.Modified[c.Index] = c.NewModified;
            }
            for (int n = 0; n < buffer.PaintCount; n++)
            {
                PaintChange c = buffer.Paints[n];
                view.Paint[c.Index] = c.NewColor;
                view.PaintModified[c.Index] = c.NewModified;
            }
        }
    }
}
