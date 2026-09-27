namespace EarthWright.Terrain
{
    /// <summary>
    /// Owner side (the machine that owns the compiler's ZDO): plans an edit against the compiler, writes the changes into
    /// its arrays, then relaxes the surroundings when gentle slopes are on. The owner handler already stripped the
    /// Privileged and IgnoreLimits flags unless the server approved the edit, so here they mean an approved admin edit:
    /// IgnoreLimits lifts the height limits, Privileged lifts the size caps to the admin ceilings. The caller saves the
    /// compiler, which replicates it.
    /// </summary>
    public static class EngineApply
    {
        private static readonly HeightView view = new HeightView();
        private static readonly ChangeBuffer changes = new ChangeBuffer();
        private static readonly ChangeBuffer relaxed = new ChangeBuffer();
        private static readonly LimitContext limits = new LimitContext();

        public static EditResult Run(TerrainComp comp, TerrainEdit edit)
        {
            if (edit == null || !EngineViews.ForComp(comp, view))
                return default;
            view.UnderPiece = EngineViews.PieceProbeFor(edit);
            limits.Reset(edit.Has(EditFlags.IgnoreLimits), edit.Has(EditFlags.Privileged));
            EnginePlanner.Plan(view, edit, limits, changes);
            EnginePlanner.Write(view, changes);
            EditResult result = new EditResult
            {
                HeightChanged = changes.HeightCount > 0, PaintChanged = changes.PaintCount > 0, HitLimit = changes.HitLimit,
            };
            if (result.HeightChanged && SlopeRelax.Applies(edit))
            {
                SlopeRelax.Run(view, changes, limits, relaxed);
                EnginePlanner.Write(view, relaxed);
            }
            return result;
        }
    }
}
