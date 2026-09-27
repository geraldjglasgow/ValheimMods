using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;

namespace EarthWright.Core
{
    /// <summary>
    /// The root console command <c>ew</c> (alias <c>earthwright</c>). Modules add subcommands with <see cref="Add"/> in
    /// their Initialize; <c>ew</c> or <c>ew help</c> lists them. An admin-only subcommand is refused for a player who is
    /// neither an admin nor the host on a server.
    /// </summary>
    public static class Command
    {
        public delegate void Handler(Terminal.ConsoleEventArgs args);

        private sealed class Sub
        {
            public string Name;
            public string Usage;
            public Handler Run;
            public bool AdminOnly;
        }

        private static readonly Dictionary<string, Sub> subs = new Dictionary<string, Sub>(StringComparer.OrdinalIgnoreCase);
        private static bool registered;

        /// <summary>Adds <c>ew &lt;name&gt; ...</c>. The usage line is shown by <c>ew help</c>; args[1] is the subcommand name.</summary>
        public static void Add(string name, string usage, Handler run, bool adminOnly = false)
        {
            subs[name] = new Sub { Name = name, Usage = usage, Run = run, AdminOnly = adminOnly };
        }

        internal static void Register()
        {
            if (registered)
                return;
            registered = true;
            foreach (string root in new[] { "ew", "earthwright" })
            {
                new Terminal.ConsoleCommand(root, "EarthWright terraforming: ew help",
                    (Terminal.ConsoleEvent)(args => Safe.Run("ew command", () => Run(args))),
                    optionsFetcher: () => subs.Keys.OrderBy(k => k).ToList());
            }
        }

        private static void Run(Terminal.ConsoleEventArgs args)
        {
            string name = args.Length > 1 ? args[1] : "help";
            if (!subs.TryGetValue(name, out Sub sub))
            {
                Help(args);
                return;
            }
            if (sub.AdminOnly && !Side.LocalIsAdmin)
            {
                args.Context.AddString("EarthWright: only an admin or the host can use 'ew " + sub.Name + "'.");
                return;
            }
            sub.Run(args);
        }

        private static void Help(Terminal.ConsoleEventArgs args)
        {
            args.Context.AddString("EarthWright commands:");
            foreach (Sub sub in subs.Values.OrderBy(s => s.Name))
                args.Context.AddString("  ew " + sub.Usage + (sub.AdminOnly ? "   (admin)" : ""));
        }
    }

    /// <summary>Registers the command once the game has set up its own; InitTerminal itself runs only once.</summary>
    [HarmonyPatch(typeof(Terminal), nameof(Terminal.InitTerminal))]
    public static class CommandRegisterPatch
    {
        [HarmonyPostfix]
        public static void Postfix() => Command.Register();
    }
}
