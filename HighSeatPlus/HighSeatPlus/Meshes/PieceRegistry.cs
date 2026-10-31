using System.Collections.Generic;
using UnityEngine;

namespace HighSeatPlus.Meshes
{
    /// <summary>
    /// Every building piece this mod knows, by the instance id of its WearNTear: added from WearNTear.Awake (only
    /// pieces the game itself lists, that is with a ZDO: never the placement ghost), removed from WearNTear.OnDestroy.
    /// A piece waits in a queue until it has been unchanged for <see cref="CombinedMeshes.SettleSeconds"/>; each frame
    /// up to <see cref="PopsPerFrame"/> waiting pieces are looked at and at most a given number inspected, the rest go
    /// to the back. A piece that can not be combined for now (a highlight, snow, a glow) is looked at again after
    /// <see cref="RetrySeconds"/>; a kind that never can is left out for good. <see cref="Touch"/> is what every patch
    /// calls when something about a piece changed or may change.
    /// </summary>
    internal static class PieceRegistry
    {
        public const float RetrySeconds = 20f;
        private const int PopsPerFrame = 64;

        private static readonly Dictionary<int, PieceEntry> Entries = new Dictionary<int, PieceEntry>();
        private static readonly Queue<PieceEntry> Pending = new Queue<PieceEntry>();

        public static bool TryGet(WearNTear piece, out PieceEntry entry)
        {
            return Entries.TryGetValue(piece.GetInstanceID(), out entry);
        }

        public static void Add(WearNTear piece)
        {
            if (piece == null || piece.m_myIndex < 0 || Entries.ContainsKey(piece.GetInstanceID()))
            {
                return;
            }
            PieceEntry entry = new PieceEntry(piece);
            Entries.Add(entry.Id, entry);
            Touch(entry);
        }

        /// <summary>Every piece already in the world, when combining is switched on.</summary>
        public static void AddAll()
        {
            List<WearNTear> pieces = WearNTear.GetAllInstances();
            for (int i = 0; i < pieces.Count; i++)
            {
                Add(pieces[i]);
            }
        }

        public static void Remove(WearNTear piece)
        {
            if (!Entries.TryGetValue(piece.GetInstanceID(), out PieceEntry entry))
            {
                return;
            }
            Entries.Remove(entry.Id);
            entry.Removed = true;
            entry.Group?.Leave(entry);
            entry.SetForced(false);
        }

        public static void Touch(PieceEntry entry)
        {
            entry.TouchedAt = Time.time;
            if (entry.Group != null)
            {
                entry.Group.Touched(entry);
            }
            else if (!entry.LeftOut)
            {
                Enqueue(entry);
            }
        }

        public static void ProcessPending(float now, int inspections)
        {
            for (int pops = 0; pops < PopsPerFrame && inspections > 0 && Pending.Count > 0; pops++)
            {
                PieceEntry entry = Pending.Dequeue();
                entry.Queued = false;
                if (entry.Removed || entry.Group != null || entry.LeftOut)
                {
                    continue;
                }
                if (!entry.Settled(now))
                {
                    Enqueue(entry);
                    continue;
                }
                inspections--;
                Place(entry, PieceInspector.Inspect(entry), now);
            }
        }

        /// <summary>Acts on what a look at the piece decided.</summary>
        public static void Place(PieceEntry entry, Verdict verdict, float now)
        {
            if (verdict == Verdict.Combine)
            {
                CellGroups.Join(entry, now);
            }
            else if (verdict == Verdict.Never)
            {
                entry.LeftOut = true;
            }
            else
            {
                entry.RetryAt = now + RetrySeconds;
                Enqueue(entry);
            }
        }

        public static void Clear()
        {
            Entries.Clear();
            Pending.Clear();
        }

        private static void Enqueue(PieceEntry entry)
        {
            if (entry.Queued || entry.Removed)
            {
                return;
            }
            entry.Queued = true;
            Pending.Enqueue(entry);
        }
    }
}
