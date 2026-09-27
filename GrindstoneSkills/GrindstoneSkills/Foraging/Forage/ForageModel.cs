using System;
using System.Collections.Generic;
using YamlConfig;

namespace GrindstoneSkills
{
    /// <summary>
    /// The parsed GrindstoneSkills.Forage*.yml files: under <c>forage:</c> one entry per item prefab, a map of
    /// <c>stars</c>, <c>best</c> and <c>experience</c>. A later file replaces an earlier file's entry for the same item.
    /// Mistakes that make an entry meaningless (an unknown best-time word, a negative experience) are errors, so the
    /// file is rejected and the previous one stays. Item names are not checked here: the item database may not exist yet.
    /// </summary>
    public sealed class ForageModel : YamlModel
    {
        public Dictionary<string, ForageEntry> Items { get; } = new Dictionary<string, ForageEntry>(StringComparer.OrdinalIgnoreCase);

        protected override void Read(YamlNode root)
        {
            YamlNode forage = root.Get("forage");
            if (forage.Kind == YamlNodeKind.Missing || forage.Kind == YamlNodeKind.Null)
                return;
            foreach (KeyValuePair<string, YamlNode> entry in forage.Entries)
                ReadItem(entry.Key.Trim(), entry.Value);
        }

        protected override void Verify()
        {
            if (Errors.Count == 0 && Items.Count == 0)
                Warnings.Add("no items are listed under 'forage'; no pick trains Foraging");
        }

        private void ReadItem(string item, YamlNode node)
        {
            if (item.Length == 0)
            {
                node.Error("an item needs its prefab name as the key, for example 'Raspberry:'");
                return;
            }
            if (node.Kind == YamlNodeKind.Null)
            {
                Items[item] = new ForageEntry(item, false, BestTime.None, 1f);
                return;
            }
            if (node.Kind != YamlNodeKind.Map)
            {
                node.Error("an item is a map like { stars: true, best: [day, dry], experience: 1 }");
                return;
            }
            bool stars = node.Get("stars").TryBool(out bool starred) && starred;
            BestTime best = ReadBest(node.Get("best"));
            float experience = ReadExperience(node.Get("experience"));
            Items[item] = new ForageEntry(item, stars, best, experience);
        }

        /// <summary>The conditions listed (one word or a list); None when not given.</summary>
        private static BestTime ReadBest(YamlNode node)
        {
            BestTime best = BestTime.None;
            if (node.Kind == YamlNodeKind.Missing || node.Kind == YamlNodeKind.Null || !node.TryStringList(out List<string> words))
                return best;
            foreach (string word in words)
            {
                if (BestTimes.TryParse(word, out BestTime time))
                    best |= time;
                else
                    node.Error($"'{word}' is not a time; use {BestTimes.Allowed}");
            }
            if ((best & BestTime.Day) != 0 && (best & BestTime.Night) != 0 || (best & BestTime.Wet) != 0 && (best & BestTime.Dry) != 0)
                node.Warn("these conditions never hold together, so the plant is never at its best");
            return best;
        }

        /// <summary>1 when not given; a negative factor is an error.</summary>
        private static float ReadExperience(YamlNode node)
        {
            if (!node.TryFloat(out float experience))
                return 1f;
            if (experience >= 0f)
                return experience;
            node.Error("experience cannot be negative");
            return 0f;
        }
    }
}
