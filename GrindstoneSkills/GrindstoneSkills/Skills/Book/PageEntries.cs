namespace GrindstoneSkills
{
    /// <summary>A line or a heading of a <see cref="SkillPage"/>, with the word the line explains on hover, if any.</summary>
    internal sealed class PageEntry
    {
        public PageEntry(string text, string term, string tip, bool heading)
        {
            Text = text ?? "";
            Term = term ?? "";
            Tip = tip ?? "";
            IsHeading = heading;
        }

        public string Text { get; }
        public string Term { get; }
        public string Tip { get; }
        public bool IsHeading { get; }
    }

    /// <summary>A named perk of a <see cref="SkillPage"/>: the level that unlocks it and its hover description.</summary>
    internal sealed class PagePerk
    {
        public PagePerk(string name, float level, string tip)
        {
            Name = name ?? "";
            Level = level;
            Tip = tip ?? "";
        }

        public string Name { get; }
        public float Level { get; }
        public string Tip { get; }
    }
}
