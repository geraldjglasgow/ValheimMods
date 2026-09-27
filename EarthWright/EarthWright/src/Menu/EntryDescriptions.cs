using System.Collections.Generic;
using EarthWright.Actions;
using EarthWright.Core;

namespace EarthWright.Menu
{
    /// <summary>
    /// The descriptions under the entry names in the build menu: a $word saying what the entry does (a custom entry's
    /// own text), then (unless the player switched it off) a line with the keys that work with it as currently bound.
    /// The game localizes the text every frame and caches by the whole text, so writing a new text is enough to show
    /// changed keys at once. The game's own terrain entries get EarthWright's descriptions while EarthWright is active
    /// and their own back otherwise.
    /// </summary>
    public static class EntryDescriptions
    {
        private static readonly Dictionary<string, string> originals = new Dictionary<string, string>();

        public static void ApplyAll()
        {
            foreach (GameEntry entry in GameEntries.All)
            {
                Piece piece = GamePieces.PieceOf(entry.Id);
                if (piece != null)
                    Set(piece, GameText(entry, piece));
            }
            foreach (EntryDef def in EntryDefs.All)
                Set(EntryRegistry.PieceOf(def.Id), Compose("$" + def.DescriptionKey, def.Action, def.Hints));
            foreach (CustomEntry entry in CustomEntries.All)
                Set(EntryRegistry.PieceOf(entry.PrefabName), Compose(entry.Description ?? "", ActionCatalog.ById(entry.PrefabName)));
        }

        /// <summary>The description with the key line appended, or either alone when the other is empty or hints are off.</summary>
        public static string Compose(string description, ToolAction action, HintKind[] pathKeys = null)
        {
            string line = MenuSettings.ShowKeyHints.Value ? KeyHints.Line(KeyHints.For(action, pathKeys), action) : "";
            if (line.Length == 0)
                return description;
            return description.Length == 0 ? line : description + "\n\n" + line;
        }

        private static string GameText(GameEntry entry, Piece piece)
        {
            if (!originals.ContainsKey(entry.Id))
                originals[entry.Id] = piece.m_description;
            if (!GeneralSettings.Active)
                return originals[entry.Id];
            ToolAction action = ActionCatalog.ById(entry.Id);
            if (entry.EnglishFlatDescription != null && !GameEntryBehaviour.Levels(entry.Id))
                return Compose("$" + entry.FlatDescriptionKey, action);
            return Compose("$" + entry.DescriptionKey, action);
        }

        private static void Set(Piece piece, string text)
        {
            if (piece != null && piece.m_description != text)
                piece.m_description = text;
        }
    }
}
