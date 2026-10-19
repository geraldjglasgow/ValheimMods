using System.Collections.Generic;
using System.Globalization;
using EliteCreaturesReborn.Arena.Trial;
using Translations;
using UnityEngine;

namespace EliteCreaturesReborn.Arena.Rewards
{
    /// <summary>
    /// The reward's message, in the player's own language: "Hel rewards you: Bronze Battle Idol, Iron Protection Idol, 412
    /// coins, 6 runes, 3 essence, +18% skill XP." Named items by the game's own names (a count after the name when more
    /// than one), then the coins, the runes and the essence as counts, and the skill XP bonus as a percent. Empty for an
    /// empty reward. <see cref="Items"/> is the list alone, for Hel's "Current rewards: ..." (<see cref="RewardPreview"/>).
    /// </summary>
    internal static class RewardText
    {
        private static readonly List<string> Parts = new List<string>();

        public static string For(RewardList list)
        {
            string items = Items(list);
            return items.Length > 0 ? Lang.Format(ArenaWords.Reward, items) : "";
        }

        /// <summary>The reward's parts alone, comma separated: "Bronze Battle Idol, 412 coins, +18% skill XP"; empty for
        /// an empty reward.</summary>
        public static string Items(RewardList list)
        {
            Localization? words = Localization.instance;
            if (words == null || list.Empty)
            {
                return "";
            }
            Parts.Clear();
            Named(words, list);
            Counted(list, RewardList.Kind.Coins, ArenaWords.RewardCoins);
            Counted(list, RewardList.Kind.Rune, ArenaWords.RewardRunes);
            Counted(list, RewardList.Kind.Essence, ArenaWords.RewardEssence);
            int percent = Mathf.RoundToInt(list.XpFactor * 100f);
            if (percent > 0)
            {
                Parts.Add(Lang.Format(ArenaWords.RewardXp, percent.ToString(CultureInfo.InvariantCulture)));
            }
            string text = Joined();
            Parts.Clear();
            return text;
        }

        // The parts one after another by the language's own list pattern ("$1, $2" in English).
        private static string Joined()
        {
            string text = Parts.Count > 0 ? Parts[0] : "";
            for (int i = 1; i < Parts.Count; i++)
            {
                text = Lang.Format(ArenaWords.RewardJoin, text, Parts[i]);
            }
            return text;
        }

        private static void Named(Localization words, RewardList list)
        {
            foreach (RewardList.Entry entry in list.Entries)
            {
                GameObject? prefab = entry.Type == RewardList.Kind.Item && ObjectDB.instance != null
                    ? ObjectDB.instance.GetItemPrefab(entry.Prefab) : null;
                ItemDrop? item = prefab != null ? prefab.GetComponent<ItemDrop>() : null;
                if (item != null)
                {
                    string name = words.Localize(item.m_itemData.m_shared.m_name);
                    Parts.Add(entry.Count > 1 ? Lang.Format(ArenaWords.RewardItem, name, entry.Count.ToString()) : name);
                }
            }
        }

        private static void Counted(RewardList list, RewardList.Kind kind, string key)
        {
            int count = 0;
            foreach (RewardList.Entry entry in list.Entries)
            {
                count += entry.Type == kind ? entry.Count : 0;
            }
            if (count > 0)
            {
                Parts.Add(Lang.Format(key, count.ToString("N0", CultureInfo.InvariantCulture)));
            }
        }
    }
}
