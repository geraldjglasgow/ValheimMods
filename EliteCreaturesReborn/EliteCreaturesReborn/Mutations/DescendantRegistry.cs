using System.Collections.Generic;

namespace EliteCreaturesReborn.Mutations
{
    /// <summary>
    /// Counts the live members of each Splintering cascade, keyed by the cascade's root. Only consulted when the
    /// (off-by-default) live-descendant cap is on, to decide whether a further split is truncated. Counts are kept
    /// on the owner where splits happen; a miscount only ever loosens or tightens the cap by a few, never crashes.
    /// </summary>
    public static class DescendantRegistry
    {
        private static readonly Dictionary<string, int> Counts = new Dictionary<string, int>();

        public static void Register(string root)
        {
            if (string.IsNullOrEmpty(root))
            {
                return;
            }
            Counts.TryGetValue(root, out int count);
            Counts[root] = count + 1;
        }

        public static void Unregister(string root)
        {
            if (string.IsNullOrEmpty(root) || !Counts.TryGetValue(root, out int count))
            {
                return;
            }
            if (count <= 1)
            {
                Counts.Remove(root);
            }
            else
            {
                Counts[root] = count - 1;
            }
        }

        public static int Count(string root)
        {
            if (string.IsNullOrEmpty(root))
            {
                return 0;
            }
            Counts.TryGetValue(root, out int count);
            return count;
        }
    }
}
