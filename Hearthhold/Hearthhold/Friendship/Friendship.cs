using UnityEngine;

namespace Hearthhold
{
    /// <summary>
    /// A player's friendship with each trader: 0 to 1000 points, 100 per heart, 0 to 10 hearts, kept in the player's
    /// own custom data (<see cref="FriendshipKeys"/>), so it is personal and saved with the character. Each heart takes
    /// 1% off what that trader sells, at most 10%. While the "Trader Friendship" switch is off nothing reads as a
    /// discount and no gift is taken, but the points stay stored.
    /// </summary>
    public static class Friendship
    {
        public const int PointsPerHeart = 100;
        public const int MaxHearts = 10;
        public const int MaxPoints = PointsPerHeart * MaxHearts;
        public const int PercentPerHeart = 1;

        private static readonly string[] lines = BuildLines();

        public static bool On => Settings.TraderFriendship != null && Settings.TraderFriendship.Value;

        public static int Points(Player player, TraderInfo trader) => ReadInt(player, trader.PointsKey, 0);

        public static int Hearts(Player player, TraderInfo trader) => Mathf.Clamp(Points(player, trader) / PointsPerHeart, 0, MaxHearts);

        /// <summary>The discount this player gets from this trader, 0 to 10 (percent); 0 while the switch is off.</summary>
        public static int DiscountPercent(Player player, TraderInfo trader) =>
            On && player != null ? Hearts(player, trader) * PercentPerHeart : 0;

        /// <summary>A price with the discount taken off, rounded down but never below 1 (a free item stays free).</summary>
        public static int Discounted(int price, int percent)
        {
            if (percent <= 0 || price <= 1)
                return price;
            return Mathf.Max(1, price * (100 - percent) / 100);
        }

        /// <summary>The in-game day of this player's last gift to this trader, -1 for never.</summary>
        public static int LastGiftDay(Player player, TraderInfo trader) => ReadInt(player, trader.GiftDayKey, -1);

        /// <summary>Records a gift on <paramref name="day"/> worth <paramref name="points"/>; returns the new heart count.</summary>
        public static int AddGift(Player player, TraderInfo trader, int points, int day)
        {
            int total = Mathf.Clamp(Points(player, trader) + points, 0, MaxPoints);
            player.m_customData[trader.PointsKey] = total.ToString();
            player.m_customData[trader.GiftDayKey] = day.ToString();
            return total / PointsPerHeart;
        }

        /// <summary>The friendship as one line, e.g. "Friendship 3/10, 3% off"; built once per heart count.</summary>
        public static string Line(int hearts) => lines[Mathf.Clamp(hearts, 0, MaxHearts)];

        private static string[] BuildLines()
        {
            string[] built = new string[MaxHearts + 1];
            for (int hearts = 0; hearts <= MaxHearts; hearts++)
            {
                string off = hearts > 0 ? $", {hearts * PercentPerHeart}% off" : "";
                built[hearts] = $"Friendship {hearts}/{MaxHearts}{off}";
            }
            return built;
        }

        private static int ReadInt(Player player, string key, int fallback)
        {
            if (player == null || !player.m_customData.TryGetValue(key, out string text))
                return fallback;
            return int.TryParse(text, out int value) ? value : fallback;
        }
    }
}
