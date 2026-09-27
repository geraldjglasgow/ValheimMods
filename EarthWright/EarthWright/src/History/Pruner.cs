using EarthWright.Core;
using EarthWright.Terrain;
using UnityEngine;

namespace EarthWright.History
{
    /// <summary>
    /// Narrows a recorded step to what its edits really changed. The recorder copies a generous square around each
    /// edit (it cannot know beforehand which vertices the owner will change); once the edit has been applied and this
    /// machine's copy is up to date (<see cref="SendTracker.Settled"/>: at once for ground this machine owns, a few
    /// seconds later otherwise), every freshly recorded index whose value is still the recorded one is dropped. So an
    /// undo later puts back only the player's own changes: it does not revert what someone else changed next to them
    /// afterwards, and it is not refused by a ward that merely stands next to the stroke. A step left with nothing is
    /// removed from the list.
    /// </summary>
    internal static class Pruner
    {
        private const float Interval = 0.5f;
        private static float nextTick;

        public static void Initialize()
        {
            EditEvents.Sent += edit => Safe.Run("EarthWright undo prune", () => AfterSend(edit));
            Ticker.OnUpdate("EarthWright undo prune", Tick);
        }

        /// <summary>Right after an edit left: ground this machine owns has already changed, so the open step can be narrowed now.</summary>
        private static void AfterSend(TerrainEdit edit)
        {
            if (edit == null || edit.Has(EditFlags.IsRestore) || Timeline.Open == null)
                return;
            PruneStep(Timeline.Open);
            Timeline.DropEmpty();
        }

        /// <summary>Twice a second: narrows the steps whose ground has settled since.</summary>
        private static void Tick()
        {
            if (Time.time < nextTick || Timeline.Undo.Count == 0)
                return;
            nextTick = Time.time + Interval;
            bool pruned = false;
            foreach (Step step in Timeline.Undo)
                pruned |= PruneStep(step);
            if (pruned)
                Timeline.DropEmpty();
        }

        /// <summary>True when any record of the step was checked.</summary>
        private static bool PruneStep(Step step)
        {
            bool pruned = false;
            foreach (CompRecord record in step.Comps.Values)
            {
                if (record.Fresh.Count > 0 && SendTracker.Settled(record.Key))
                    pruned |= Prune(record);
            }
            return pruned;
        }

        /// <summary>Drops the fresh indices whose ground still holds the recorded values. False when the ground is not loaded.</summary>
        private static bool Prune(CompRecord record)
        {
            Heightmap map = RawAccess.MapAt(record.Position);
            if (map == null)
                return false;
            TerrainComp comp = RawAccess.CompOf(map);
            foreach (int index in record.Fresh)
            {
                if (record.Values.TryGetValue(index, out RawVertex before) && RawAccess.Same(RawAccess.Read(map, comp, index), before))
                    record.Values.Remove(index);
            }
            record.Fresh.Clear();
            return true;
        }
    }
}
