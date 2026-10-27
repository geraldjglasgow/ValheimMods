using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// One operation of a sea edit on one blueprint piece of a launched ship: <see cref="Index"/> -1 with a
    /// <see cref="Piece"/> adds it (appended to the blueprint); an index with a piece changes that entry; an index without
    /// one removes it (the entry becomes a tombstone, <see cref="KwSeaApply.Gone"/>, so no index ever shifts).
    /// </summary>
    public sealed class KwSeaPart
    {
        public KwSeaPart(int index, KwBpPiece? piece) => (Index, Piece) = (index, piece);

        public int Index { get; }
        public KwBpPiece? Piece { get; }

        public static KwSeaPart Add(KwBpPiece piece) => new KwSeaPart(-1, piece);

        public static KwSeaPart Change(int index, KwBpPiece piece) => new KwSeaPart(index, piece);

        public static KwSeaPart Remove(int index) => new KwSeaPart(index, null);
    }

    /// <summary>
    /// An edit of a launched ship where it floats (the user's request, 2026-10-26: railings, doors and windows, ladders,
    /// masts and sails, post caps and the bowsprit), as the editing player's game asks it of the ship's owner in one
    /// <see cref="ZPackage"/> (<c>KW_SeaEdit</c>, <see cref="KwSeaShip"/>): the kind that made it, the ship's revision it
    /// was planned on (<see cref="KwSeaShip.RevKey"/>; a stale one is refused), the words the asker reads once it is done,
    /// and its parts, whole pieces in the ship's frame with every saved value. Pure.
    /// </summary>
    public sealed class KwSeaEdit
    {
        public KwSeaEdit(string kind, int revision, string done, List<KwSeaPart> parts) => (Kind, Revision, Done, Parts) = (kind, revision, done, parts);

        public string Kind { get; }
        public int Revision { get; }
        public string Done { get; }
        public List<KwSeaPart> Parts { get; }

        public ZPackage Write()
        {
            ZPackage pkg = new ZPackage();
            pkg.Write(1);
            pkg.Write(Kind ?? "");
            pkg.Write(Revision);
            pkg.Write(Done ?? "");
            pkg.Write(Parts.Count);
            foreach (KwSeaPart part in Parts)
            {
                pkg.Write(part.Index);
                pkg.Write(part.Piece != null);
                if (part.Piece != null)
                    KwSeaPiece.Write(pkg, part.Piece);
            }
            return pkg;
        }

        /// <summary>An edit as written; null when it does not read (another version, cut short).</summary>
        public static KwSeaEdit? Read(ZPackage pkg)
        {
            try
            {
                if (pkg.ReadInt() != 1)
                    return null;
                (string kind, int revision, string done, int count) = (pkg.ReadString(), pkg.ReadInt(), pkg.ReadString(), pkg.ReadInt());
                List<KwSeaPart> parts = new List<KwSeaPart>();
                for (int i = 0; i < count && i < 256; i++)
                {
                    int index = pkg.ReadInt();
                    parts.Add(new KwSeaPart(index, pkg.ReadBool() ? KwSeaPiece.Read(pkg) : null));
                }
                return parts.Count == count ? new KwSeaEdit(kind, revision, done, parts) : null;
            }
            catch (System.Exception)
            {
                return null;
            }
        }
    }

    /// <summary>
    /// Blueprint pieces as sea edits make and send them: a tool's part from its text and tier, an old piece with its text
    /// changed (its other saved values kept), and a whole piece written to and read from a package. Pure.
    /// </summary>
    public static class KwSeaPiece
    {
        private static readonly int SpanHash = KwGenerated.Key.GetStableHashCode();

        /// <summary>A generated part's piece: its text, and its tier when it has one.</summary>
        public static KwBpPiece Make(string prefab, Vector3 at, Quaternion turn, string text, KwBiomeTier? tier)
        {
            List<KwBpValue> data = new List<KwBpValue> { new KwBpValue(SpanHash, text) };
            if (tier != null)
                data.Add(new KwBpValue(KwBiomeTiers.Hash, tier.Id));
            return new KwBpPiece(prefab, at, turn, data);
        }

        /// <summary>An old piece with a new text (its tier too when given), its other saved values kept.</summary>
        public static KwBpPiece With(KwBpPiece old, string text, KwBiomeTier? tier) => Placed(old, old.Position, old.Rotation, text, tier);

        /// <summary>The same, placed anew.</summary>
        public static KwBpPiece Placed(KwBpPiece old, Vector3 at, Quaternion turn, string text, KwBiomeTier? tier)
        {
            List<KwBpValue> data = old.Data.FindAll(v => v.Hash != SpanHash && (tier == null || v.Hash != KwBiomeTiers.Hash));
            data.Insert(0, new KwBpValue(SpanHash, text));
            if (tier != null)
                data.Add(new KwBpValue(KwBiomeTiers.Hash, tier.Id));
            return new KwBpPiece(old.Prefab, at, turn, data);
        }

        /// <summary>A piece's saved text (its span, or whatever a generated part saves).</summary>
        public static string Text(KwBpPiece piece) => KwBpData.Text(piece.Data, SpanHash) ?? "";

        /// <summary>Whether a piece saves a tier of its own.</summary>
        public static bool Tiered(KwBpPiece piece) => KwBpData.Text(piece.Data, KwBiomeTiers.Hash) != null;

        /// <summary>Whether two pieces are the same: prefab, place to the millimetre, turn, every saved value (in any order).</summary>
        public static bool Same(KwBpPiece a, KwBpPiece b)
        {
            if (a.Prefab != b.Prefab || (a.Position - b.Position).sqrMagnitude > 1e-6f || Quaternion.Angle(a.Rotation, b.Rotation) > 0.01f || a.Data.Count != b.Data.Count)
                return false;
            foreach (KwBpValue value in a.Data)
            {
                if (!b.Data.Exists(v => v.Hash == value.Hash && Equals(v.Value, value.Value)))
                    return false;
            }
            return true;
        }

        public static void Write(ZPackage pkg, KwBpPiece piece)
        {
            pkg.Write(piece.Prefab);
            pkg.Write(piece.Position);
            pkg.Write(piece.Rotation);
            pkg.Write(piece.Data.Count);
            foreach (KwBpValue value in piece.Data)
            {
                pkg.Write(value.Hash);
                int type = value.Value is string ? 0 : value.Value is int ? 1 : 2;
                pkg.Write(type);
                if (type == 0)
                    pkg.Write((string)value.Value);
                else if (type == 1)
                    pkg.Write((int)value.Value);
                else
                    pkg.Write(value.Value is float f ? f : 0f);
            }
        }

        public static KwBpPiece Read(ZPackage pkg)
        {
            (string prefab, Vector3 at, Quaternion turn, int count) = (pkg.ReadString(), pkg.ReadVector3(), pkg.ReadQuaternion(), pkg.ReadInt());
            List<KwBpValue> data = new List<KwBpValue>();
            for (int i = 0; i < count && i < 64; i++)
            {
                int hash = pkg.ReadInt(), type = pkg.ReadInt();
                object value = type == 0 ? pkg.ReadString() : type == 1 ? pkg.ReadInt() : (object)pkg.ReadSingle();
                data.Add(new KwBpValue(hash, value));
            }
            return new KwBpPiece(prefab, at, turn, data);
        }
    }
}
