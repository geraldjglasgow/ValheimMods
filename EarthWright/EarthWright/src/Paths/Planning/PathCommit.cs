using EarthWright.Actions;
using EarthWright.Brush;
using EarthWright.Core;
using EarthWright.Costs;
using EarthWright.Terrain;

namespace EarthWright.Paths
{
    /// <summary>
    /// Builds a ramp or road: plans the draft afresh, turns the plan into one vertex edit (targets with weights and
    /// paint), runs the building hooks and the sender guards (<see cref="Dispatcher.Check"/>), charges the costs, and
    /// sends it through the dispatcher to the owner of every terrain compiler it touches. The same order as a brush
    /// click: a refused edit costs nothing, and the undo history records the whole ramp or road as one step.
    /// </summary>
    public static class PathCommit
    {
        /// <summary>True when the edit was sent; otherwise the reason has been shown to the player.</summary>
        public static bool Commit(PathDraft draft, ToolAction action, string source)
        {
            Player player = Player.m_localPlayer;
            if (player == null || draft == null)
                return false;
            TerrainEdit edit = Prepare(draft, action, source, out string reason);
            if (edit == null)
            {
                Messages.Center(reason);
                return false;
            }
            if (!CostApi.TryCharge(player, action, Engine.Estimate(edit)))
                return false;
            Dispatcher.SendChecked(edit);
            return true;
        }

        /// <summary>Plans and checks the draft: the edit ready to send, or null and the reason.</summary>
        private static TerrainEdit Prepare(PathDraft draft, ToolAction action, string source, out string reason)
        {
            reason = BrushCaps.LevelUnlocks(source) ? null : PathWords.Text("level_locked");
            if (reason != null)
                return null;
            // The plan's limit check already used the admin override when it is held (the same rule the building hook applies).
            PathPlan plan = PathPlanner.Plan(draft);
            reason = plan.FirstProblem ?? (plan.Vertices.Count == 0 ? PathWords.Text("nothing") : null);
            if (reason != null)
                return null;
            TerrainEdit edit = EditFor(plan, action, source);
            reason = Dispatcher.Check(edit);
            return string.IsNullOrEmpty(reason) ? edit : null;
        }

        /// <summary>The vertex edit of a plan. Admin-only entries make privileged edits, which the server checks.</summary>
        public static TerrainEdit EditFor(PathPlan plan, ToolAction action, string source)
        {
            VertexSet set = new VertexSet { Mode = VertexMode.Targets };
            foreach (PlannedVertex vertex in plan.Vertices)
            {
                set.Targets.Add(new TargetVertex
                {
                    X = vertex.X, Z = vertex.Z, Height = vertex.Target, Weight = vertex.Weight,
                    Paint = vertex.Paint, PaintStrength = vertex.Paint == PaintOp.None ? 0f : 1f,
                });
            }
            // The entry's piece id is the source other modules key on (protection, cooldown, undo names); the tool key is the fallback.
            TerrainEdit edit = TerrainEdit.ForVertices(set, action != null ? action.Id : source);
            if (action != null && action.AdminOnly)
                edit.Flags |= EditFlags.Privileged;
            return edit;
        }
    }
}
