using System.Collections.Generic;
using System.Globalization;
using EliteCreaturesReborn.Arena.Trial;
using UnityEngine;

namespace EliteCreaturesReborn.Arena.Rewards
{
    /// <summary>
    /// The reward's message, in the player's own language: "Hel rewards you: Bronze Battle Idol, Iron Protection Idol, 412
    /// coins, 6 runes, 3 essence, +18% skill XP." Named items by the game's own names (a count after the name when more
    /// than one), then the coins, the runes and the essence as counts, and the skill XP bonus as a percent. Empty for an
    /// empty reward.
    /// </summary>
    internal static class RewardText
    {
        private static readonly List<string> Parts = new List<string>();

        public static string For(RewardList list)
        {
            Localization? words = Localization.instance;
            if (words == null || list.Empty)
            {
                return "";
            }
            Parts.Clear();
            Named(words, list);
            Counted(words, list, RewardList.Kind.Coins, ArenaWords.RewardCoins);
            Counted(words, list, RewardList.Kind.Rune, ArenaWords.RewardRunes);
            Counted(words, list, RewardList.Kind.Essence, ArenaWords.RewardEssence);
            int percent = Mathf.RoundToInt(list.XpFactor * 100f);
            if (percent > 0)
            {
                Parts.Add(words.Localize("$" + ArenaWords.RewardXp, percent.ToString(CultureInfo.InvariantCulture)));
            }
            string text = Parts.Count > 0 ? words.Localize("$" + ArenaWords.Reward, string.Join(", ", Parts)) : "";
            Parts.Clear();
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
                    Parts.Add(entry.Count > 1 ? name + " x" + entry.Count : name);
                }
            }
        }

        private static void Counted(Localization words, RewardList list, RewardList.Kind kind, string key)
        {
            int count = 0;
            foreach (RewardList.Entry entry in list.Entries)
            {
                count += entry.Type == kind ? entry.Count : 0;
            }
            if (count > 0)
            {
                Parts.Add(words.Localize("$" + key, count.ToString("N0", CultureInfo.InvariantCulture)));
            }
        }
    }
}
