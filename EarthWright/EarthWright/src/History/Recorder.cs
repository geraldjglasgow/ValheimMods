using System.Collections.Generic;
using EarthWright.Core;
using EarthWright.Terrain;
using UnityEngine;

namespace EarthWright.History
{
    /// <summary>
    /// Records the undo history. Just before an edit leaves this machine (<see cref="EditEvents.BeforeSend"/>, after
    /// the guards passed) it copies the raw compiler values of every index the edit may touch, per compiler, into the
    /// current step. Edits flagged <see cref="EditFlags.IsRestore"/> (undo, redo, snapshot restores) are not recorded.
    /// Every edit, recorded or not, is noted for <see cref="SendTracker"/>.
    /// </summary>
    internal static class Recorder
    {
        public static void Initialize()
        {
            EditEvents.BeforeSend += edit => Safe.Run("EarthWright undo snapshot", () => OnBeforeSend(edit));
        }

        private static void OnBeforeSend(TerrainEdit edit)
        {
            if (edit == null || Side.IsDedicated)
                return;
            List<TerrainComp> comps = Dispatcher.Compilers(edit);
            foreach (TerrainComp comp in comps)
                SendTracker.Note(comp, edit);
            if (edit.Has(EditFlags.IsRestore))
                return;
            Step open = Joinable(edit);
            Step step = open ?? new Step(new[] { edit.Source }, Carried(edit)) { Press = PressTracker.Current };
            foreach (TerrainComp comp in comps)
                Snapshot(step, comp, edit);
            step.Edits++;
            step.LastSend = Time.time;
            if (open == null && !step.IsEmpty)
            {
                Timeline.Record(step);
                Timeline.Open = step;
            }
        }

        /// <summary>The open step when this edit belongs to it: sent during the same held press, or within the group window.</summary>
        private static Step Joinable(TerrainEdit edit)
        {
            Step open = Timeline.Open;
            if (open == null || Timeline.Undo.Count == 0 || Timeline.Undo[Timeline.Undo.Count - 1] != open)
                return null;
            int press = PressTracker.Current;
            bool samePress = press >= 0 && open.Press == press;
            bool soon = Time.time - open.LastSend <= HistorySettings.GroupWindow.Value;
            if (!samePress && !soon)
                return null;
            open.AddSource(edit.Source);
            open.Carry |= Carried(edit);
            return open;
        }

        private static EditFlags Carried(TerrainEdit edit) => edit.Flags & (EditFlags.Privileged | EditFlags.IgnoreLimits);

        /// <summary>
        /// Copies the values this edit may change on one compiler; indices already in the step keep their first value.
        /// New indices are marked fresh: once the edit has been applied, <see cref="Pruner"/> drops those it left alone.
        /// </summary>
        private static void Snapshot(Step step, TerrainComp comp, TerrainEdit edit)
        {
            Heightmap map = comp.m_hmap;
            if (map == null || !comp.m_initialized)
                return;
            CompRecord record = step.For(map);
            foreach (int index in AreaIndices.For(edit, map))
            {
                if (record.Values.ContainsKey(index))
                    continue;
                record.Values[index] = RawAccess.Read(map, comp, index);
                record.Fresh.Add(index);
            }
        }
    }
}
