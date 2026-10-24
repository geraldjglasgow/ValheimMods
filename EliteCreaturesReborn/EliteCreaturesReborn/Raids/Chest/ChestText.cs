using System.Globalization;
using Translations;

namespace EliteCreaturesReborn.Raids
{
    /// <summary>
    /// What the Raiders Chest says: its hover lines in this machine's language (keys in <see cref="RaidWords"/>), with the
    /// game's own words where it has them ($piece_container_open, the key names). Why it will not sound a raid is a
    /// <c>LangWire</c> message (<see cref="ChestRules"/>, <see cref="ChestSounding"/>), read where it is shown. The hover
    /// localizes the whole text once each time it is rebuilt, never per frame.
    /// </summary>
    internal static class ChestText
    {
        /// <summary>The word for Shift + E's action, "Sound the raid".</summary>
        public const string SoundWord = "hud_ecr_raid_sound";

        private const string UseKey = "[<color=yellow><b>$KEY_Use</b></color>]";

        private const string Open = UseKey + " $piece_container_open";

        private const string SoundKeys = "   [<color=yellow><b>$KEY_AltPlace + $KEY_Use</b></color>] $" + SoundWord;

        private const string SoundPad = "   [<color=yellow><b>$KEY_AltKeys + $KEY_Use</b></color>] $" + SoundWord;

        /// <summary>"1,000 coins and 12 trophies - Deadly raid (back x2, drops x5)": the raid this stake would make now.</summary>
        public static string Stake(int coins, int trophies, RaidHeat heat) => Lang.Format(RaidWords.StakeLine,
            Amount(coins, trophies), heat.Band.Title, Times(heat.Band.CoinsBack), Times(heat.Drops));

        /// <summary>"1,000 coins", "12 trophies" or "1,000 coins and 12 trophies".</summary>
        public static string Amount(int coins, int trophies)
        {
            if (trophies <= 0)
            {
                return Coins(coins);
            }
            string counted = Lang.Format(trophies == 1 ? RaidWords.TrophiesOne : RaidWords.TrophiesMany,
                trophies.ToString("N0", CultureInfo.InvariantCulture));
            return coins > 0 ? Lang.Format(RaidWords.CoinsAndTrophies, Coins(coins), counted) : counted;
        }

        /// <summary>"1,000 coins", for a chest that cannot sound a raid.</summary>
        public static string Coins(int coins) =>
            Lang.Format(coins == 1 ? RaidWords.CoinsOne : RaidWords.CoinsMany, coins.ToString("N0", CultureInfo.InvariantCulture));

        /// <summary>How to stop the raid, with E's key (localized with the rest of the hover).</summary>
        public static string HoldToStop() => Lang.Format(RaidWords.HoldToStop, UseKey);

        /// <summary>The hold on E, in tenths.</summary>
        public static string Stopping(int step) =>
            Lang.Format(RaidWords.Stopping, (step * 10).ToString(CultureInfo.InvariantCulture));

        /// <summary>Told to the player whose E finds the chest locked by its raid.</summary>
        public static string Locked() => Lang.Get(RaidWords.Locked);

        /// <summary>The cooldown still to run, in milliseconds of the server clock, as a message.</summary>
        public static string Resting(long ms) => LangWire.Message(RaidWords.Resting, RaidText.Clock((int)((ms + 999L) / 1000L)));

        /// <summary>The keys line: E opens; Shift + E (the gamepad's own pair) sounds the raid when it may.</summary>
        public static string Keys(bool canSound, bool gamepadKeys) =>
            canSound ? Open + (gamepadKeys ? SoundPad : SoundKeys) : Open;

        private static string Times(float factor) => factor.ToString("0.##", CultureInfo.InvariantCulture);
    }
}
