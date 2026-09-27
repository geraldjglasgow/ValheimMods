using System.Globalization;
using EarthWright.Core;
using EarthWright.Terrain;

namespace EarthWright.Paths
{
    /// <summary>
    /// The module's English words ("ew_paths_*") and helpers that localize them and fill in numbers. Texts with numbers
    /// use {0}-style placeholders so a translation can move them; numbers are written with a dot as decimal sign.
    /// </summary>
    public static class PathWords
    {
        private const string Prefix = "ew_paths_";

        public static void Register()
        {
            RegisterNames();
            RegisterHud();
            RegisterProblems();
            RegisterMessages();
        }

        /// <summary>The localized word.</summary>
        public static string Text(string key) => Language.Localize("$" + Prefix + key);

        /// <summary>The localized word with its placeholders filled in.</summary>
        public static string Format(string key, params object[] args) => string.Format(CultureInfo.InvariantCulture, Text(key), args);

        public static string ProfileName(RampProfile profile) => Text("profile_" + profile.ToString().ToLowerInvariant());

        public static string PaintName(PaintOp paint) => Text("paint_" + paint.ToString().ToLowerInvariant());

        private static void Add(string key, string english) => Language.Add(Prefix + key, english);

        private static void RegisterNames()
        {
            Add("profile_straight", "straight");
            Add("profile_softjoins", "straight, soft joins");
            Add("profile_softends", "soft ends");
            Add("profile_scurve", "S-curve");
            Add("paint_none", "keep the ground's paint");
            Add("paint_dirt", "dirt");
            Add("paint_cultivated", "cultivated");
            Add("paint_paved", "paved");
            Add("paint_grass", "grass");
            Add("paint_clearvegetation", "no grass");
            Add("paint_vegetation", "grass density");
            Add("paint_deepsnow", "deep snow");
            Add("paint_original", "original ground");
        }

        private static void RegisterHud()
        {
            Add("title_ramp", "Ramp ({0})");
            Add("title_quick", "Quick ramp ({0})");
            Add("title_road", "Road, {0} waypoints");
            Add("hud_shape", "{0:0.0} m long, rise {1:+0.0;-0.0;0.0} m, {2:0.0} m wide");
            Add("hud_stats", "slope {0:0}° steepest, {1:0}° average, {2} points, paint: {3}");
            Add("next_start", "Click: set the ramp's start. {0}: quick ramp from your feet to here");
            Add("next_start_plain", "Click: set the ramp's start");
            Add("hud_set_height", "Points take the set height {0:0.00} m");
            Add("next_end", "Click: set the ramp's end. {0}: remove the start");
            Add("next_build", "Click: build the ramp. Hold {0}: all width to one side, {1}: blend the ends. {2}: profile. {3}: remove the end");
            Add("next_road", "Click: add a waypoint. {0}: carve the road, {1}: carve it paved. {2}: remove the last waypoint");
        }

        private static void RegisterProblems()
        {
            Add("too_short", "Too short: the ends must be at least a metre apart");
            Add("too_long", "Too long: {0:0} m (at most {1:0} m)");
            Add("too_steep", "Too steep: {0:0}° (at most {1:0}°)");
            Add("too_big", "Too big: {0} points (at most {1})");
            Add("unloaded", "Part of it is too far away; move closer");
            Add("past_limit", "Past the height limit at {0} points");
            Add("steep_warning", "Steeper than {0:0}°: players slide down");
            Add("need_two", "Place at least two waypoints first");
            Add("waypoint_close", "Too close to the last waypoint");
            Add("quick_off", "The quick ramp is turned off on this server, or its ramp entry is");
            Add("level_locked", "Your tool's level does not allow this yet");
            Add("nothing", "Nothing to change there");
        }

        private static void RegisterMessages()
        {
            Add("start_set", "Ramp start set");
            Add("end_set", "Ramp end set");
            Add("built", "Ramp built");
            Add("carved", "Road carved");
            Add("waypoint", "Waypoint {0} placed");
            Add("removed", "Last point removed");
            Add("profile", "Ramp profile: {0}");
        }
    }
}
