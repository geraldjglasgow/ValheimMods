using System.Collections.Generic;
using UnityEngine;

namespace HighSeatPlus.Meshes
{
    /// <summary>
    /// The pieces of one <see cref="GroupKey"/> and their combined build. A build is shown, with its pieces turned off,
    /// or open: hidden, with every piece it holds drawing itself again. Anything that changes or may change how one of
    /// its pieces looks opens it at once, in the same frame, so the game's own renderers show the change (a highlight,
    /// a worn look, a glow, a removed piece) and nothing is ever drawn twice or missing; opening costs one flag per
    /// renderer and no mesh work. Once nothing in the group has been touched for <see cref="CombinedMeshes.SettleSeconds"/>
    /// the group settles (<see cref="CellGroups"/>): a build whose pieces all still look as they did is shown again as
    /// it is (a hammer highlight ends this way), otherwise the group is rebuilt (<see cref="GroupBuilder"/>). A new
    /// member does not open the build: it draws itself until the next rebuild, <see cref="JoinDelaySeconds"/> after the
    /// last one joined, so a burst of loaded pieces makes one rebuild.
    /// </summary>
    internal sealed class CellGroup
    {
        public const float JoinDelaySeconds = 1f;

        public readonly GroupKey Key;
        public readonly Vector3 Origin;
        public readonly List<PieceEntry> Members = new List<PieceEntry>();

        public GroupBuild Build;
        public bool Open;
        public bool Dirty;
        public bool Queued;
        public bool Discarded;
        public float QuietUntil;
        public int AllIndex = -1;

        public CellGroup(GroupKey key)
        {
            Key = key;
            float size = CellGroups.CellSize;
            Origin = new Vector3((key.X + 0.5f) * size, (key.Y + 0.5f) * size, (key.Z + 0.5f) * size);
        }

        public bool Ready(float now)
        {
            return now >= QuietUntil;
        }

        public void Add(PieceEntry entry, float now)
        {
            entry.Group = this;
            entry.MemberIndex = Members.Count;
            entry.InBuild = false;
            Members.Add(entry);
            Dirty = true;
            QuietUntil = Mathf.Max(QuietUntil, now + JoinDelaySeconds);
            CellGroups.Queue(this);
        }

        /// <summary>Something about this member changed or may be changing.</summary>
        public void Touched(PieceEntry entry)
        {
            WaitFor(entry);
            if (entry.InBuild)
            {
                Hide();
            }
            CellGroups.Queue(this);
        }

        /// <summary>The member is gone or leaves for good: its look must not stay in the build.</summary>
        public void Leave(PieceEntry entry)
        {
            if (entry.InBuild)
            {
                Hide();
                Dirty = true;
            }
            Drop(entry);
            if (Members.Count == 0)
            {
                CellGroups.Discard(this);
                return;
            }
            CellGroups.Queue(this);
        }

        /// <summary>Takes the member out of the list only; the caller handles the build.</summary>
        public void Drop(PieceEntry entry)
        {
            entry.SetForced(false);
            int index = entry.MemberIndex;
            int last = Members.Count - 1;
            if (index >= 0 && index <= last && Members[index] == entry)
            {
                Members[index] = Members[last];
                Members[index].MemberIndex = index;
                Members.RemoveAt(last);
            }
            entry.Group = null;
            entry.MemberIndex = -1;
            entry.InBuild = false;
        }

        /// <summary>No settling before this member has settled.</summary>
        public void WaitFor(PieceEntry entry)
        {
            float settled = Mathf.Max(entry.TouchedAt + CombinedMeshes.SettleSeconds, entry.RetryAt);
            QuietUntil = Mathf.Max(QuietUntil, settled);
        }

        /// <summary>Hides the build and lets every piece in it draw itself.</summary>
        public void Hide()
        {
            if (Open || Build == null)
            {
                return;
            }
            Open = true;
            Build.SetShown(false);
            for (int i = 0; i < Members.Count; i++)
            {
                Members[i].SetForced(false);
            }
        }

        /// <summary>A quiet group: shows the hidden build again when nothing in it changed, or rebuilds. Returns the
        /// vertices combined, or 0.</summary>
        public int Settle(float now, int budget)
        {
            if (Open && !Dirty)
            {
                if (AllValid())
                {
                    Show();
                    return 0;
                }
                Dirty = true;
            }
            if (!Dirty || budget <= 0)
            {
                return 0;
            }
            return GroupBuilder.Rebuild(this, now);
        }

        private bool AllValid()
        {
            for (int i = 0; i < Members.Count; i++)
            {
                if (!Members[i].InBuild || !PartChecks.Valid(Members[i]))
                {
                    return false;
                }
            }
            return true;
        }

        private void Show()
        {
            Open = false;
            for (int i = 0; i < Members.Count; i++)
            {
                Members[i].SetForced(true);
            }
            Build.SetShown(true);
        }
    }
}
