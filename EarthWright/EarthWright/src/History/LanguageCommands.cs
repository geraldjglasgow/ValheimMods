using System;
using System.Linq;
using EarthWright.Core;

namespace EarthWright.History
{
    /// <summary>
    /// The console subcommand <c>ew language write | reload</c>: writes the English words as a template for translators
    /// (<c>EarthWright.Language.English.yml</c> next to the cfg) and applies a changed translation file without a restart.
    /// </summary>
    internal static class LanguageCommands
    {
        public static void Register()
        {
            Command.Add("language", "language write | reload   writes the English words as a translation template, or reloads the translation file", Run);
        }

        private static void Run(Terminal.ConsoleEventArgs args)
        {
            string verb = args.Length > 2 ? args[2].ToLowerInvariant() : "";
            if (verb == "write")
                Write(args);
            else if (verb == "reload")
                Reload(args);
            else
                args.Context.AddString($"Usage: ew language write | reload. Your language: {Language.CurrentLanguage ?? "not set up yet"}, {Language.TranslatedCount} of {Language.Translatable.Count()} words translated.");
        }

        private static void Write(Terminal.ConsoleEventArgs args)
        {
            try
            {
                string path = LanguageFiles.WriteEnglishTemplate();
                args.Context.AddString($"EarthWright: {Language.Translatable.Count()} English words written to {path}");
                args.Context.AddString($"Copy it to {LanguageFiles.Prefix}<Language>.yml (your game language: {Language.CurrentLanguage ?? "?"}), translate the texts, then run 'ew language reload'.");
            }
            catch (Exception e)
            {
                args.Context.AddString("EarthWright: the word list could not be written: " + e.Message);
            }
        }

        private static void Reload(Terminal.ConsoleEventArgs args)
        {
            if (!Language.Reload())
            {
                args.Context.AddString("EarthWright: the game has not set up a language yet.");
                return;
            }
            args.Context.AddString($"EarthWright: {Language.CurrentLanguage}: {Language.TranslatedCount} of {Language.Translatable.Count()} words translated (the rest stay English). Some texts update when their window opens again.");
        }
    }
}
