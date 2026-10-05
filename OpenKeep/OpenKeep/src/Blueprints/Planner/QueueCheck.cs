using System.Collections.Generic;
using System.Text.RegularExpressions;
using OpenKeep.Blueprints.Sites;

namespace OpenKeep.Blueprints.Planner
{
    /// <summary>
    /// The owner's check of a queue a player sent, before it goes into the site's ZDO: at most
    /// <see cref="QueueEdits.MaxEntries"/> entries; names without rich-text tags, trimmed and at most 40 characters
    /// (an empty one named by its place); piece indices within the blueprint, each piece only in the first entry that
    /// holds it (so the queue never outgrows the blueprint); entries left without pieces are dropped.
    /// </summary>
    public static class QueueCheck
    {
        private const int MaxName = 40;

        public static List<SiteSelection> Clean(List<SiteSelection> sent, int pieceCount)
        {
            List<SiteSelection> clean = new List<SiteSelection>();
            HashSet<int> taken = new HashSet<int>();
            foreach (SiteSelection entry in sent)
            {
                if (clean.Count >= QueueEdits.MaxEntries)
                    break;
                List<int> pieces = Pieces(entry.Pieces, pieceCount, taken);
                if (pieces.Count > 0)
                    clean.Add(new SiteSelection { Name = CleanName(entry.Name, clean.Count + 1), Pieces = pieces });
            }
            return clean;
        }

        private static List<int> Pieces(List<int> sent, int pieceCount, HashSet<int> taken)
        {
            List<int> pieces = new List<int>();
            foreach (int i in sent)
            {
                if (i >= 0 && i < pieceCount && taken.Add(i))
                    pieces.Add(i);
            }
            return pieces;
        }

        private static string CleanName(string name, int place)
        {
            string plain = Regex.Replace(name ?? "", "<[^>]*>", "").Replace("<", "").Replace(">", "").Trim();
            if (plain.Length > MaxName)
                plain = plain.Substring(0, MaxName).TrimEnd();
            return plain.Length > 0 ? plain : "#" + place;
        }
    }
}
