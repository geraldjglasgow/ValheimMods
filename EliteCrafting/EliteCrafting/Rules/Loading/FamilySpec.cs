namespace EliteCrafting.Rules
{
    /// <summary>The fixed facts of one YAML family: file pattern, main file, embedded default, sync key, id lists.</summary>
    internal sealed class FamilySpec
    {
        public static readonly FamilySpec Affixes = new FamilySpec("EliteCrafting_affixes", "ecf_affixes", new[] { "affixes" });
        public static readonly FamilySpec Economy = new FamilySpec("EliteCrafting_economy", "ecf_economy", new[] { "rarities", "stones", "salvage.fragments" });

        private FamilySpec(string prefix, string syncKey, string[] idLists)
        {
            Prefix = prefix;
            SyncKey = syncKey;
            IdLists = idLists;
        }

        /// <summary>File names match <c>&lt;Prefix&gt;*.yml</c>.</summary>
        public string Prefix { get; }

        /// <summary>The Charter article name.</summary>
        public string SyncKey { get; }

        /// <summary>Dotted paths of the lists that merge by <c>id</c>, field by field (root keys, or <c>salvage.fragments</c>).</summary>
        public string[] IdLists { get; }

        public string MainFile => Prefix + ".yml";

        public string DefaultResource => "EliteCrafting.config." + MainFile;

        public const string DefaultsOrigin = "(built-in defaults)";
    }

    /// <summary>One YAML text and the name it is reported under (a file name, or the built-in defaults).</summary>
    internal sealed class SourceText
    {
        public SourceText(string name, string text)
        {
            Name = name;
            Text = text;
        }

        public string Name { get; }
        public string Text { get; }
    }
}
