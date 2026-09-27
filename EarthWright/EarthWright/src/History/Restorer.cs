using System.Collections.Generic;
using System.Linq;
using EarthWright.Terrain;

namespace EarthWright.History
{
    /// <summary>What happened when recorded values were sent back.</summary>
    internal sealed class RestoreOutcome
    {
        /// <summary>The values the ground had just before the restore, for the opposite list (redo after an undo).</summary>
        public Step Inverse;

        /// <summary>Records that were not sent (refused), to hand back.</summary>
        public readonly List<CompRecord> Left = new List<CompRecord>();

        /// <summary>A heightmap of the step is not loaded here; nothing was sent.</summary>
        public bool TooFar;

        /// <summary>The sender guards refused before anything was sent; the reason to show.</summary>
        public string Refusal;

        /// <summary>Compilers an edit was sent to.</summary>
        public int Sent;
    }

    /// <summary>
    /// Puts recorded raw values back: per compiler a <see cref="VertexSet"/> in Restore mode, flagged
    /// <see cref="EditFlags.IsRestore"/>, through <see cref="Dispatcher.Submit"/>, so the owner applies it and the
    /// protection guards still apply. Only values that differ from the ground now are sent (all of them while an edit
    /// of ours to that compiler may still be on its way). Every heightmap must be loaded, and the sender guards are
    /// asked for every part first, so a step is normally restored whole or not at all.
    /// </summary>
    internal static class Restorer
    {
        private sealed class Plan
        {
            public CompRecord Original;
            public CompRecord Current;
            public TerrainEdit Edit;
        }

        public static RestoreOutcome Apply(Step step, string source)
        {
            RestoreOutcome outcome = new RestoreOutcome { Inverse = step.Sibling() };
            List<Plan> plans = new List<Plan>();
            foreach (CompRecord record in step.Comps.Values.Where(r => r.Values.Count > 0))
            {
                if (!TryPlan(record, source, step.Carry, out Plan plan))
                {
                    outcome.TooFar = true;
                    return outcome;
                }
                if (plan != null)
                    plans.Add(plan);
            }
            outcome.Refusal = plans.Select(p => EditGuards.CheckSender(p.Edit)).FirstOrDefault(r => !string.IsNullOrEmpty(r));
            if (outcome.Refusal != null)
                return outcome;
            foreach (Plan plan in plans)
                Send(plan, outcome);
            return outcome;
        }

        /// <summary>False when the heightmap is not loaded; plan null when nothing differs.</summary>
        private static bool TryPlan(CompRecord record, string source, EditFlags carry, out Plan plan)
        {
            plan = null;
            Heightmap map = RawAccess.MapAt(record.Position);
            if (map == null)
                return false;
            TerrainComp comp = RawAccess.CompOf(map);
            bool settled = SendTracker.Settled(record.Key);
            VertexSet set = new VertexSet { Mode = VertexMode.Restore, CompPosition = map.transform.position };
            CompRecord current = new CompRecord { Position = map.transform.position };
            foreach (KeyValuePair<int, RawVertex> value in record.Values)
            {
                RawVertex now = RawAccess.Read(map, comp, value.Key);
                if (settled && RawAccess.Same(now, value.Value))
                    continue;
                set.Raw.Add(value.Value);
                current.Values[value.Key] = now;
            }
            if (set.Raw.Count > 0)
                plan = new Plan { Original = record, Current = current, Edit = RestoreEdit(set, source, carry) };
            return true;
        }

        private static TerrainEdit RestoreEdit(VertexSet set, string source, EditFlags carry)
        {
            TerrainEdit edit = TerrainEdit.ForVertices(set, source);
            edit.Flags |= EditFlags.IsRestore | carry;
            return edit;
        }

        private static void Send(Plan plan, RestoreOutcome outcome)
        {
            if (Dispatcher.Submit(plan.Edit))
            {
                outcome.Inverse.Add(plan.Current);
                outcome.Sent++;
            }
            else
            {
                outcome.Left.Add(plan.Original);
            }
        }

        /// <summary>The records of a step that were not sent, as a step of their own to hand back.</summary>
        public static Step Remainder(Step step, RestoreOutcome outcome)
        {
            Step rest = step.Sibling();
            foreach (CompRecord record in outcome.Left)
                rest.Add(record);
            rest.Started = step.Started;
            rest.LastSend = step.LastSend;
            return rest;
        }
    }
}
