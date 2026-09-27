using UnityEngine;

namespace EarthWright.Terrain
{
    /// <summary>
    /// Plans a vertex set on one heightmap. Targets (ramps, roads): each entry names a world vertex (whole metres); the
    /// height blends from the current one to the target by the entry's weight, within the height limits, and the cell
    /// with the same index takes the entry's paint. Restore (undo): raw compiler values by index, written as they are,
    /// no limits. An index is used once per plan (the first entry wins), and a set is cut at
    /// <see cref="MaxEntries"/> entries so no package can make an owner work without end.
    /// </summary>
    public static class EngineVertexPlanner
    {
        /// <summary>Entries read per heightmap; a heightmap has 4225 vertices, so this leaves room for any honest set.</summary>
        public const int MaxEntries = 16384;

        private static int[] visited = new int[0];
        private static int visit;

        public static void Plan(HeightView view, TerrainEdit edit, LimitContext limits, ChangeBuffer buffer)
        {
            BeginVisits(view.Count);
            if (edit.Vertices.Mode == VertexMode.Restore)
                PlanRestore(view, edit.Vertices, buffer);
            else
                PlanTargets(view, edit.Vertices, limits, buffer);
        }

        private static void PlanTargets(HeightView view, VertexSet set, LimitContext limits, ChangeBuffer buffer)
        {
            int count = Mathf.Min(set.Targets.Count, MaxEntries);
            for (int n = 0; n < count; n++)
            {
                TargetVertex t = set.Targets[n];
                if (!view.TryVertex(t.X, t.Z, out int x, out int y))
                    continue;
                int i = y * view.Pitch + x;
                if (FirstVisit(i))
                    PlanTarget(view, t, limits, buffer, x, y, i);
            }
        }

        private static void PlanTarget(HeightView view, TargetVertex t, LimitContext limits, ChangeBuffer buffer, int x, int y, int i)
        {
            float wx = view.VertexX(x);
            float wz = view.VertexZ(y);
            float before = view.Current(i);
            if (view.UnderPiece != null && view.UnderPiece(wx, before + view.OriginY, wz))
                return;
            float weight = StrokeParams.Ok(t.Weight) ? Mathf.Clamp01(t.Weight) : 0f;
            if (weight > 0f && StrokeParams.Ok(t.Height))
            {
                float after = before + (t.Height - view.OriginY - before) * weight;
                EngineRecord.Height(view, limits, buffer, i, wx, wz, before, after, false);
            }
            float paintWeight = StrokeParams.Ok(t.PaintStrength) ? Mathf.Clamp01(t.PaintStrength) : 0f;
            if (t.Paint != PaintOp.None && paintWeight > 0f)
                PaintPlanner.AddCell(view, i, t.Paint, paintWeight, 1f, false, buffer);
        }

        /// <summary>Raw values by index, only on the heightmap the set names (indices mean nothing on another one).</summary>
        private static void PlanRestore(HeightView view, VertexSet set, ChangeBuffer buffer)
        {
            if (!view.Holds(set.CompPosition))
                return;
            int count = Mathf.Min(set.Raw.Count, MaxEntries);
            for (int n = 0; n < count; n++)
            {
                RawVertex r = set.Raw[n];
                if (r.Index < 0 || r.Index >= view.Count || !FirstVisit(r.Index) || !Finite(r))
                    continue;
                RestoreHeight(view, r, buffer);
                RestorePaint(view, r, buffer);
            }
        }

        private static void RestoreHeight(HeightView view, RawVertex r, ChangeBuffer buffer)
        {
            int i = r.Index;
            if (r.HeightModified == view.ModifiedAt(i) && r.LevelDelta == view.LevelAt(i) && r.SmoothDelta == view.SmoothAt(i))
                return;
            float b = view.Base[i];
            float after = Mathf.Clamp(b + r.LevelDelta + r.SmoothDelta, b - view.Absolute, b + view.Absolute);
            buffer.AddRawHeight(i, view.Current(i), after, r.LevelDelta, r.SmoothDelta, r.HeightModified);
        }

        private static void RestorePaint(HeightView view, RawVertex r, ChangeBuffer buffer)
        {
            Color before = view.PaintAt(r.Index);
            if (r.PaintModified == view.PaintModifiedAt(r.Index) && before == r.Paint)
                return;
            buffer.AddPaint(r.Index, before, r.Paint, r.PaintModified);
        }

        private static bool Finite(RawVertex r)
        {
            return StrokeParams.Ok(r.LevelDelta) && StrokeParams.Ok(r.SmoothDelta) && StrokeParams.Ok(r.Paint.r)
                && StrokeParams.Ok(r.Paint.g) && StrokeParams.Ok(r.Paint.b) && StrokeParams.Ok(r.Paint.a);
        }

        /// <summary>Starts a new plan's visit marks (a counter instead of clearing the array).</summary>
        private static void BeginVisits(int count)
        {
            if (visited.Length < count)
                visited = new int[count];
            visit++;
            if (visit != int.MaxValue)
                return;
            System.Array.Clear(visited, 0, visited.Length);
            visit = 1;
        }

        private static bool FirstVisit(int i)
        {
            if (visited[i] == visit)
                return false;
            visited[i] = visit;
            return true;
        }
    }
}
