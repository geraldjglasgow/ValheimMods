using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace DevBridge.Reload
{
    /// <summary>Console commands whose handlers are the old copy's code; the new copy registers its own again.</summary>
    internal static class CommandSweep
    {
        internal static List<string> Remove(Assembly old)
        {
            List<string> names = Terminal.commands.Where(pair => Owned(pair.Value, old)).Select(pair => pair.Key).ToList();
            foreach (string name in names) Terminal.commands.Remove(name);
            return names;
        }

        private static bool Owned(Terminal.ConsoleCommand command, Assembly old) =>
            command != null && (OldCode.Owns(command.action, old) || OldCode.Owns(command.actionFailable, old)
                || OldCode.Owns(command.m_tabOptionsFetcher, old));

        /// <summary>After the new copy started: which removed commands it registered again, and which are gone.</summary>
        internal static void Compare(List<string> removed, Assembly current, Report report)
        {
            List<string> back = removed.Where(n => Terminal.commands.TryGetValue(n, out Terminal.ConsoleCommand c) && Owned(c, current)).ToList();
            report.Count("console commands", removed.Count);
            if (back.Count > 0) report.Commands.Add("registered again: " + string.Join(", ", back));
            List<string> gone = removed.Except(back).ToList();
            if (gone.Count > 0) report.Commands.Add("gone until the new copy registers them (a restart if it does so only at startup): " + string.Join(", ", gone));
        }
    }
}
