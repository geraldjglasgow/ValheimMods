using OpenKeep.Core;

namespace OpenKeep.Tracker
{
    /// <summary>The "$ok_tracker..." words of the module, registered at Initialize. <see cref="Format"/> fills {0}, {1} placeholders.</summary>
    public static class TrackerWords
    {
        public const string Track = "$ok_tracker_track";
        public const string Untrack = "$ok_tracker_untrack";
        public const string Title = "$ok_tracker_title";
        public const string Tracking = "$ok_tracker_tracking";
        public const string Untracked = "$ok_tracker_untracked";
        public const string Full = "$ok_tracker_full";
        public const string Made = "$ok_tracker_made";
        public const string Upgrade = "$ok_tracker_upgrade";
        public const string Station = "$ok_tracker_station";
        public const string AnyOne = "$ok_tracker_anyone";

        public static void Register()
        {
            Language.Add("ok_tracker_track", "Track");
            Language.Add("ok_tracker_untrack", "Untrack");
            Language.Add("ok_tracker_title", "Tracked recipes");
            Language.Add("ok_tracker_tracking", "Tracking {0}");
            Language.Add("ok_tracker_untracked", "No longer tracking {0}");
            Language.Add("ok_tracker_full", "The tracker holds {0} recipes at most");
            Language.Add("ok_tracker_made", "{0} made, off the tracker");
            Language.Add("ok_tracker_upgrade", "{0}, level {1}");
            Language.Add("ok_tracker_station", "{0}, level {1}");
            Language.Add("ok_tracker_anyone", "Any one of these");
        }

        public static string Format(string word, params object[] args)
        {
            string text = Language.Localize(word);
            try
            {
                return string.Format(text, args);
            }
            catch (System.FormatException)
            {
                return text;
            }
        }
    }
}
