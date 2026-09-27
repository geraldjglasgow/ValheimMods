using System;
using OpenKeep.Core;

namespace OpenKeep.Homestead
{
    /// <summary>
    /// The items Auto Feed Skip names, in OpenKeep's item vocabulary (no groups: the setting lives in the .cfg, not in a
    /// YAML file with a groups: map). The setting is read on every call and parsed again only when its text changes;
    /// an entry that matches nothing is warned about once per change.
    /// </summary>
    public static class FeedSkip
    {
        private static string parsedText;
        private static ItemMatchSet parsed = ItemMatchSet.Empty;

        /// <summary>The station's predicate with the skipped items taken out.</summary>
        public static Func<ItemDrop.ItemData, bool> Without(Func<ItemDrop.ItemData, bool> accepts)
        {
            ItemMatchSet skip = Current();
            return item => accepts(item) && !skip.Matches(item);
        }

        private static ItemMatchSet Current()
        {
            string text = FeedSettings.AutoFeedSkip.Value ?? "";
            if (text == parsedText)
                return parsed;
            parsedText = text;
            parsed = ItemMatchSet.Parse(text.Split(','), ItemGroups.Empty);
            foreach (ItemMatcher matcher in parsed.Matchers)
            {
                if (matcher.Problem != null)
                    Plugin.Log.LogWarning($"OpenKeep: Auto Feed Skip entry '{matcher}' matches nothing: {matcher.Problem}");
            }
            return parsed;
        }
    }
}
