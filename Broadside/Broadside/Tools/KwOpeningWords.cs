using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// The Doorway and Window tools' placement message, where the game says a piece's placement messages (the middle of the
    /// screen): the current type and where it stands in the list ("Square gunport, lid flaps down (2/8)"), what the ghost
    /// snaps to ("Equally spaced, 2.0 m apart", "Centred between the corner and the doorway"), the sea where its sill would
    /// be under the waterline, and that Shift lays a row; a refused spot's reason instead of the snap. With Shift held, the
    /// row: "Row of 3 single doors, 2.4 m apart: wheel = how many, click = place", the stretch it is centred in, how many of
    /// it fit and why the others do not. On an opening, what it is and that a click changes it (Shift: its row). Said again
    /// twice a second while it stands, so it does not fade, and held back a moment after the tools have said something of
    /// their own (a click's result). Local to this player.
    /// </summary>
    public static class KwOpeningWords
    {
        private const float Again = 0.5f, Held = 2.5f;
        private static readonly Regex Noun = new Regex(@"\b(doorway|door|gunport|porthole|port|window|opening)\b");
        private static string shown = "";
        private static float next, held;

        /// <summary>The tool's openings as its words name one and many: "Window", "windows".</summary>
        public static (string One, string Many) Kind(string tool) => tool == KwDoorwayTool.Tool ? ("Doorway", "doorways") : ("Window", "windows");

        /// <summary>What the message says for the aim, the ghost's placing there (none: no ghost), Shift, and the row shown; kept while they stay.</summary>
        public static string Of(string tool, KwOpeningSight sight, KwOpeningPlacing? placing, bool row, KwOpeningRowPlan? rowed)
        {
            var now = (tool, sight.Opening, sight.Site == null, !sight.Cuttable, placing, row, rowed, KwOpeningCurrent.Of(tool).Type);
            if (!now.Equals(said))
                (said, text) = (now, Words(tool, sight, placing, row, rowed));
            return text;
        }

        private static (string, KwOpening?, bool, bool, KwOpeningPlacing?, bool, KwOpeningRowPlan?, KwOpeningType) said = ("", null, false, false, null, false, null, KwOpeningTypes.Windows[0]);
        private static string text = "";

        private static string Words(string tool, KwOpeningSight sight, KwOpeningPlacing? placing, bool row, KwOpeningRowPlan? rowed)
        {
            if (sight.Opening != null)
                return Opening(tool, sight.Opening);
            if (sight.Site == null)
                return "";
            string label = KwOpeningCurrent.Of(tool).Label;
            if (!sight.Cuttable)
                return label + "\nOnly parts on a dry dock or a launched ship can be cut";
            if (placing == null)
                return label;
            return row ? Row(tool, placing, rowed) : One(tool, label, placing);
        }

        // One opening at the aim: the type, the snap and the sea, or why not; that Shift lays a row.
        private static string One(string tool, string label, KwOpeningPlacing placing)
        {
            const string Hint = "[Shift] a row of them, evenly spaced";
            if (!placing.Fits)
                return label + "\nCannot: " + placing.Plan.Why + "\n" + Hint;
            List<string> lines = new List<string> { label };
            (KwOpeningSite Site, KwOpening Opening)? cut = placing.Cut;
            string sea = cut != null ? Sea(tool, new[] { cut.Value }) : "";
            if (placing.Words.Length > 0 || sea.Length > 0)
                lines.Add(KwOpeningPlace.Join(placing.Words, sea));
            lines.Add(Hint);
            return string.Join("\n", lines);
        }

        // The row: how many, how far apart, the keys; the stretch it is centred in and how many fit; the sea.
        private static string Row(string tool, KwOpeningPlacing placing, KwOpeningRowPlan? rowed)
        {
            if (!placing.Sited)
                return "Cannot: " + placing.Plan.Why;
            if (rowed == null)
                return "No free stretch of wall here for a row";
            string name = Plural(placing.Type.Name, rowed.Count);
            string head = rowed.Count == 1 ? $"Row of 1 {name}" : $"Row of {rowed.Count} {name}, {rowed.Spacing:0.0#} m apart";
            KwOpeningPlan plan = rowed.Plan;
            string fit = plan.Refused > 0 && plan.Made == 0 ? "Cannot: " + plan.Why
                : plan.Made < rowed.Count ? $"{plan.Made} of {rowed.Count} fit ({plan.SkipWhy})" : "";
            string moved = rowed.Moved == 0 ? "" : rowed.Moved == 1 ? "one moved off a rib" : $"{rowed.Moved} moved off the ribs";
            string where = KwOpeningPlace.Join(KwOpeningPlace.Join(KwOpeningSpans.Words(rowed.Span), moved), fit);
            List<string> lines = new List<string> { head + ": wheel = how many, click = place", where };
            string sea = Sea(tool, Cuts(plan));
            if (sea.Length > 0)
                lines.Add(sea);
            return string.Join("\n", lines);
        }

        /// <summary>A type's name for a row of so many, lower case, what follows its noun in brackets: "single doors", "square gunports (lid flaps down)".</summary>
        public static string Plural(string name, int count)
        {
            string lower = name.Length > 0 ? char.ToLowerInvariant(name[0]) + name.Substring(1) : name;
            string named = count == 1 ? lower : Noun.Replace(lower, m => m.Value + "s", 1);
            int comma = named.IndexOf(", ", System.StringComparison.Ordinal);
            return comma < 0 ? named : named.Substring(0, comma) + " (" + named.Substring(comma + 2) + ")";
        }

        // An opening under the crosshair: this tool's changes it; the other tool's is named.
        private static string Opening(string tool, KwOpening o)
        {
            bool doorways = tool == KwDoorwayTool.Tool;
            if (o.IsDoorway != doorways)
                return o.IsDoorway ? "A doorway: click to change it (switches to the Doorway tool)" : "A window: click to change it (switches to the Window tool)";
            return $"{KwOpeningTypes.Name(KwOpeningAsk.Of(o, null))}\nClick to change it (Shift: its row)";
        }

        private static List<(KwOpeningSite Site, KwOpening Opening)> Cuts(KwOpeningPlan plan)
        {
            List<(KwOpeningSite, KwOpening)> cuts = new List<(KwOpeningSite, KwOpening)>();
            foreach (KwPartCut cut in plan.Parts)
                cuts.AddRange(cut.Shown);
            return cuts;
        }

        // Whether openings cut would sit under the waterline once launched: those that shut, and those that let the sea in.
        private static string Sea(string tool, IEnumerable<(KwOpeningSite Site, KwOpening Opening)> cuts)
        {
            KwOpeningWorld world = KwOpeningReads.Context(tool).World;
            (bool shut, bool open) = (false, false);
            foreach ((KwOpeningSite site, KwOpening o) in cuts)
            {
                if (world.Below != null && world.Below(site, o))
                    (shut, open) = (shut || o.Covered, open || !o.Covered);
            }
            return open ? "below the waterline: it lets the sea in" : shut ? "below the waterline: shut it at sea" : "";
        }

        /// <summary>The message said (nothing new for ""), again twice a second while it stands; not while held back.</summary>
        public static void Hover(string text)
        {
            if (text.Length == 0 || Time.time < held || (text == shown && Time.time < next) || MessageHud.instance == null)
            {
                if (text.Length == 0)
                    shown = "";
                return;
            }
            (shown, next) = (text, Time.time + Again);
            MessageHud.instance.ShowMessage(MessageHud.MessageType.Center, text, 0, null, false, false);
        }

        /// <summary>The tools said something of their own: the placement message waits a moment.</summary>
        public static void Hold()
        {
            held = Time.time + Held;
            shown = "";
        }
    }
}
