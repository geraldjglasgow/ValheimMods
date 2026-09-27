using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using EarthWright.Terrain;
using YamlConfig;

namespace EarthWright.Menu
{
    /// <summary>
    /// The model of EarthWright.Entries*.yml: <c>entries:</c>, a list of custom menu entries (see <see cref="CustomEntry"/>
    /// and the shipped file's comments). An entry needs an id and a command; an id may appear only once across the files.
    /// A bad entry is an error and the previous entries stay.
    /// </summary>
    public sealed class CustomEntriesModel : YamlModel
    {
        private static readonly Regex IdPattern = new Regex("^[A-Za-z0-9_-]+$");

        public List<CustomEntry> Entries { get; } = new List<CustomEntry>();

        protected override void Read(YamlNode root)
        {
            YamlNode list = root.Get("entries");
            if (list.Kind == YamlNodeKind.Missing || list.Kind == YamlNodeKind.Null)
                return;
            foreach (YamlNode node in list.Items)
            {
                CustomEntry entry = ReadEntry(node);
                if (entry != null)
                    Entries.Add(entry);
            }
        }

        protected override void Verify()
        {
            foreach (IGrouping<string, CustomEntry> twice in Entries.GroupBy(e => e.Id.ToLowerInvariant()).Where(g => g.Count() > 1))
                Errors.Add($"entries: the id '{twice.First().Id}' is used {twice.Count()} times; every entry needs its own id");
        }

        /// <summary>Every key is read (so none is reported as unknown) before a bad entry is dropped.</summary>
        private static CustomEntry ReadEntry(YamlNode node)
        {
            CustomEntry entry = new CustomEntry();
            bool valid = ReadId(node, entry) & ReadCommand(node, entry);
            entry.Name = Text(node, "name") ?? entry.Id;
            entry.Description = Text(node, "description") ?? "";
            entry.Icon = Text(node, "icon");
            if (Present(node.Get("tool")) && node.Get("tool").TryEnum(out CustomTool tool))
                entry.Tool = tool;
            if (Present(node.Get("shape")) && node.Get("shape").TryEnum(out BrushShape shape))
                entry.Shape = shape;
            entry.Repeat = Flag(node, "repeat");
            entry.Admin = Flag(node, "admin");
            valid &= ReadNumbers(node, entry);
            return valid ? entry : null;
        }

        private static bool ReadId(YamlNode node, CustomEntry entry)
        {
            YamlNode id = node.Get("id");
            if (!Present(id))
            {
                node.Error("an entry needs an id, for example id: level_big");
                return false;
            }
            if (!id.TryString(out string text) || !IdPattern.IsMatch(text.Trim()))
            {
                id.Error("the id may only use letters, digits, _ and -");
                return false;
            }
            entry.Id = text.Trim();
            return true;
        }

        private static bool ReadCommand(YamlNode node, CustomEntry entry)
        {
            string command = Text(node, "command");
            if (string.IsNullOrWhiteSpace(command))
            {
                node.Error("an entry needs a command, for example command: ew reset {radius}");
                return false;
            }
            entry.Command = command.Trim();
            return true;
        }

        /// <summary>Position, radius and height; false when one of them is out of range (reported).</summary>
        private static bool ReadNumbers(YamlNode node, CustomEntry entry)
        {
            YamlNode position = node.Get("position");
            if (Present(position) && position.TryInt(out int index))
            {
                if (index < 0)
                    return Fail(position, "the position cannot be negative (0 is the first place in the menu)");
                entry.Position = index;
            }
            YamlNode radius = node.Get("radius");
            if (Present(radius) && radius.TryFloat(out float r))
            {
                if (r <= 0f)
                    return Fail(radius, "the radius must be greater than 0");
                entry.Radius = r;
            }
            YamlNode height = node.Get("height");
            if (Present(height) && height.TryFloat(out float h))
                entry.Height = h;
            return true;
        }

        private static bool Fail(YamlNode node, string message)
        {
            node.Error(message);
            return false;
        }

        private static string Text(YamlNode node, string key)
        {
            YamlNode value = node.Get(key);
            return Present(value) && value.TryString(out string text) ? text : null;
        }

        private static bool Flag(YamlNode node, string key)
        {
            YamlNode value = node.Get(key);
            return Present(value) && value.TryBool(out bool flag) && flag;
        }

        private static bool Present(YamlNode node) => node.Kind != YamlNodeKind.Missing && node.Kind != YamlNodeKind.Null;
    }
}
