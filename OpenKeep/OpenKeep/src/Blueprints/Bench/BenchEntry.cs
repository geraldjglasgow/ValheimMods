using System;
using System.Collections.Generic;

namespace OpenKeep.Blueprints.Bench
{
    public enum BenchKind
    {
        Up,
        Folder,
        Blueprint,
        Owner,
    }

    /// <summary>One line of a bench list: the way up, a folder, a blueprint, or (the pool's top list) a player's part.</summary>
    public sealed class BenchEntry
    {
        public BenchKind Kind;

        /// <summary>The folder's or blueprint's path from the top of the library or of the player's part; the way up: the folder it opens.</summary>
        public string Path = "";

        /// <summary>The player whose part of the pool it is (0 in the library, and for the way up to the list of players).</summary>
        public long Owner;

        /// <summary>What the line shows (it may carry rich text).</summary>
        public string Label = "";

        /// <summary>The plain name, for messages.</summary>
        public string Name = "";

        /// <summary>Names the line across refills (the selection).</summary>
        public string Key => (int)Kind + "|" + Owner + "|" + Path;

        /// <summary>It opens and takes drops: everything but a blueprint.</summary>
        public bool Holds => Kind != BenchKind.Blueprint;

        public static BenchEntry Up(long owner, string path) =>
            new BenchEntry { Kind = BenchKind.Up, Owner = owner, Path = path, Label = "..", Name = ".." };

        public static BenchEntry Of(BenchKind kind, long owner, string path)
        {
            string name = BlueprintEntries.Title(BlueprintLibrary.Leaf(path));
            return new BenchEntry { Kind = kind, Owner = owner, Path = path, Label = name, Name = name };
        }
    }

    /// <summary>A button under a list: its word, when it can be pressed, and what it does.</summary>
    public sealed class BenchAction
    {
        public BenchAction(string word, Func<bool> enabled, Action run)
        {
            Word = word;
            Enabled = enabled;
            Run = run;
        }

        public string Word { get; }
        public Func<bool> Enabled { get; }
        public Action Run { get; }
    }

    /// <summary>
    /// One side of the bench window (the player's library, or the shared pool): where it is, what it lists, its buttons,
    /// and what opening, renaming, removing and dropping do there. The view (<see cref="BenchPaneView"/>) draws it.
    /// </summary>
    public interface IBenchPane
    {
        string Header { get; }
        string Location { get; }
        List<BenchEntry> Entries { get; }
        BenchSelection Selection { get; }

        /// <summary>Goes up whenever <see cref="Entries"/> changed.</summary>
        int Version { get; }

        List<BenchAction> Actions { get; }

        /// <summary>Per frame while the window is open: reads its source again and refills when it changed.</summary>
        void Tick();

        /// <summary>A double click on a folder, a player or the way up.</summary>
        void Open(BenchEntry entry);

        /// <summary>F2 or a right click on a line.</summary>
        void Rename(BenchEntry entry);

        /// <summary>The Delete key: the selection.</summary>
        void Remove();

        /// <summary>Lines from <paramref name="from"/> may be dropped on <paramref name="onto"/> (null: the folder shown).</summary>
        bool Accepts(IBenchPane from, BenchEntry onto);

        void Drop(IBenchPane from, List<BenchEntry> dragged, BenchEntry onto);
    }
}
