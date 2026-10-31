using System.Collections.Generic;
using UnityEngine;

namespace HighSeatPlus
{
    /// <summary>
    /// The look round a piece right after the game ran its own support check, deciding whether the next checks may be
    /// skipped and what they must watch (<see cref="SupportRecord"/>). Only pieces the game's own cache will not cover
    /// are worth it (<see cref="Worth"/>): the game would run its full search again next time even if nothing changed.
    /// The look is the game's own query (the piece's boxes, m_bounds, made once at its first check; WearNTear.s_rayMask;
    /// the trigger, rigidbody and own-collider tests of UpdateSupport) in the same frame as the game's, so it finds
    /// exactly what the game's search just found. A collider on the terrain layer is ground; one with no WearNTear in its
    /// parents is a non-piece, which wins over the ground as in the game. A piece with neither is held by other pieces and
    /// keeps the game's check. A box holding more than <see cref="MaxCollidersPerBox"/> colliders (of the buffer's 128)
    /// gets no record: a crowd of players or a pile of items walking in could push an anchor out of the game's buffer.
    /// At most <see cref="MaxAnchors"/> anchors of a kind are kept (one still there is enough). The columns of a box are
    /// those of its world-aligned bounds widened by <see cref="BoxMargin"/>.
    /// </summary>
    internal static class SupportScan
    {
        private const int MaxCollidersPerBox = 64;
        private const int MaxAnchors = 16;
        private const float BoxMargin = 0.1f;

        private static readonly Collider[] Hits = new Collider[128];
        private static readonly List<Collider> Ground = new List<Collider>(4);
        private static readonly List<Collider> Things = new List<Collider>(4);
        private static int _terrainLayer = -1;

        /// <summary>True when the game's next check would search again even with nothing changed, and the piece has
        /// full support, which an anchor always gives: a piece below full support has no anchor and is not looked at.</summary>
        public static bool Worth(WearNTear piece)
        {
            if (piece.m_clearCachedSupport || piece.m_colliders == null || piece.m_bounds == null || piece.m_bounds.Count == 0)
            {
                return false;
            }
            return piece.m_support.Equals(piece.GetMaxSupport()) && !GameCacheHolds(piece);
        }

        /// <summary>Looks round the piece with the game's boxes; true when it is held by a non-piece or the ground and no
        /// box is crowded. What was found stays here until <see cref="Fill"/>.</summary>
        public static bool Look(WearNTear piece)
        {
            if (_terrainLayer < 0)
            {
                _terrainLayer = LayerMask.NameToLayer("terrain");
            }
            Ground.Clear();
            Things.Clear();
            foreach (WearNTear.BoundData bound in piece.m_bounds)
            {
                int count = Physics.OverlapBoxNonAlloc(bound.m_pos, bound.m_size, Hits, bound.m_rot, WearNTear.s_rayMask);
                bool crowded = count > MaxCollidersPerBox;
                if (!crowded)
                {
                    Classify(piece, count);
                }
                System.Array.Clear(Hits, 0, count);
                if (crowded)
                {
                    return Forget();
                }
            }
            return Things.Count > 0 || Ground.Count > 0 || Forget();
        }

        /// <summary>Lets go of what the last look found; always false, for the caller's return.</summary>
        public static bool Forget()
        {
            Ground.Clear();
            Things.Clear();
            return false;
        }

        /// <summary>Makes <paramref name="record"/> from the last <see cref="Look"/>; false when it cannot be made now
        /// (the game's cache is not what a non-piece leaves, or a ground-held piece's area is too big).</summary>
        public static bool Fill(WearNTear piece, SupportRecord record)
        {
            record.Clear();
            record.Static = Things.Count > 0;
            record.Entries = piece.m_supportColliders.Count;
            if (record.Static ? record.Entries != 0 : !SupportCells.Collect(piece.m_bounds, record))
            {
                return Forget();
            }
            List<Collider> anchors = record.Static ? Things : Ground;
            for (int i = 0; i < anchors.Count; i++)
            {
                record.AddAnchor(anchors[i]);
            }
            record.Support = piece.m_support;
            record.Material = piece.m_materialType;
            record.Own = piece.transform;
            record.Own.GetPositionAndRotation(out record.Position, out record.Rotation);
            Forget();
            return true;
        }

        // UpdateSupport's own check ends with "every entry the same and the best of them at least the piece's support":
        // right after the game's check every entry is the same, so only the second half decides.
        private static bool GameCacheHolds(WearNTear piece)
        {
            List<float> values = piece.m_supportValue;
            if (values.Count == 0)
            {
                return false;
            }
            float best = 0f;
            for (int i = 0; i < values.Count; i++)
            {
                best = Mathf.Max(best, values[i]);
            }
            return best >= piece.m_support;
        }

        // UpdateSupport's tests, in its order: rigidbodies, triggers and the piece's own colliders are skipped; the
        // terrain layer is ground; a collider with no piece above it is a non-piece.
        private static void Classify(WearNTear piece, int count)
        {
            for (int i = 0; i < count; i++)
            {
                Collider collider = Hits[i];
                if (collider.attachedRigidbody != null || collider.isTrigger || IsOwn(piece, collider))
                {
                    continue;
                }
                if (collider.gameObject.layer == _terrainLayer)
                {
                    Keep(Ground, collider);
                }
                else if (collider.GetComponentInParent<WearNTear>() == null)
                {
                    Keep(Things, collider);
                }
            }
        }

        private static bool IsOwn(WearNTear piece, Collider collider)
        {
            Collider[] own = piece.m_colliders;
            for (int i = 0; i < own.Length; i++)
            {
                if (ReferenceEquals(own[i], collider))
                {
                    return true;
                }
            }
            return false;
        }

        // One entry per collider (a collider can sit in two of the piece's boxes), at most MaxAnchors: a record needs
        // only one of them to stay, so the ones past the limit are never needed.
        private static void Keep(List<Collider> list, Collider collider)
        {
            if (list.Count >= MaxAnchors)
            {
                return;
            }
            for (int i = 0; i < list.Count; i++)
            {
                if (ReferenceEquals(list[i], collider))
                {
                    return;
                }
            }
            list.Add(collider);
        }

        /// <summary>The world-aligned box round one of the piece's turned boxes (m_size is already half the size),
        /// a little wider, for the columns it covers.</summary>
        public static Bounds Box(WearNTear.BoundData bound)
        {
            Vector3 half = bound.m_size;
            Vector3 x = bound.m_rot * new Vector3(half.x, 0f, 0f);
            Vector3 y = bound.m_rot * new Vector3(0f, half.y, 0f);
            Vector3 z = bound.m_rot * new Vector3(0f, 0f, half.z);
            Vector3 extents = new Vector3(
                Mathf.Abs(x.x) + Mathf.Abs(y.x) + Mathf.Abs(z.x),
                Mathf.Abs(x.y) + Mathf.Abs(y.y) + Mathf.Abs(z.y),
                Mathf.Abs(x.z) + Mathf.Abs(y.z) + Mathf.Abs(z.z));
            return new Bounds(bound.m_pos, extents * 2f + new Vector3(BoxMargin, BoxMargin, BoxMargin) * 2f);
        }
    }
}
