using System.Collections.Generic;
using UnityEngine;

namespace HighSeatPlus
{
    /// <summary>
    /// What one piece's last full support search depended on, kept so the next checks can tell whether anything it
    /// read has changed (<see cref="SupportSkip"/>). Made only for a piece held by an anchor: something that is not a
    /// piece (a rock, a tree, a stump, a location wall, a mine rock area), or failing that the ground. Either gives full
    /// support whatever the other pieces around it do, so the record keeps the anchors, not the neighbours:
    /// <list type="bullet">
    /// <item>Held by a non-piece (<see cref="Static"/>): the game empties its own cache whenever it meets one, so the
    /// result stays the same while at least one of the anchors found is still there (alive, enabled, active, not moved,
    /// not rescaled).</item>
    /// <item>Held by the ground: the game keeps the pieces under the centre of mass in its cache, so the record also
    /// needs the columns under its boxes (<see cref="SupportCells"/>), raised when the ground is rebuilt or a piece
    /// appears there, and the game's own cache unchanged, checked entry by entry exactly as UpdateSupport checks it.</item>
    /// </list>
    /// Both also need the piece itself unchanged: not just placed, not moved, the same material (its full support) and
    /// support value. Arrays grow when needed and are reused when the record is made again, so a check allocates nothing.
    /// </summary>
    internal sealed class SupportRecord
    {
        internal struct Anchor
        {
            public Collider Collider;
            public GameObject GameObject;
            public Transform Transform;
            public Vector3 Position;
            public Quaternion Rotation;
            public Vector3 Scale;
        }

        public bool Valid;
        public bool Static;
        public WearNTear.MaterialType Material;
        public float Support;
        public float ExpiresAt;
        public float RetryAt;
        public Transform Own = null!;
        public Vector3 Position;
        public Quaternion Rotation;
        public int Entries;
        public Anchor[] Anchors = new Anchor[2];
        public int AnchorCount;
        public SupportCells.Cell[] Cells = new SupportCells.Cell[4];
        public int CellCount;
        public int StampSum;

        /// <summary>True when nothing UpdateSupport would read for <paramref name="piece"/> has changed since the
        /// record was made; cheapest tests first.</summary>
        public bool Holds(WearNTear piece)
        {
            if (piece.m_clearCachedSupport || !piece.m_support.Equals(Support) || piece.m_materialType != Material)
            {
                return false;
            }
            if (!Static && SupportCells.Sum(Cells, CellCount) != StampSum)
            {
                return false;
            }
            return OwnUnchanged() && AnyAnchorIntact() && GameCacheUnchanged(piece);
        }

        public void AddAnchor(Collider collider)
        {
            if (AnchorCount == Anchors.Length)
            {
                System.Array.Resize(ref Anchors, AnchorCount * 2);
            }
            Transform transform = collider.transform;
            ref Anchor anchor = ref Anchors[AnchorCount++];
            anchor.Collider = collider;
            anchor.GameObject = collider.gameObject;
            anchor.Transform = transform;
            transform.GetPositionAndRotation(out anchor.Position, out anchor.Rotation);
            anchor.Scale = transform.lossyScale;
        }

        public void AddCell(SupportCells.Cell cell)
        {
            for (int i = 0; i < CellCount; i++)
            {
                if (ReferenceEquals(Cells[i], cell))
                {
                    return;
                }
            }
            if (CellCount == Cells.Length)
            {
                System.Array.Resize(ref Cells, CellCount * 2);
            }
            Cells[CellCount++] = cell;
        }

        /// <summary>Lets go of everything the record points at, so a dropped record keeps no destroyed object alive.</summary>
        public void Clear()
        {
            System.Array.Clear(Anchors, 0, AnchorCount);
            System.Array.Clear(Cells, 0, CellCount);
            AnchorCount = 0;
            CellCount = 0;
            Valid = false;
        }

        private bool OwnUnchanged()
        {
            Own.GetPositionAndRotation(out Vector3 position, out Quaternion rotation);
            return position.Equals(Position) && rotation.Equals(Rotation);
        }

        // One anchor still where it was is enough: the game's search meets it and gives full support.
        private bool AnyAnchorIntact()
        {
            for (int i = 0; i < AnchorCount; i++)
            {
                if (Intact(ref Anchors[i]))
                {
                    return true;
                }
            }
            return false;
        }

        private static bool Intact(ref Anchor anchor)
        {
            if (anchor.Collider == null || !anchor.Collider.enabled || !anchor.GameObject.activeInHierarchy)
            {
                return false;
            }
            anchor.Transform.GetPositionAndRotation(out Vector3 position, out Quaternion rotation);
            return position.Equals(anchor.Position) && rotation.Equals(anchor.Rotation) &&
                anchor.Transform.lossyScale.Equals(anchor.Scale);
        }

        // The first loop of UpdateSupport, entry by entry: the same tests, so whatever would make the game drop its
        // cache and search again makes this run the game's check instead of skipping it.
        private bool GameCacheUnchanged(WearNTear piece)
        {
            List<Collider> colliders = piece.m_supportColliders;
            int count = colliders.Count;
            if (count != Entries)
            {
                return false;
            }
            for (int i = 0; i < count; i++)
            {
                Collider collider = colliders[i];
                WearNTear supporter = piece.m_supportWearNTear[i];
                if (collider == null || supporter == null || !supporter.m_supports ||
                    collider.transform.position != piece.m_supportPositions[i] ||
                    !supporter.GetSupport().Equals(piece.m_supportValue[i]))
                {
                    return false;
                }
            }
            return true;
        }
    }
}
