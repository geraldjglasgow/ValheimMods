using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using BepInEx;
using BepInEx.Logging;

namespace Translations
{
    /// <summary>
    /// Where a mod's words come from. Embedded resources <c>&lt;Mod&gt;.translations.&lt;Language&gt;[.&lt;part&gt;].yml</c>
    /// (a csproj line <c>&lt;EmbeddedResource Include="translations\*.yml"
    /// LogicalName="&lt;Mod&gt;.translations.%(Filename)%(Extension)" /&gt;</c>), all parts of a language loaded; then loose
    /// files <c>&lt;Mod&gt;.translations.&lt;Language&gt;.yml</c> under the plugins folder (a translation pack) and in the
    /// config folder (a player's own, which wins). A loose English file replaces the shipped English, for a translation
    /// mod that writes its language into the game's English.
    /// </summary>
    internal sealed class WordSources
    {
        private readonly Assembly assembly;
        private readonly string prefix;

        public WordSources(Assembly assembly, string modName, ManualLogSource log)
        {
            this.assembly = assembly;
            prefix = modName + ".translations.";
            Log = log;
        }

        public ManualLogSource Log { get; }

        public void LoadEnglish(Dictionary<string, string> english)
        {
            foreach (string resource in Resources("English"))
            {
                WordFile.Merge(english, ReadResource(resource), resource, Log);
            }
            Log.LogInfo($"{english.Count} English words loaded");
        }

        /// <summary>The words of a language that replace English: embedded files, then loose files.</summary>
        public Dictionary<string, string> Translation(string language, Dictionary<string, string> english)
        {
            Dictionary<string, string> words = new Dictionary<string, string>(StringComparer.Ordinal);
            List<string> from = new List<string>();
            if (language != "English")
            {
                foreach (string resource in Resources(language))
                {
                    WordFile.Merge(words, ReadResource(resource), resource, Log);
                    from.Add(resource);
                }
            }
            foreach (string path in LooseFiles(language))
            {
                WordFile.Merge(words, File.ReadAllText(path), path, Log);
                from.Add(path);
            }
            Report(language, words, english, from);
            return words;
        }

        private void Report(string language, Dictionary<string, string> words, Dictionary<string, string> english, List<string> from)
        {
            if (from.Count == 0)
            {
                if (language != "English")
                {
                    Log.LogInfo($"{language}: no translation, English shown (a translation is {prefix}{language}.yml)");
                }
                return;
            }
            int used = words.Keys.Count(english.ContainsKey);
            string unused = words.Count > used ? $", {words.Count - used} keys this version does not use" : "";
            Log.LogInfo($"{language}: {used} of {english.Count} words translated{unused} ({string.Join(", ", from)})");
        }

        private IEnumerable<string> Resources(string language) => assembly.GetManifestResourceNames()
            .Where(name => name.StartsWith(prefix, StringComparison.Ordinal) && name.EndsWith(".yml", StringComparison.Ordinal)
                && LanguageOf(name) == language)
            .OrderBy(name => name, StringComparer.Ordinal);

        private string LanguageOf(string resource)
        {
            string rest = resource.Substring(prefix.Length);
            int dot = rest.IndexOf('.');
            return dot < 0 ? rest : rest.Substring(0, dot);
        }

        private string ReadResource(string resource)
        {
            using Stream? stream = assembly.GetManifestResourceStream(resource);
            if (stream == null)
            {
                return "";
            }
            using StreamReader reader = new StreamReader(stream);
            return reader.ReadToEnd();
        }

        // Plugins first (a translation pack), then config (the player's own file wins).
        private IEnumerable<string> LooseFiles(string language)
        {
            string name = prefix + language + ".yml";
            return Find(Paths.PluginPath, name, SearchOption.AllDirectories)
                .Concat(Find(Paths.ConfigPath, name, SearchOption.TopDirectoryOnly));
        }

        private string[] Find(string folder, string name, SearchOption depth)
        {
            try
            {
                return Directory.Exists(folder) ? Directory.GetFiles(folder, name, depth) : Array.Empty<string>();
            }
            catch (Exception e)
            {
                Log.LogWarning($"could not look for {name} in {folder}: {e.Message}");
                return Array.Empty<string>();
            }
        }
    }
}
