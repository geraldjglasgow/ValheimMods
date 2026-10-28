using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// What a launched ship's rig damage marks (<see cref="KwRigMark"/>) come to, worked out on every peer each time they
    /// change (<see cref="KwRigDamage"/>): each spar's notches, each sail's holes, each cap's chips, each rudder's chunks,
    /// the pieces fallen; where a yard, boom or gaff is broken through (<see cref="BrokenAt"/>); each sail's fastenings
    /// and how much of it droops (<see cref="Sail"/>, <see cref="KwSailHurt"/>); and how much of each sail still drives
    /// (<see cref="Working"/>). Plain data from the marks and the blueprint.
    /// </summary>
    public sealed class KwRigCondition
    {
        private readonly Dictionary<long, List<KwRigMark>> notches = new Dictionary<long, List<KwRigMark>>();
        private readonly Dictionary<long, List<KwRigMark>> holes = new Dictionary<long, List<KwRigMark>>();
        private readonly Dictionary<int, List<KwRigMark>> chips = new Dictionary<int, List<KwRigMark>>();
        private readonly Dictionary<int, List<KwRigMark>> chunks = new Dictionary<int, List<KwRigMark>>();
        private readonly Dictionary<int, KwRigMark> falls = new Dictionary<int, KwRigMark>();
        private readonly Dictionary<long, KwSailHurt> sails = new Dictionary<long, KwSailHurt>();
        private readonly List<KwRigMark> all = new List<KwRigMark>();
        private static readonly List<KwRigMark> None = new List<KwRigMark>();

        private KwRigCondition(KwRigParts parts) => Parts = parts;

        public KwRigParts Parts { get; }

        /// <summary>Whether there is no damage at all.</summary>
        public bool Whole { get; private set; } = true;

        public static KwRigCondition Of(KwRigParts parts, List<KwRigMark> marks)
        {
            KwRigCondition made = new KwRigCondition(parts);
            foreach (KwRigMark mark in marks)
                made.Add(mark);
            return made;
        }

        /// <summary>A piece's marks of some types as one text, to know when what they draw changed.</summary>
        public string Key(int piece, string types)
        {
            System.Text.StringBuilder key = new System.Text.StringBuilder();
            foreach (KwRigMark mark in all)
            {
                if (mark.Piece == piece && types.IndexOf(mark.Type) >= 0)
                    key.Append(mark).Append(';');
            }
            return key.ToString();
        }

        private void Add(KwRigMark mark)
        {
            all.Add(mark);
            Whole = false;
            switch (mark.Type)
            {
                case KwRigMark.Notch: Listed(notches, Key(mark.Piece, mark.Part)).Add(mark); break;
                case KwRigMark.Hole: Listed(holes, Key(mark.Piece, mark.Part)).Add(mark); break;
                case KwRigMark.Chip: Listed(chips, mark.Piece).Add(mark); break;
                case KwRigMark.Chunk: Listed(chunks, mark.Piece).Add(mark); break;
                default: falls[mark.Piece] = mark; break;
            }
        }

        private static List<KwRigMark> Listed<T>(Dictionary<T, List<KwRigMark>> lists, T key)
        {
            if (!lists.TryGetValue(key, out List<KwRigMark> list))
                lists[key] = list = new List<KwRigMark>();
            return list;
        }

        public static long Key(int piece, int part) => ((long)piece << 20) ^ (part + 1000);

        public List<KwRigMark> Notches(int piece, int part) => notches.TryGetValue(Key(piece, part), out List<KwRigMark> list) ? list : None;

        public List<KwRigMark> Holes(int piece, int sail) => holes.TryGetValue(Key(piece, sail), out List<KwRigMark> list) ? list : None;

        public List<KwRigMark> Chips(int piece) => chips.TryGetValue(piece, out List<KwRigMark> list) ? list : None;

        public List<KwRigMark> Chunks(int piece) => chunks.TryGetValue(piece, out List<KwRigMark> list) ? list : None;

        public bool Fallen(int piece) => falls.ContainsKey(piece);

        /// <summary>
        /// Whether a sail went with its mast's fall: it hung from above where the mast broke (the user, 2026-10-27: the sails
        /// below a break stay, set and drawing); any sail of a fallen bowsprit or cap.
        /// </summary>
        public bool SailDown(int piece, int sail)
        {
            if (!falls.TryGetValue(piece, out KwRigMark fall))
                return false;
            KwRigPiece? mast = Parts.Get(piece);
            return mast == null || !mast.IsMast || sail < 0 || sail >= mast.Sails.Count || Above(mast, sail, fall);
        }

        /// <summary>Whether a mast's sail hangs from above a fall's break: its head higher up the mast than where it broke.</summary>
        public static bool Above(KwRigPiece mast, int sail, KwRigMark fall) => mast.Main.Along(new Vector3(0f, 0f, mast.Sails[sail].Spec.Top)) > fall.A;

        /// <summary>A fallen piece's fall (where it broke, which way it went); null when it stands.</summary>
        public KwRigMark? FallOf(int piece) => falls.TryGetValue(piece, out KwRigMark fall) ? fall : (KwRigMark?)null;

        /// <summary>Whether a piece has any mark at all.</summary>
        public bool Marked(int piece)
        {
            foreach (KeyValuePair<long, List<KwRigMark>> list in notches)
            {
                if (list.Value.Count > 0 && list.Value[0].Piece == piece)
                    return true;
            }
            foreach (KeyValuePair<long, List<KwRigMark>> list in holes)
            {
                if (list.Value.Count > 0 && list.Value[0].Piece == piece)
                    return true;
            }
            return chips.ContainsKey(piece) || chunks.ContainsKey(piece) || falls.ContainsKey(piece);
        }

        /// <summary>Where along a spar it is broken through (more than half its section gone at one place), or null.</summary>
        public float? BrokenAt(int piece, int part)
        {
            List<KwRigMark> cuts = Notches(piece, part);
            if (cuts.Count == 0)
                return null;
            (float at, float lost) = KwRigSection.Worst(cuts);
            return lost > KwRigSection.Breaks ? at : (float?)null;
        }

        /// <summary>A sail's fastenings and droop (<see cref="KwSailHurt"/>), worked out the first time it is asked.</summary>
        public KwSailHurt Sail(int piece, int sail)
        {
            long key = Key(piece, sail);
            if (sails.TryGetValue(key, out KwSailHurt hurt))
                return hurt;
            KwRigPiece? mast = Parts.Get(piece);
            hurt = mast == null || sail < 0 || sail >= mast.Sails.Count ? KwSailHurt.Whole : KwSailHurtRead.Of(this, mast, sail);
            return sails[key] = hurt;
        }

        /// <summary>The share of a sail's drive left: none once it went with its mast's fall, less its holes' area and the share drooping.</summary>
        public float Working(int piece, int sail)
        {
            if (Whole)
                return 1f;
            if (SailDown(piece, sail))
                return 0f;
            KwSailHurt hurt = Sail(piece, sail);
            return Mathf.Clamp01(1f - hurt.Droop - hurt.Holed);
        }

        /// <summary>The share of a bowsprit's jibs' drive left: none once it or the foremast its stay comes from is down.</summary>
        public float Jibs(KwRigPiece sprit) => Fallen(sprit.Index) || (sprit.Foremast >= 0 && Fallen(sprit.Foremast)) ? 0f : 1f;
    }
}
