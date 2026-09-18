using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using PatchGuard;

namespace Party.Commands
{
    /// <summary><c>/party ...</c>, always registered, plus short aliases when the word isn't already taken.</summary>
    public static class PartyCommands
    {
        private const string Usage = "party invite <name> | leave | remove <name> | promote <name> | p [text] | panel edit|done | name [text] | status";

        public static void Register()
        {
            new Terminal.ConsoleCommand("party", Usage, args => Guard.Run("party command", () => RunParty(args)),
                isCheat: false, isNetwork: true, optionsFetcher: () => new List<string> { "invite", "leave", "remove", "promote", "p", "panel", "name", "status" });

            RegisterAlias("invite", args => Guard.Run("invite alias", () => CommandHandlers.Invite(Rest(args, 1))), CommandHandlers.OnlinePlayerNames);
            RegisterAlias("leave", args => Guard.Run("leave alias", CommandHandlers.Leave), null);
            RegisterAlias("remove", args => Guard.Run("remove alias", () => CommandHandlers.Remove(Rest(args, 1))), CommandHandlers.PartyMemberNames);
            RegisterAlias("promote", args => Guard.Run("promote alias", () => CommandHandlers.Promote(Rest(args, 1))), CommandHandlers.PartyMemberNames);
            RegisterAlias("p", args => Guard.Run("p alias", () => CommandHandlers.P(TextAfter(args, 0))), null);
        }

        private static void RunParty(Terminal.ConsoleEventArgs args)
        {
            if (args.Args.Length < 2)
            {
                PartyCommandOutput.Print($"Usage: {Usage}");
                return;
            }
            string verb = args.Args[1].ToLowerInvariant();
            RunVerb(verb, args);
        }

        private static void RunVerb(string verb, Terminal.ConsoleEventArgs args)
        {
            switch (verb)
            {
                case "invite": CommandHandlers.Invite(Rest(args, 2)); break;
                case "leave": CommandHandlers.Leave(); break;
                case "remove": CommandHandlers.Remove(Rest(args, 2)); break;
                case "promote": CommandHandlers.Promote(Rest(args, 2)); break;
                case "p": CommandHandlers.P(TextAfter(args, 1)); break;
                case "panel": CommandHandlers.Panel(Rest(args, 2)); break;
                case "name": CommandHandlers.Name(TextAfter(args, 1)); break;
                case "status": CommandHandlers.Status(); break;
                default: PartyCommandOutput.Print($"Usage: {Usage}"); break;
            }
        }

        /// <summary>Registers a short alias only if the word is still free.</summary>
        private static void RegisterAlias(string word, Terminal.ConsoleEvent action, Terminal.ConsoleOptionsFetcher fetcher)
        {
            if (Terminal.commands.ContainsKey(word))
            {
                PartyPlugin.Log.LogInfo($"/{word} is already registered by another mod; use 'party {word}' instead.");
                return;
            }
            new Terminal.ConsoleCommand(word, $"Alias for 'party {word}'. {Usage}", action,
                isCheat: false, isNetwork: true, optionsFetcher: fetcher, alwaysRefreshTabOptions: fetcher != null);
        }

        private static string[] Rest(Terminal.ConsoleEventArgs args, int fromIndex) =>
            args.Args.Length > fromIndex ? args.Args.Skip(fromIndex).ToArray() : new string[0];

        private static string TextAfter(Terminal.ConsoleEventArgs args, int wordIndex) =>
            args.Args.Length > wordIndex + 1 ? string.Join(" ", args.Args.Skip(wordIndex + 1)) : "";
    }

    [HarmonyPatch(typeof(Terminal), "InitTerminal")]
    public static class TerminalInitPatch
    {
        private static bool registered;

        [HarmonyPostfix, HarmonyPriority(Priority.Low)]
        public static void Postfix()
        {
            if (registered)
                return;
            registered = true;
            PartyCommands.Register();
        }
    }
}
