using System.Collections.Generic;

namespace OpenKeep.Blueprints.Sites
{
    /// <summary>
    /// The order a construction site is built in: the queue's selections first, in queue order, each selection with the
    /// unbuilt pieces that hold it up, supports first (<see cref="SiteSupport"/>: a queued roof brings its walls and
    /// beams), then every other piece in blueprint order (the blueprint's own order is support order: what holds comes
    /// before what hangs). Only pieces not built yet, each once.
    /// </summary>
    public static class SiteOrder
    {
        /// <summary>The next piece to build, or -1 when every piece stands.</summary>
        public static int Next(SiteState s)
        {
            bool[] built = s.Built;
            foreach (SiteSelection selection in s.Queue)
            {
                List<int> order = Supported(s, selection.Pieces, built);
                if (order.Count > 0)
                    return order[0];
            }
            for (int i = 0; i < built.Length; i++)
            {
                if (!built[i])
                    return i;
            }
            return -1;
        }

        /// <summary>Every piece not built yet, in building order.</summary>
        public static List<int> Remaining(SiteState s)
        {
            bool[] built = s.Built;
            bool[] taken = new bool[built.Length];
            List<int> order = new List<int>();
            foreach (List<int> entry in ByEntry(s))
            {
                foreach (int i in entry)
                    Take(i, built, taken, order);
            }
            for (int i = 0; i < built.Length; i++)
                Take(i, built, taken, order);
            return order;
        }

        /// <summary>
        /// What each queue entry builds, in queue order: its unbuilt pieces with the unbuilt pieces that hold them up and
        /// that no earlier entry brings, supports first.
        /// </summary>
        public static List<List<int>> ByEntry(SiteState s)
        {
            bool[] built = s.Built;
            bool[] taken = new bool[built.Length];
            List<List<int>> entries = new List<List<int>>();
            foreach (SiteSelection selection in s.Queue)
            {
                List<int> order = new List<int>();
                foreach (int i in Supported(s, selection.Pieces, built))
                    Take(i, built, taken, order);
                entries.Add(order);
            }
            return entries;
        }

        /// <summary>The unbuilt pieces of the set with the unbuilt pieces that hold them up, supports first.</summary>
        public static List<int> WithSupports(SiteState s, List<int> pieces) => Supported(s, pieces, s.Built);

        private static void Take(int i, bool[] built, bool[] taken, List<int> order)
        {
            if (i < 0 || i >= built.Length || built[i] || taken[i])
                return;
            taken[i] = true;
            order.Add(i);
        }

        /// <summary>A selection's valid pieces with what holds them up, supports first; plain blueprint order when the support cannot be worked out.</summary>
        private static List<int> Supported(SiteState s, List<int> pieces, bool[] built)
        {
            List<int> valid = pieces.FindAll(i => i >= 0 && i < built.Length);
            Blueprint bp = s.Blueprint;
            List<int> order = bp != null ? BlueprintSafe.Call("OpenKeep site support", () => SiteSupport.WithSupports(bp, valid, built), null) : null;
            if (order != null)
                return order;
            valid.RemoveAll(i => built[i]);
            valid.Sort();
            return valid;
        }
    }
}
