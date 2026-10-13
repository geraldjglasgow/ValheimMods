using EliteCreaturesReborn.Patches;
using HarmonyLib;

namespace EliteCreaturesReborn.Arena.Trial
{
    /// <summary>
    /// The trial's words in the game's translation table, as the Raiders Chest keeps its own
    /// (<see cref="Raids.ChestWords"/>): the HUD lines, the start, boss, cleared and end messages, and the word after an
    /// arena death, with "$1" to "$3" for the words put in them. English for every language until translations exist;
    /// added after each language setup, which rebuilds the table. A message travels as its key and its words, so each
    /// player reads it in their own language (<see cref="Show"/>).
    /// </summary>
    [HarmonyPatch(typeof(Localization), nameof(Localization.SetupLanguage))]
    internal static class ArenaWords
    {
        public const string Name = "ecr_arena_name";
        public const string Line = "ecr_arena_line";
        public const string Countdown = "ecr_arena_countdown";
        public const string Started = "ecr_arena_started";
        public const string Boss = "ecr_arena_boss";
        public const string Ended = "ecr_arena_ended";
        public const string BreakLine = "ecr_arena_break_line";
        public const string Cleared = "ecr_arena_cleared";
        public const string Spared = "ecr_arena_spared";
        public const string Reward = "ecr_arena_reward";
        public const string RewardCoins = "ecr_arena_reward_coins";
        public const string RewardRunes = "ecr_arena_reward_runes";
        public const string RewardEssence = "ecr_arena_reward_essence";
        public const string RewardXp = "ecr_arena_reward_xp";

        public static void Add(Localization? localization)
        {
            if (localization == null)
            {
                return;
            }
            localization.AddWord(Name, "Hel's Trial");
            localization.AddWord(Line, "Hel's Trial - wave $1 - $2 left - $3");
            localization.AddWord(Countdown, "Hel's Trial - the first wave in $1");
            localization.AddWord(Started, "Hel wakes. Her trial begins!");
            localization.AddWord(Boss, "Hel sends $1!");
            localization.AddWord(Ended, "Hel's Trial ends at wave $1: $2 kills, score $3.");
            localization.AddWord(BreakLine, "Hel's Trial - wave $1 cleared - next wave in $2 - $3");
            localization.AddWord(Cleared, "Wave $1 cleared. The next comes in $2 seconds.");
            localization.AddWord(Spared, "Hel spares your skills.");
            localization.AddWord(Reward, "Hel rewards you: $1.");
            localization.AddWord(RewardCoins, "$1 coins");
            localization.AddWord(RewardRunes, "$1 runes");
            localization.AddWord(RewardEssence, "$1 essence");
            localization.AddWord(RewardXp, "+$1% skill XP");
        }

        /// <summary>A message for this player's screen: the key's words with the given ones put in, each of those
        /// translated first (a boss arrives as its name key, "$enemy_dragon").</summary>
        public static void Show(string key, string first, string second, string third)
        {
            Localization? words = Localization.instance;
            if (MessageHud.instance == null || words == null || string.IsNullOrEmpty(key))
            {
                return;
            }
            string text = words.Localize("$" + key, words.Localize(first), words.Localize(second), words.Localize(third));
            MessageHud.instance.ShowMessage(MessageHud.MessageType.Center, text);
        }

        /// <summary>The HUD line, with its numbers put in. Called only when a number in it changes.</summary>
        public static string HudLine(int wave, int left, string score) => Localization.instance != null
            ? Localization.instance.Localize("$" + Line, wave.ToString(), left.ToString(), score)
            : "";

        /// <summary>The HUD line during a break: the wave just cleared and the seconds to the next.</summary>
        public static string HudBreak(int wave, int seconds, string score) => Localization.instance != null
            ? Localization.instance.Localize("$" + BreakLine, wave.ToString(), seconds.ToString(), score)
            : "";

        /// <summary>The HUD line during the countdown.</summary>
        public static string HudCountdown(string clock) =>
            Localization.instance != null ? Localization.instance.Localize("$" + Countdown, clock) : "";

        private static void Postfix(Localization __instance) =>
            SafeCall.Run("Hel's Trial words", static l => Add(l), __instance);
    }
}
