using System.Collections.Generic;

namespace Broadside
{
    /// <summary>
    /// The Doorway and Window tools' note line and their words once OK builds (<see cref="KwOpeningPlan"/>): what the
    /// window would do, what it refuses or skips and why, and where an opening's sill would sit under the waterline the
    /// ship floats at once launched: "Below the waterline: shut it at sea" for one that shuts (a cover, a door), "Below the
    /// waterline: it lets the sea in" for one that does not. Pure.
    /// </summary>
    public static class KwOpeningNotes
    {
        /// <summary>
        /// The note: the tool's own words first, the plan's notes, what OK does (of one opening and several, "window",
        /// "windows"), its refusals and skips, then the sea.
        /// </summary>
        public static string Of(KwOpeningPlan plan, KwOpeningWorld world, string lead, string one = "opening", string many = "openings")
        {
            List<string> lines = new List<string>();
            if (lead.Length > 0)
                lines.Add(lead);
            lines.AddRange(plan.Notes);
            int done = plan.Made + plan.Changed + plan.Filled;
            if (plan.Refused > 0)
                lines.Add(done == 0 ? $"Cannot: {plan.Why}." : $"{plan.Refused} left out: {plan.Why}.");
            else if (plan.Skipped == 0 && done > 1)
                lines.Add($"OK {Does(plan)} {many.ToLowerInvariant()}.");
            if (plan.Skipped > 0)
                lines.Add($"{plan.Made} of {plan.Made + plan.Skipped} fit; skipped: {plan.SkipWhy}.");
            lines.AddRange(Sea(plan, world));
            return string.Join(" ", lines);
        }

        // What OK does, by how many: "cuts 4", "cuts 4 and fills in 3" (a row laid out again).
        private static string Does(KwOpeningPlan plan)
        {
            List<string> does = new List<string>();
            if (plan.Made > 0)
                does.Add($"cuts {plan.Made}");
            if (plan.Changed > 0)
                does.Add($"changes {plan.Changed}");
            if (plan.Filled > 0)
                does.Add($"fills in {plan.Filled}");
            return string.Join(" and ", does);
        }

        // Which of the openings cut or changed would sit under the waterline, those that shut and those that do not.
        private static List<string> Sea(KwOpeningPlan plan, KwOpeningWorld world)
        {
            List<string> lines = new List<string>();
            if (world.Below == null)
                return lines;
            int shut = 0, open = 0;
            foreach (KwPartCut cut in plan.Parts)
            {
                foreach ((KwOpeningSite site, KwOpening o) in cut.Shown)
                {
                    if (!world.Below(site, o))
                        continue;
                    if (o.Covered)
                        shut++;
                    else
                        open++;
                }
            }
            if (shut > 0)
                lines.Add(shut == 1 ? "Below the waterline: shut it at sea." : $"{shut} below the waterline: shut them at sea.");
            if (open > 0)
                lines.Add(open == 1 ? "Below the waterline: it lets the sea in." : $"{open} below the waterline with nothing to shut: they let the sea in.");
            return lines;
        }

        /// <summary>What the player reads once OK has built: "Window cut", "5 windows cut", "Doorway filled in", with what was left out.</summary>
        public static string Done(KwOpeningPlan plan, string one, string many, int kept)
        {
            List<string> said = new List<string>();
            if (plan.Made > 0)
                said.Add(plan.Made == 1 ? $"{one} cut" : $"{plan.Made} {many} cut");
            if (plan.Changed > 0)
                said.Add(plan.Changed == 1 ? $"{one} changed" : $"{plan.Changed} {many} changed");
            if (plan.Filled > 0)
                said.Add(plan.Filled == 1 ? $"{one} filled in" : $"{plan.Filled} {many} filled in");
            string text = said.Count == 0 ? "Nothing changed" : string.Join(", ", said);
            if (plan.Skipped > 0)
                text += $"\n{plan.Skipped} skipped: {plan.SkipWhy}";
            if (plan.Refused > 0)
                text += $"\n{plan.Refused} left out: {plan.Why}";
            if (kept > 0)
                text += $"\n{kept} part{(kept == 1 ? "" : "s")} left out: a ward you have no access to";
            return text;
        }
    }
}
