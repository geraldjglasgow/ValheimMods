using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EliteCreaturesPack.Custom.Export
{
    /// <summary>
    /// The text of an exported definition, written line by line in the shape the custom creature files are read in
    /// (Custom/Files): blocks of keys indented two spaces, lists of blocks with a dash, notes as YAML comments. A stretch
    /// can be written commented out (<see cref="BeginComment"/>): the same lines behind a <c>#</c>, so removing the
    /// <c>#</c> leaves them correctly indented. The keyed writes that check a range write a note in place of a value the
    /// files would refuse, so whatever is written as a value reads back cleanly; and a block that ends up holding notes
    /// only has its own key commented out too, since a key with nothing under it reads as an empty value, not a block.
    /// </summary>
    internal sealed class ExportWriter
    {
        private const int Indent = 2, Width = 116;

        private readonly StringBuilder text = new StringBuilder();
        private int depth;
        private int commented;
        private int commentDepth;
        private bool dash;

        private readonly Stack<Block> blocks = new Stack<Block>();

        /// <summary>A plain value: <c>key: value</c>, the value already formatted (<see cref="ExportValues"/>).</summary>
        public void Key(string key, string value)
        {
            Line(key + ": " + value);
            Filled();
        }

        /// <summary><c>key:</c> and the lines after it one level in, until <see cref="Close"/>.</summary>
        public void Open(string key)
        {
            blocks.Push(new Block(Line(key + ":"), commented > 0));
            depth++;
        }

        /// <summary>Ends the block; one that holds no value has its key commented out.</summary>
        public void Close()
        {
            depth--;
            Block block = blocks.Pop();
            if (block.Filled)
            {
                Filled();
                return;
            }
            text.Insert(block.Start, "# ");
        }

        /// <summary>A block in a list: its first line gets the dash, the rest line up under it, until <see cref="EndItem"/>.</summary>
        public void Item()
        {
            depth++;
            dash = true;
        }

        public void EndItem()
        {
            depth--;
            dash = false;
        }

        /// <summary>One value of a list written as lines: <c>- value</c>.</summary>
        public void Entry(string value)
        {
            Line("- " + value);
            Filled();
        }

        /// <summary>A comment, wrapped to the width of the files' own comments.</summary>
        public void Note(string note)
        {
            foreach (string part in Wrap(note))
            {
                Write(part.Length == 0 ? "#" : "# " + part, false);
            }
        }

        /// <summary>A list of name lists, each written <c>- [a, b]</c>; empty lists left out, <c>[]</c> when none is left.</summary>
        public void NameLists(string key, IEnumerable<IEnumerable<string>> lists)
        {
            List<string> written = lists.Select(list => list.ToList()).Where(list => list.Count > 0).Select(ExportValues.Names).ToList();
            if (written.Count == 0)
            {
                Key(key, "[]");
                return;
            }
            Open(key);
            written.ForEach(Entry);
            Close();
        }

        /// <summary>The lines from here to <see cref="EndComment"/> are written commented out.</summary>
        public void BeginComment()
        {
            if (commented++ == 0)
            {
                commentDepth = depth;
            }
        }

        public void EndComment() => commented--;

        /// <summary>A number within the range the files take, or a note saying why it is left as it is.</summary>
        public void Number(string key, float value, float min, float max)
        {
            if (ExportValues.Within(value, min, max))
            {
                Key(key, ExportValues.Number(value));
                return;
            }
            Note($"{key}: {ExportValues.Number(value)} - outside the {ExportValues.Number(min)} to {ExportValues.Number(max)} "
                + "a definition takes, so it is left as it is");
        }

        /// <summary>Text in quotes, or a note when there is none: the files refuse an empty value.</summary>
        public void Text(string key, string? value, string none)
        {
            if (string.IsNullOrEmpty(value))
            {
                Note($"{key}: {none}");
                return;
            }
            Key(key, ExportValues.Text(value!));
        }

        /// <summary>A switch: <c>true</c> or <c>false</c>.</summary>
        public void Switch(string key, bool value) => Key(key, value ? "true" : "false");

        public override string ToString() => text.ToString();

        /// <summary>Writes a line of the definition; returns where its text starts.</summary>
        private int Line(string body)
        {
            int start = Write(body, dash);
            dash = false;
            return start;
        }

        // A value fills the block it is written in only on the same side of the comment: a block written as values with
        // only commented lines under it would read as empty, and a commented one is complete once uncommented.
        private void Filled()
        {
            if (blocks.Count > 0 && blocks.Peek().Commented == commented > 0)
            {
                blocks.Peek().Filled = true;
            }
        }

        // Inside a commented stretch the # goes at the stretch's own depth and the indentation follows it, so a line
        // uncommented by deleting "# " sits exactly where it belongs.
        private int Write(string body, bool withDash)
        {
            int level = withDash ? depth - 1 : depth;
            string lead = withDash ? "- " : "";
            if (commented > 0)
            {
                text.Append(' ', commentDepth * Indent).Append("# ").Append(' ', System.Math.Max(0, level - commentDepth) * Indent);
            }
            else
            {
                text.Append(' ', level * Indent);
            }
            text.Append(lead);
            int start = text.Length;
            text.Append(body).Append('\n');
            return start;
        }

        /// <summary>An open block: where its key starts in the text, whether it is commented, whether a value is under it.</summary>
        private sealed class Block
        {
            public Block(int start, bool commented)
            {
                Start = start;
                Commented = commented;
            }

            public int Start { get; }

            public bool Commented { get; }

            public bool Filled { get; set; }
        }

        private IEnumerable<string> Wrap(string note)
        {
            int room = Width - depth * Indent;
            while (note.Length > room)
            {
                int cut = note.LastIndexOf(' ', room);
                cut = cut > 0 ? cut : room;
                yield return note.Substring(0, cut);
                note = note.Substring(cut).TrimStart();
            }
            yield return note;
        }
    }
}
