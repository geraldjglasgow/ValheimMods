using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// A sea edit applied to a launched ship's blueprint, in place, so every component holding the blueprint sees the ship
    /// as it is now: a change replaces its entry, an addition is appended, a removal leaves a tombstone
    /// (<see cref="Gone"/>: the removed piece's place, no data, a prefab no reader knows) so no index ever shifts and every
    /// mark keyed by an index (holes, wear, broken railings, rig damage, the shot router's tags) keeps pointing at its own
    /// part. What is checked first (<see cref="Why"/>): every index in range and not a tombstone, every piece before and
    /// after one a sea kind owns (<see cref="KwSeaKinds"/>). Pure.
    /// </summary>
    public static class KwSeaApply
    {
        /// <summary>A removed piece's tombstone prefab.</summary>
        public const string Gone = "KW_sea_gone";

        /// <summary>The most entries one edit may touch.</summary>
        public const int Most = 64;

        public static bool IsGone(KwBpPiece? piece) => piece == null || piece.Prefab == Gone;

        /// <summary>A removed piece's tombstone: where it stood, nothing else.</summary>
        public static KwBpPiece Tombstone(KwBpPiece removed) => new KwBpPiece(Gone, removed.Position, removed.Rotation, new List<KwBpValue>());

        /// <summary>Why an edit cannot be applied to a blueprint; null when it can.</summary>
        public static string? Why(KwBlueprint blueprint, KwSeaEdit edit, System.Func<KwBpPiece, bool> owned)
        {
            if (edit.Parts.Count == 0 || edit.Parts.Count > Most)
                return "Nothing to change";
            HashSet<int> seen = new HashSet<int>();
            foreach (KwSeaPart part in edit.Parts)
            {
                if (part.Index < 0)
                {
                    if (part.Piece == null || !owned(part.Piece))
                        return "That cannot be built at sea";
                    continue;
                }
                if (part.Index >= blueprint.Pieces.Count || IsGone(blueprint.Pieces[part.Index]) || !seen.Add(part.Index))
                    return "That part is no longer there";
                if (!owned(blueprint.Pieces[part.Index]) || (part.Piece != null && !owned(part.Piece)))
                    return "Only railings, doors and windows, ladders and the rig change at sea";
            }
            return null;
        }

        /// <summary>The edit made in the blueprint, in place; what it changed, in index order.</summary>
        public static KwSeaChanges Apply(KwBlueprint blueprint, KwSeaEdit edit)
        {
            List<KwSeaChange> changes = new List<KwSeaChange>();
            foreach (KwSeaPart part in edit.Parts)
            {
                if (part.Index < 0)
                {
                    blueprint.Pieces.Add(part.Piece!);
                    changes.Add(new KwSeaChange(blueprint.Pieces.Count - 1, null, part.Piece));
                    continue;
                }
                KwBpPiece old = blueprint.Pieces[part.Index];
                blueprint.Pieces[part.Index] = part.Piece ?? Tombstone(old);
                changes.Add(new KwSeaChange(part.Index, old, part.Piece));
            }
            changes.Sort((a, b) => a.Index.CompareTo(b.Index));
            return new KwSeaChanges(changes);
        }

        /// <summary>A copy of a blueprint's list (the same pieces), to try an edit on without touching the ship's.</summary>
        public static KwBlueprint Copy(KwBlueprint blueprint)
        {
            KwBlueprint copy = new KwBlueprint(blueprint.Name);
            copy.Pieces.AddRange(blueprint.Pieces);
            return copy;
        }

        /// <summary>The blueprint as the edit would leave it (a copy; the ship's untouched).</summary>
        public static KwBlueprint Tried(KwBlueprint blueprint, KwSeaEdit edit)
        {
            KwBlueprint copy = Copy(blueprint);
            Apply(copy, edit);
            return copy;
        }

        /// <summary>A unit rotation's inverse, in plain arithmetic (Unity's own is native code; offline checks run this).</summary>
        public static Quaternion Back(Quaternion q) => new Quaternion(-q.x, -q.y, -q.z, q.w);

        /// <summary>A rotation's heading alone (turned about the vertical only), in plain arithmetic.</summary>
        public static Quaternion Yaw(Quaternion q)
        {
            Vector3 forward = q * Vector3.forward;
            float half = Mathf.Atan2(forward.x, forward.z) / 2f;
            return new Quaternion(0f, Mathf.Sin(half), 0f, Mathf.Cos(half));
        }

        /// <summary>A point of a piece's kit in the ship's frame (as the parts are placed).</summary>
        public static Vector3 OnShip(KwBpPiece piece, Vector3 kit) => piece.Position + piece.Rotation * KwMesh.ToUnity(kit);
    }
}
