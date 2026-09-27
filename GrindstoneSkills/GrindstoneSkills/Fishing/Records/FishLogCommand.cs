using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using HarmonyLib;
using UnityEngine;

namespace GrindstoneSkills
{
    /// <summary>
    /// The <c>fishlog</c> command, in the console (F5) or in chat as <c>/fishlog</c>: the local character's angler's log,
    /// one line per species landed with its levels and record, then how many species are still to find. Species not yet
    /// landed are counted, not named, so the log keeps its surprises. Not a cheat, so chat allows it. Registered once, in
    /// a Terminal.InitTerminal postfix, as other mods in this workspace register theirs.
    /// </summary>
    public static class FishLogCommand
    {
        private static bool registered;

        [HarmonyPatch(typeof(Terminal), "InitTerminal")]
        private static class Init
        {
            [HarmonyPostfix]
            [HarmonyPriority(Priority.Low)]
            private static void Postfix()
            {
                if (registered)
                    return;
                registered = true;
                new Terminal.ConsoleCommand("fishlog", "GrindstoneSkills: your angler's log - the fish you have landed, their levels and your records.",
                    args => HookGuard.Run("fishlog", () => Print(args)));
            }
        }

        private static void Print(Terminal.ConsoleEventArgs args)
        {
            Player player = Player.m_localPlayer;
            if (player == null)
            {
                args.Context.AddString("No character is loaded.");
                return;
            }
            IReadOnlyList<GameObject> species = FishInfo.Species();
            int known = 0;
            foreach (GameObject prefab in species)
            {
                int levels = AnglerLog.Levels(player, prefab.name);
                if (levels == 0)
                    continue;
                known++;
                args.Context.AddString(Line(player, prefab, levels));
            }
            int left = species.Count - known;
            args.Context.AddString($"Angler's log: {known} of {species.Count} species." + (left > 0 ? $" {left} still to find." : " Every one of them!"));
        }

        private static string Line(Player player, GameObject prefab, int levels)
        {
            string name = FishInfo.Localize(prefab.GetComponent<ItemDrop>().m_itemData.m_shared.m_name);
            string line = name + ": levels " + string.Join(" ", AnglerLog.LevelsIn(levels).Select(level => level.ToString(CultureInfo.InvariantCulture)));
            if (AnglerLog.Best(player, prefab.name, out float weight, out int best))
                line += $", record {weight.ToString("0.0", CultureInfo.InvariantCulture)} kg (level {best})";
            return line;
        }
    }
}
