using System;
using System.Collections.Generic;
using EliteCreaturesPack.Custom.Definitions;
using YamlConfig;

namespace EliteCreaturesPack.Custom.Files
{
    /// <summary>
    /// The model of EliteCreaturesPack.Creatures*.yml: every file's <c>creatures:</c> list merged into one set of
    /// <see cref="CreatureDefinition"/>s, in file order (the main file first). A mistake in one creature - an unknown key,
    /// a value of the wrong kind, a number out of range, a missing name or base - leaves out that creature only: its
    /// problems are logged as warnings naming the file, the creature and the line, and the rest load. Only a file that is
    /// not YAML, or whose <c>creatures:</c> is not a list, is refused as a whole (the previous set stays). A second
    /// creature with a name already used (in any case) is left out the same way.
    /// </summary>
    public sealed class CreatureFile : YamlModel
    {
        private readonly HashSet<string> names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        /// <summary>Every creature read without problems.</summary>
        public List<CreatureDefinition> Creatures { get; } = new List<CreatureDefinition>();

        protected override bool TrackLines => true;

        protected override void Read(YamlNode root)
        {
            foreach (YamlNode entry in root.Get("creatures").Items)
            {
                ReadEntry(entry);
            }
        }

        private void ReadEntry(YamlNode entry)
        {
            CreatureDefinition? definition = null;
            List<string> problems = CollectErrors(() => definition = ReadChecked(entry, CurrentFile ?? ""));
            string label = $"creature '{(definition != null && definition.Name.Length > 0 ? definition.Name : CreatureReader.NameOf(entry))}'";
            if (definition == null || problems.Count > 0)
            {
                problems.ForEach(problem => entry.Warn($"{label} left out: {problem}"));
                return;
            }
            if (!names.Add(definition.Name))
            {
                entry.Warn($"{label} left out: an earlier creature has the same name");
                return;
            }
            Creatures.Add(definition);
        }

        /// <summary>Reads the entry and refuses its unknown keys; a throw (a bug, never a file's fault) is one more problem.</summary>
        private static CreatureDefinition? ReadChecked(YamlNode entry, string file)
        {
            try
            {
                CreatureDefinition definition = CreatureReader.Read(entry, file);
                entry.ErrorUnknownKeys();
                return definition;
            }
            catch (Exception e)
            {
                entry.Error("could not be read: " + e.Message);
                return null;
            }
        }
    }
}
