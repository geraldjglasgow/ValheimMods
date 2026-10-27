using System;
using System.Collections.Generic;
using System.Linq;

namespace Broadside
{
    /// <summary>
    /// The marks a launched ship's ZDO keeps by blueprint index, gone with a part a sea edit removes or builds anew (new
    /// wood, <see cref="KwSeaKind.NewWood"/>): its holes (<see cref="KwHullHoleText.HolesKey"/>) and their patches
    /// (<see cref="KwHullPatchText"/>), its wear (<see cref="KwHullHoleText.WearKey"/>), its broken railing stretches
    /// (<see cref="KwShipRails.Key"/>) and its rig damage (<see cref="KwRigMarks"/>). Every other mark stays as it is: no
    /// index shifts (<see cref="KwSeaApply"/>). Written only by the ship's owner; every peer reads the texts as before.
    /// The text functions are pure.
    /// </summary>
    public static class KwSeaMarks
    {
        /// <summary>Every mark of these indices taken out of the ship's ZDO (each text written only when it changed).</summary>
        public static void Drop(ZDO zdo, ISet<int> indices) => Drop(hash => zdo.GetString(hash), (hash, text) => zdo.Set(hash, text), indices);

        /// <summary>The same on any store of texts by key hash (a ZDO's, or an offline check's).</summary>
        public static void Drop(Func<int, string> get, Action<int, string> set, ISet<int> indices)
        {
            if (indices.Count == 0)
                return;
            DropHoles(get, set, hole => indices.Contains(hole.Part));
            Put(get, set, KwHullHoleText.WearHash, Wear(get(KwHullHoleText.WearHash), indices));
            Put(get, set, KwShipRails.Hash, Rails(get(KwShipRails.Hash), indices));
            Put(get, set, KwRigMarks.Hash, Rig(get(KwRigMarks.Hash), indices));
        }

        /// <summary>The holes a test picks taken out of the ship's ZDO, with their patches.</summary>
        public static void DropHoles(ZDO zdo, Func<KwHullHole, bool> drop) => DropHoles(hash => zdo.GetString(hash), (hash, text) => zdo.Set(hash, text), drop);

        /// <summary>The same on any store of texts by key hash.</summary>
        public static void DropHoles(Func<int, string> get, Action<int, string> set, Func<KwHullHole, bool> drop)
        {
            (string holes, string patches) = Holes(get(KwHullHoleText.HolesHash), get(KwHullPatchText.Hash), drop);
            Put(get, set, KwHullHoleText.HolesHash, holes);
            Put(get, set, KwHullPatchText.Hash, patches);
        }

        /// <summary>A holes text and a patches text without the holes a test picks (and their patches); both as they were when none.</summary>
        public static (string Holes, string Patches) Holes(string holesText, string patchesText, Func<KwHullHole, bool> drop)
        {
            List<KwHullHole> holes = KwHullHoleText.Read(holesText);
            List<KwHullHole> gone = holes.FindAll(h => drop(h));
            if (gone.Count == 0)
                return (holesText, patchesText);
            Dictionary<int, float> patches = KwHullPatchText.Read(patchesText);
            foreach (KwHullHole hole in gone)
                patches.Remove(hole.Id);
            return (KwHullHoleText.Write(holes.FindAll(h => !drop(h))), KwHullPatchText.Write(patches));
        }

        public static string Wear(string text, ISet<int> indices)
        {
            Dictionary<int, float> wear = KwHullHoleText.ReadWear(text);
            return wear.Keys.Any(indices.Contains) ? KwHullHoleText.WriteWear(wear.Where(w => !indices.Contains(w.Key)).ToDictionary(w => w.Key, w => w.Value)) : text;
        }

        public static string Rails(string text, ISet<int> indices)
        {
            HashSet<(int Piece, int Part)> broken = KwShipRails.Parse(text);
            return broken.Any(b => indices.Contains(b.Piece)) ? KwShipRails.Write(new HashSet<(int, int)>(broken.Where(b => !indices.Contains(b.Piece)))) : text;
        }

        public static string Rig(string text, ISet<int> indices)
        {
            List<KwRigMark> marks = KwRigMarks.Read(text);
            return marks.Exists(m => indices.Contains(m.Piece)) ? KwRigMarks.Write(marks.FindAll(m => !indices.Contains(m.Piece))) : text;
        }

        // A text written only when it changed (an unchanged ZDO value sends nothing).
        private static void Put(Func<int, string> get, Action<int, string> set, int hash, string text)
        {
            if (get(hash) != text)
                set(hash, text);
        }
    }
}
