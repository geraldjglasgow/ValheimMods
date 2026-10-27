using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// The planking, deck and railing tools' window: once the second click has planned a part (<see cref="KwSpans"/>), or every part
    /// from one keel end to the other (<see cref="KwKeelRun"/>), a small window (<see cref="KwForm"/>) asks what they are
    /// made of (<see cref="KwBiomeField"/>) and, for planking, its painted bands (<see cref="KwBandField"/>), for a railing
    /// its posts' spacing (<see cref="KwRailField"/>), what they are shows as the choice changes (with what a run left out), and
    /// <see cref="KwPreview"/> shows them see-through in that tier's look (a railing in the plank wood, its tier its cost); OK builds them
    /// all in that tier and tells how many were built; Cancel builds nothing. Local to the clicking player until the parts
    /// are built.
    /// </summary>
    public static class KwSpanForm
    {
        private static List<KwPlan> plans = new List<KwPlan>();
        private static string tool = "", note = "", told = "";
        private static KwBiomeTier tier = KwBiomeTiers.Meadows;

        /// <summary>Whether a tool asks for its material before it builds.</summary>
        public static bool Takes(string which) => which == "planking" || which == "deck" || which == KwBiomeCost.Rail;

        /// <summary>Whether this tool's window is open.</summary>
        public static bool IsOpenFor(string? which) => plans.Count > 0 && KwForm.IsOpen && which == tool;

        /// <summary>Opens the window for the parts planned, with what the player should read beside what they are (null for nothing).</summary>
        public static void Open(Player player, string which, List<KwPlan> planned, string? said)
        {
            KwForm.Close();
            (plans, tool, note, told) = (planned, which, said ?? "", "");
            KwPart part = Part(which);
            tier = KwBiomeField.Last(part);
            List<KwField> fields = new List<KwField> { KwBiomeField.Field(part) };
            fields.AddRange(KwBandField.Fields(which, planned));
            fields.AddRange(KwRailField.Fields(which));
            string title = $"New {KwToolPieces.Label(which).ToLowerInvariant()}" + (planned.Count > 1 ? $", {planned.Count} parts" : "");
            KwForm.Open(title, fields, values => Changed(player, values), values => Done(player, values));
        }

        // The kind of part whose tier the window opens on and keeps: a railing's is the deck's (its cost only, KwRailLooks).
        private static KwPart Part(string which) => which == "planking" ? KwPart.Hull : KwPart.Deck;

        private static void Changed(Player player, KwFormValues values)
        {
            if (plans.Count == 0)
                return;
            tier = KwBiomeField.Picked(values, Part(tool));
            KwRailField.Apply(values, tool, plans);
            if (player != null)
                Tell(player);
            Preview();
        }

        // What the window would build, and what a run left out, when that changed since it was last shown.
        private static void Tell(Player player)
        {
            string parts = plans.Count > 1 ? $", {plans.Count} parts" : "";
            string text = KwTools.Named(tool, tier) + parts + (note.Length > 0 ? "\n" + note : "");
            if (text == told)
                return;
            told = text;
            player.Message(MessageHud.MessageType.Center, text);
        }

        /// <summary>Shows the parts the open window would build; nothing while it is not open.</summary>
        public static void Preview()
        {
            if (plans.Count == 0 || !KwForm.IsOpen)
                KwPreview.Hide();
            else
                KwPreview.Show(tool, plans, tier);
        }

        private static void Done(Player player, KwFormValues? values)
        {
            List<KwPlan> at = plans;
            plans = new List<KwPlan>();
            KwPreview.Hide();
            if (values == null || at.Count == 0 || player == null)
                return;
            tier = KwBiomeField.Picked(values, Part(tool));
            string paint = KwBandField.Picked(values, tool);
            KwRailField.Apply(values, tool, at);
            if (at.Count == 1 && note.Length == 0)
                KwTools.Build(player, tool, at[0].Position, Quaternion.LookRotation(at[0].Direction), KwPaint.Join(at[0].Span.ToString(), paint), tier);
            else
                BuildAll(player, at, paint);
        }

        // Every part built in the tier, planking with its painted bands; then how many were built, with what the run left out.
        private static void BuildAll(Player player, List<KwPlan> at, string paint)
        {
            foreach (KwPlan plan in at)
                KwTools.Spawn(player, tool, plan.Position, Quaternion.LookRotation(plan.Direction), KwPaint.Join(plan.Span.ToString(), paint), tier);
            string text = $"{at.Count} {KwTools.Named(tool, tier)} {(at.Count == 1 ? "part" : "parts")} built";
            player.Message(MessageHud.MessageType.Center, note.Length > 0 ? text + "\n" + note : text);
        }
    }
}
