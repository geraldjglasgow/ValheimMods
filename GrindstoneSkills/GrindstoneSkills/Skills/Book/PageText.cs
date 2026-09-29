using System;
using System.Text;
using PlateColumn;

namespace GrindstoneSkills
{
    /// <summary>
    /// Writes a <see cref="SkillPage"/> as the info pane's rich text: the line about the skill in a muted colour, the
    /// lines as bullets under their headings, then the perks, each name in gold with the level it needs or "unlocked".
    /// Every word with a tip (a line's term, a perk's name) becomes a <c>&lt;link&gt;</c> whose tip goes to the text's
    /// <see cref="LinkTips"/>, so hovering it shows the tip box the inventory's stat boxes use.
    /// </summary>
    internal sealed class PageText
    {
        private const string AboutColour = "#C9B899";
        private const string HeadingColour = "#D8C3A0";
        private const string TermColour = "#FFC847";
        private const string UnlockedColour = "#9BD46A";
        private const string LockedColour = "#A08E74";

        private readonly LinkTips tips;
        private readonly StringBuilder text = new StringBuilder();
        private int links;

        private PageText(LinkTips tips) => this.tips = tips;

        /// <summary>The page's text; the tips it links go to <paramref name="tips"/> (cleared first), none when it is null.</summary>
        public static string Write(SkillPage page, LinkTips tips)
        {
            tips?.Clear();
            PageText writer = new PageText(tips);
            if (page.About.Length > 0)
                writer.text.Append($"<color={AboutColour}>{page.About}</color>\n");
            writer.Entries(page);
            writer.Perks(page);
            return writer.text.ToString().Trim();
        }

        private void Entries(SkillPage page)
        {
            if (page.Entries.Count > 0 && !page.Entries[0].IsHeading)
                Gap();
            for (int i = 0; i < page.Entries.Count; i++)
            {
                PageEntry entry = page.Entries[i];
                if (!entry.IsHeading)
                    Bullet(entry.Term.Length > 0 ? WithTerm(entry) : entry.Text);
                else if (i + 1 < page.Entries.Count && !page.Entries[i + 1].IsHeading)
                    Heading(entry.Text);
            }
        }

        private void Perks(SkillPage page)
        {
            if (page.Perks.Count == 0)
                return;
            Heading("Perks");
            foreach (PagePerk perk in page.Perks)
                Bullet(Link(perk.Name, perk.Name, perk.Tip) + State(perk, page.Level));
        }

        private static string State(PagePerk perk, float level)
        {
            if (perk.Level <= 0f)
                return "";
            return level >= perk.Level
                ? $"  <color={UnlockedColour}>unlocked</color>"
                : $"  <color={LockedColour}>at level {perk.Level:0}</color>";
        }

        private void Heading(string words)
        {
            Gap();
            text.Append($"<color={HeadingColour}><size=85%>{words.ToUpperInvariant()}</size></color>\n");
        }

        private void Bullet(string line) => text.Append($"•<indent=1em>{line}</indent>\n");

        /// <summary>A blank line before a group, unless the text is empty or already ends with one.</summary>
        private void Gap()
        {
            if (text.Length > 0 && !text.ToString().EndsWith("\n\n", StringComparison.Ordinal))
                text.Append('\n');
        }

        private string WithTerm(PageEntry entry)
        {
            int at = entry.Text.IndexOf(entry.Term, StringComparison.Ordinal);
            if (at < 0)
                return Link(entry.Text, entry.Term, entry.Tip);
            return entry.Text.Substring(0, at) + Link(entry.Term, entry.Term, entry.Tip) + entry.Text.Substring(at + entry.Term.Length);
        }

        private string Link(string words, string topic, string tip)
        {
            if (tips == null || tip.Length == 0)
                return $"<color={TermColour}>{words}</color>";
            string id = "tip" + links++;
            tips.Set(id, topic, tip);
            return $"<link=\"{id}\"><color={TermColour}>{words}</color></link>";
        }
    }
}
