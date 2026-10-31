using System.Collections.Generic;
using UnityEngine;

namespace HighSeatPlus.Meshes
{
    /// <summary>
    /// Every combined group, by key, and the work on them. The world is cut into cells of <see cref="CellSize"/>
    /// metres each way (up and down too, so a tall tower is several cells): small enough that a change rebuilds one
    /// cell's pieces (about 150 in a dense base, some 60,000 vertices, a millisecond or two) and that a group's LOD
    /// reference point stays within about 14 m of its pieces' own, large enough that a dense base is drawn by about a
    /// dozen renderers per cell instead of one or more per piece. Groups that may need work wait in a queue: each
    /// frame up to a given number are looked at, those still being touched go to the back, and rebuilds stop once the
    /// frame's vertex budget is spent. <see cref="Sweep"/> walks the shown builds a few parts a frame and opens a group
    /// whose piece changed in a way no patch saw.
    /// </summary>
    internal static class CellGroups
    {
        public const float CellSize = 16f;

        private static readonly Dictionary<GroupKey, CellGroup> Map = new Dictionary<GroupKey, CellGroup>();
        private static readonly List<CellGroup> All = new List<CellGroup>();
        private static readonly Queue<CellGroup> Work = new Queue<CellGroup>();
        private static int _sweepGroup;
        private static int _sweepMember;

        /// <summary>Vertices in every build now.</summary>
        public static int Vertices { get; private set; }

        public static void Join(PieceEntry entry, float now)
        {
            if (!Map.TryGetValue(entry.Key, out CellGroup group))
            {
                group = new CellGroup(entry.Key);
                Map.Add(entry.Key, group);
                group.AllIndex = All.Count;
                All.Add(group);
            }
            group.Add(entry, now);
        }

        public static void Queue(CellGroup group)
        {
            if (group.Queued || group.Discarded)
            {
                return;
            }
            group.Queued = true;
            Work.Enqueue(group);
        }

        /// <summary>Looks at up to <paramref name="groups"/> waiting groups, rebuilding while the budget lasts.</summary>
        public static void Process(float now, int groups, int vertexBudget)
        {
            for (int i = 0; i < groups && Work.Count > 0; i++)
            {
                CellGroup group = Work.Dequeue();
                group.Queued = false;
                if (group.Discarded)
                {
                    continue;
                }
                if (group.Ready(now))
                {
                    vertexBudget -= group.Settle(now, vertexBudget);
                }
                if (!group.Discarded && (group.Dirty || group.Open))
                {
                    Queue(group);
                }
            }
        }

        /// <summary>Checks the pieces of shown builds, about <paramref name="parts"/> renderers a frame.</summary>
        public static void Sweep(int parts)
        {
            int steps = parts * 2;
            while (parts > 0 && steps-- > 0 && All.Count > 0)
            {
                if (_sweepGroup >= All.Count)
                {
                    _sweepGroup = 0;
                }
                CellGroup group = All[_sweepGroup];
                if (group.Open || group.Build == null || _sweepMember >= group.Members.Count)
                {
                    _sweepGroup++;
                    _sweepMember = 0;
                    continue;
                }
                PieceEntry entry = group.Members[_sweepMember++];
                parts -= entry.Parts.Count;
                if (entry.InBuild && !PartChecks.Valid(entry))
                {
                    PieceRegistry.Touch(entry);
                }
            }
        }

        /// <summary>The old build is replaced by the new one (either may be null).</summary>
        public static void Replaced(GroupBuild old, GroupBuild build)
        {
            if (old != null)
            {
                Vertices -= old.Vertices;
                old.Destroy();
            }
            if (build != null)
            {
                Vertices += build.Vertices;
            }
        }

        /// <summary>An empty group: its build goes, and so does the group.</summary>
        public static void Discard(CellGroup group)
        {
            if (group.Discarded)
            {
                return;
            }
            group.Discarded = true;
            Replaced(group.Build, null);
            group.Build = null;
            Map.Remove(group.Key);
            int index = group.AllIndex;
            int last = All.Count - 1;
            if (index >= 0 && index <= last && All[index] == group)
            {
                All[index] = All[last];
                All[index].AllIndex = index;
                All.RemoveAt(last);
            }
        }

        /// <summary>Sets every build's LOD heights again after the LOD bias or field of view changed.</summary>
        public static void RefreshLods()
        {
            for (int i = 0; i < All.Count; i++)
            {
                All[i].Build?.UpdateLod();
            }
        }

        /// <summary>Every piece drawn by the game again and every build destroyed.</summary>
        public static void Clear()
        {
            for (int i = 0; i < All.Count; i++)
            {
                CellGroup group = All[i];
                for (int m = 0; m < group.Members.Count; m++)
                {
                    PieceEntry entry = group.Members[m];
                    entry.SetForced(false);
                    entry.Group = null;
                    entry.InBuild = false;
                }
                group.Members.Clear();
                group.Discarded = true;
                group.Build?.Destroy();
                group.Build = null;
            }
            All.Clear();
            Map.Clear();
            Work.Clear();
            Vertices = 0;
            _sweepGroup = 0;
            _sweepMember = 0;
        }
    }
}
