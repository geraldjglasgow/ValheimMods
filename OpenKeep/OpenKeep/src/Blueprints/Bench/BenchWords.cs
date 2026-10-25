using System.Collections.Generic;
using BlueprintShelf;

namespace OpenKeep.Blueprints.Bench
{
    /// <summary>
    /// The keys of the Blueprint Bench's words (<c>ok_bench_*</c>), English in <c>translations/English.yml</c>. The bench
    /// window's words are the library's (<see cref="ShelfWord"/>), each read from OpenKeep's key (<see cref="Key"/>):
    /// <c>ok_bench_</c> and the word, except those it shares with the Blueprints tab (folders, names, messages), which are
    /// the tab's own <c>ok_bp_*</c>. A word the server answers with travels as its token and its values, read in the
    /// player's language.
    /// </summary>
    public static class BenchWords
    {
        public const string Name = "ok_bench";
        public const string Description = "ok_bench_desc";

        private const string Prefix = "ok_bench_";

        private static readonly Dictionary<string, string> TabWords = new Dictionary<string, string>
        {
            [ShelfWord.Title] = Name,
            [ShelfWord.Home] = BlueprintWords.Tab,
            [ShelfWord.Top] = BlueprintWords.TopFolder,
            [ShelfWord.NewFolder] = BlueprintWords.NewFolderName,
            [ShelfWord.NewFolderTopic] = BlueprintWords.NewFolderTopic,
            [ShelfWord.RenameTopic] = BlueprintWords.RenameTopic,
            [ShelfWord.FolderMade] = BlueprintWords.FolderMade,
            [ShelfWord.Renamed] = BlueprintWords.Renamed,
            [ShelfWord.Moved] = BlueprintWords.Moved,
            [ShelfWord.MoveRefused] = BlueprintWords.MoveRefused,
            [ShelfWord.BadName] = BlueprintWords.BadName,
            [ShelfWord.Exists] = BlueprintWords.Exists,
            [ShelfWord.IntoItself] = BlueprintWords.IntoItself,
            [ShelfWord.Disabled] = BlueprintWords.Disabled,
        };

        /// <summary>A bench word's key in OpenKeep's words.</summary>
        public static string Key(string word) => TabWords.TryGetValue(word, out string key) ? key : Prefix + word;
    }
}
