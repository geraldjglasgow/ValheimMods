namespace EliteCreaturesReborn.Raids
{
    /// <summary>
    /// The keys of the raids' words: the Raiders Chest's hover and why it will not sound a raid, the messages as a raid
    /// starts and ends, and the line under the minimap. Their English is in <c>translations/English.yml</c>; the
    /// Translations library writes them into the game's table in the player's language. The chest's name, its action and
    /// the horn's caption keep their own keys (<see cref="ChestPrefab.Word"/>, <see cref="ChestText.SoundWord"/>,
    /// <see cref="RaidHorn.CaptionWord"/>), the difficulties theirs (<see cref="RaidBand.Word"/>). A raid is named after
    /// its creature in every sentence that names it ("$1 raid"), so a translation words the name with the sentence.
    /// </summary>
    internal static class RaidWords
    {
        // ---- The Raiders Chest's hover (ChestText). ----
        public const string CoinsOne = "ecr_raid_coins_one";
        public const string CoinsMany = "ecr_raid_coins_many";
        public const string TrophiesOne = "ecr_raid_trophies_one";
        public const string TrophiesMany = "ecr_raid_trophies_many";
        public const string CoinsAndTrophies = "ecr_raid_coins_and_trophies";
        public const string StakeLine = "ecr_raid_stake_line";
        public const string Empty = "ecr_raid_empty";
        public const string HoldToStop = "ecr_raid_hold_to_stop";
        public const string Stopping = "ecr_raid_stopping";
        public const string Locked = "ecr_raid_locked";

        // ---- Why no raid is sounded: the chest's rules, the raid's checks, the owner's answer. ----
        public const string Off = "ecr_raid_off";
        public const string NotInBase = "ecr_raid_not_in_base";
        public const string Resting = "ecr_raid_resting";
        public const string GameRaidNear = "ecr_raid_game_raid_near";
        public const string ChestRaidNear = "ecr_raid_chest_raid_near";
        public const string InUse = "ecr_raid_in_use";
        public const string NoGold = "ecr_raid_no_gold";
        public const string Moved = "ecr_raid_moved";
        public const string NoHost = "ecr_raid_no_host";
        public const string NotOwner = "ecr_raid_not_owner";
        public const string AlreadyOn = "ecr_raid_already_on";
        public const string NoneUnlocked = "ecr_raid_none_unlocked";

        // ---- A raid starts and ends (RaidText), sent from the host's owner. ----
        public const string Started = "ecr_raid_started";
        public const string Won = "ecr_raid_won";
        public const string WonTrophies = "ecr_raid_won_trophies";
        public const string Robbed = "ecr_raid_robbed";
        public const string Stopped = "ecr_raid_stopped";
        public const string Abandoned = "ecr_raid_abandoned";
        public const string TimedOut = "ecr_raid_timed_out";

        // ---- The line under the minimap and in the chest's hover during a raid (RaidText.Line). ----
        public const string LineCountdown = "ecr_raid_line_countdown";
        public const string LineBreak = "ecr_raid_line_break";
        public const string LineWave = "ecr_raid_line_wave";
    }
}
