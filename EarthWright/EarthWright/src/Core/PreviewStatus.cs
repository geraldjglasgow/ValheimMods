using System.Collections.Generic;

namespace EarthWright.Core
{
    /// <summary>
    /// Why the click the player is about to make would fail, reported by any module every few frames (Protection: a
    /// ward in the brush; Costs: not enough stone; Paths: the ramp is too steep). The preview draws its outline red
    /// while any reason is set and shows the first one. A module clears its own key when the reason goes away. The first
    /// reason is found again only when a report changed it, and localized again only when it or the language changed.
    /// </summary>
    public static class PreviewStatus
    {
        private static readonly SortedDictionary<string, string> reasons = new SortedDictionary<string, string>();

        private static string first;
        private static string localized;
        private static int localizedFor = -1;

        public static void Report(string key, string reason)
        {
            if (string.IsNullOrEmpty(reason))
            {
                if (reasons.Remove(key))
                    FindFirst();
                return;
            }
            if (reasons.TryGetValue(key, out string known) && known == reason)
                return;
            reasons[key] = reason;
            FindFirst();
        }

        public static bool Blocked => reasons.Count > 0;

        public static string FirstReason
        {
            get
            {
                if (first == null)
                    return null;
                if (localized == null || localizedFor != Language.Version)
                {
                    localized = TokenText.Localize(first);
                    localizedFor = Language.Version;
                }
                return localized;
            }
        }

        public static void ClearAll()
        {
            reasons.Clear();
            FindFirst();
        }

        private static void FindFirst()
        {
            string found = null;
            foreach (string reason in reasons.Values)
            {
                found = reason;
                break;
            }
            if (found == first)
                return;
            first = found;
            localized = null;
        }
    }
}
