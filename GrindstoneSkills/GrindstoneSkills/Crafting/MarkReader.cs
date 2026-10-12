using System.Collections.Generic;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace GrindstoneSkills
{
    /// <summary>
    /// Reads an item's Maker's Mark for the hot paths: the max durability behind every durability bar on every UI
    /// refresh, and the hovered item's tooltip every frame. Items with no custom data, nearly all of them, stop at the
    /// first test. Otherwise the custom data is parsed the first time it is read and the result kept beside the item
    /// (a weak table, so nothing outlives the item), with the dictionary it came from and its size: a new dictionary or
    /// one with a key added is parsed again, and every other read allocates nothing. Runs on every machine that asks.
    /// </summary>
    public static class MarkReader
    {
        private sealed class Mark
        {
            public Dictionary<string, string> Source;
            public int Count;
            public float Factor = 1f;
            public string Line;
        }

        private static readonly ConditionalWeakTable<ItemDrop.ItemData, Mark> marks = new ConditionalWeakTable<ItemDrop.ItemData, Mark>();
        private static readonly ConditionalWeakTable<ItemDrop.ItemData, Mark>.CreateValueCallback create = static _ => new Mark();

        /// <summary>The item's max durability multiplier: 1 plus its mark's share, 1 without a mark.</summary>
        public static float Factor(ItemDrop.ItemData item)
        {
            Dictionary<string, string> data = item.m_customData;
            return data == null || data.Count == 0 ? 1f : Read(item, data).Factor;
        }

        /// <summary>The item's tooltip line, null without a mark.</summary>
        public static string Line(ItemDrop.ItemData item)
        {
            Dictionary<string, string> data = item.m_customData;
            return data == null || data.Count == 0 ? null : Read(item, data).Line;
        }

        private static Mark Read(ItemDrop.ItemData item, Dictionary<string, string> data)
        {
            Mark mark = marks.GetValue(item, create);
            if (!ReferenceEquals(mark.Source, data) || mark.Count != data.Count)
                Parse(mark, data);
            return mark;
        }

        private static void Parse(Mark mark, Dictionary<string, string> data)
        {
            mark.Source = data;
            mark.Count = data.Count;
            float share = data.TryGetValue(Keys.MakerLevel, out string text)
                && float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out float level)
                ? MakersMark.Share(level)
                : 0f;
            mark.Factor = 1f + share;
            mark.Line = share > 0f ? MakersMark.LineFor(share) : null;
        }
    }
}
