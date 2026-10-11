using System;
using System.Globalization;

namespace EliteCreaturesReborn.Raids
{
    /// <summary>
    /// What the Raiders Chest says: its hover lines and why it will not sound a raid. In English, like the raids' other
    /// texts (<see cref="RaidText"/>), with the game's own words where it has them ($piece_container_open, the key names)
    /// and the chest's few words in the translation table (<see cref="ChestWords"/>). The hover localizes the whole text
    /// once each time it is rebuilt, never per frame.
    /// </summary>
    internal static class ChestText
    {
        /// <summary>The word for Shift + E's action, "Sound the raid".</summary>
        public const string SoundWord = "hud_ecr_raid_sound";

        public const string Locked = "The Raiders Chest is locked while the raid is on. Hold to stop the raid.";

        public const string HoldToStop =
            "Hold [<color=yellow><b>$KEY_Use</b></color>] to stop the raid (the raiders' gold is lost)";

        public const string Empty = "Put gold coins in to sound a raid.";

        public const string Off = "Raids from the Raiders Chest are off on this server.";

        public const string NotInBase = "Not in a base: a raid needs a base to attack.";

        public const string InUse = "Someone has the Raiders Chest open.";

        public const string NoGold = "No gold, no raid.";

        public const string Moved = "The Raiders Chest changed hands just then: try again.";

        public const string GameRaidNear = "Another raid is already on nearby.";

        public const string ChestRaidNear = "Another raid is already on within 200 m.";

        private const string Open = "[<color=yellow><b>$KEY_Use</b></color>] $piece_container_open";

        private const string SoundKeys = "   [<color=yellow><b>$KEY_AltPlace + $KEY_Use</b></color>] $" + SoundWord;

        private const string SoundPad = "   [<color=yellow><b>$KEY_AltKeys + $KEY_Use</b></color>] $" + SoundWord;

        /// <summary>"1,000 coins - Deadly raid (coins back x2, drops x5)": the raid these coins would make now.</summary>
        public static string Stake(int coins, RaidHeat heat) => FormattableString.Invariant(
            $"{Coins(coins)} - {heat.Band.Name} raid (coins back x{heat.Band.CoinsBack:0.##}, drops x{heat.Drops:0.##})");

        /// <summary>"1,000 coins", for a chest that cannot sound a raid.</summary>
        public static string Coins(int coins) =>
            coins.ToString("N0", CultureInfo.InvariantCulture) + (coins == 1 ? " coin" : " coins");

        /// <summary>The cooldown still to run, in milliseconds of the server clock.</summary>
        public static string Resting(long ms) => "The horn must rest: ready in " + RaidText.Clock((int)((ms + 999L) / 1000L));

        /// <summary>The hold on E, in tenths.</summary>
        public static string Stopping(int step) => "Stopping the raid... " + (step * 10).ToString(CultureInfo.InvariantCulture) + "%";

        /// <summary>The keys line: E opens; Shift + E (the gamepad's own pair) sounds the raid when it may.</summary>
        public static string Keys(bool canSound, bool gamepadKeys) =>
            canSound ? Open + (gamepadKeys ? SoundPad : SoundKeys) : Open;
    }
}
