using System.Collections.Generic;
using System.Linq;
using OpenKeep.Blueprints.Sites;
using OpenKeep.Core;

namespace OpenKeep.Blueprints.Planner
{
    /// <summary>
    /// Changes to a site's build queue from this machine: queue the selection, move an entry up or down, remove one.
    /// Each reads the queue from the site's ZDO as it is now, changes it and sends the whole new queue to the marker's
    /// owner (<see cref="QueueRpc"/>), who checks and writes it. A move or removal names the entry it means, so a
    /// queue another player changed meanwhile is left alone.
    /// </summary>
    public static class QueueEdits
    {
        /// <summary>The most entries a site's queue holds (the owner drops the rest).</summary>
        public const int MaxEntries = 200;

        /// <summary>Enter: the selection's pieces that are neither built nor queued yet become a new last entry ("Selection N", or "House N" after smart select).</summary>
        public static void QueueSelection()
        {
            SiteMarker site = PlannerSelection.Site;
            List<SiteSelection> queue = site.State.Queue;
            List<int> pieces = NotQueued(queue, PlannerSelection.Unbuilt());
            if (pieces.Count == 0)
            {
                Messages.Center(PlannerWords.NothingNew);
                PlannerSelection.Clear();
                return;
            }
            if (queue.Count >= MaxEntries)
            {
                Messages.Center(BlueprintWords.Format(PlannerWords.QueueFull, MaxEntries));
                return;
            }
            string name = NewName(queue, PlannerSelection.Smart ? PlannerWords.HouseName : PlannerWords.SelectionName);
            queue.Add(new SiteSelection { Name = name, Pieces = pieces });
            Send(site, queue);
            Messages.Center(BlueprintWords.Format(PlannerWords.Queued, name, pieces.Count));
            PlannerSelection.Clear();
        }

        /// <summary>Moves the named entry at the index one place up (-1) or down (+1).</summary>
        public static void Move(SiteMarker site, int index, int step, string name)
        {
            List<SiteSelection> queue = site != null ? site.State.Queue : null;
            int other = index + step;
            if (!Holds(queue, index, name) || other < 0 || other >= queue.Count)
                return;
            (queue[index], queue[other]) = (queue[other], queue[index]);
            Send(site, queue);
        }

        public static void Remove(SiteMarker site, int index, string name)
        {
            List<SiteSelection> queue = site != null ? site.State.Queue : null;
            if (!Holds(queue, index, name))
                return;
            queue.RemoveAt(index);
            Send(site, queue);
        }

        private static void Send(SiteMarker site, List<SiteSelection> queue)
        {
            QueueRpc.Send(site, queue);
            PanelModel.Invalidate();
        }

        private static bool Holds(List<SiteSelection> queue, int index, string name) =>
            queue != null && index >= 0 && index < queue.Count && queue[index].Name == name;

        /// <summary>The pieces no entry holds yet: a piece is built at its first place in the queue, so it is queued once.</summary>
        private static List<int> NotQueued(List<SiteSelection> queue, List<int> pieces)
        {
            HashSet<int> queued = new HashSet<int>(queue.SelectMany(s => s.Pieces));
            return pieces.Where(p => !queued.Contains(p)).ToList();
        }

        /// <summary>"Selection 3": the number is the entry's place, raised until no other entry has the name.</summary>
        private static string NewName(List<SiteSelection> queue, string token)
        {
            for (int n = queue.Count + 1; ; n++)
            {
                string name = BlueprintWords.Format(token, n);
                if (!queue.Exists(s => s.Name == name))
                    return name;
            }
        }
    }
}
