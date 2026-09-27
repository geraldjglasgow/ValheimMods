using System.Collections.Generic;
using System.Linq;

namespace EarthWright.Core
{
    /// <summary>
    /// Why the click the player is about to make would fail, reported by any module every few frames (Protection: a
    /// ward in the brush; Costs: not enough stone; Paths: the ramp is too steep). The preview draws its outline red
    /// while any reason is set and shows the first one. A module clears its own key when the reason goes away.
    /// </summary>
    public static class PreviewStatus
    {
        private static readonly SortedDictionary<string, string> reasons = new SortedDictionary<string, string>();

        public static void Report(string key, string reason)
        {
            if (string.IsNullOrEmpty(reason))
                reasons.Remove(key);
            else
                reasons[key] = reason;
        }

        public static bool Blocked => reasons.Count > 0;

        public static string FirstReason => reasons.Count > 0 ? Language.Localize(reasons.Values.First()) : null;

        public static void ClearAll() => reasons.Clear();
    }
}
