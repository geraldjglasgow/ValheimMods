using EliteCreaturesReborn.Patches;
using HarmonyLib;

namespace EliteCreaturesReborn.Arena.Trial
{
    /// <summary>
    /// The trial's words in the game's translation table, as the Raiders Chest keeps its own
    /// (<see cref="Raids.ChestWords"/>): the HUD lines, the start, boss, cleared, recall and end messages (the end also
    /// in the chat window), and the word after an arena death, with "$1" to "$3" for the words put in them. English for every language until translations exist;
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
        public const string ReadyHint = "ecr_arena_ready_hint";
        public const string ReadyMine = "ecr_arena_ready_mine";
        public const string ReadyVote = "ecr_arena_ready_vote";
        public const string AllReady = "ecr_arena_all_ready";
        public const string Anvil = "ecr_arena_anvil";
        public const string AnvilRepair = "ecr_arena_anvil_repair";
        public const string AnvilPoor = "ecr_arena_anvil_poor";
        public const string AnvilCold = "ecr_arena_anvil_cold";
        public const string AnvilDone = "ecr_arena_anvil_done";
        public const string AnvilNothing = "ecr_arena_anvil_nothing";
        public const string CallHint = "ecr_arena_call_hint";
        public const string Called = "ecr_arena_called";
        public const string BossWaits = "ecr_arena_boss_waits";
        public const string WaveDone = "ecr_arena_wave_done";
        public const string ClearedFirst = "ecr_arena_cleared_first";
        public const string Recall = "ecr_arena_recall";
        public const string RecallLine = "ecr_arena_recall_line";
        public const string RewardNow = "ecr_arena_reward_now";
        public const string RewardNone = "ecr_arena_reward_none";

        public static void Add(Localization? localization)
        {
            if (localization == null)
            {
                return;
            }
            localization.AddWord(Name, "Hel's Trial");
            localization.AddWord(Line, "Hel's Trial - $1, wave $2 - $3 left - $4 souls");
            localization.AddWord(Countdown, "Hel's Trial - $1 - the first wave in $2");
            localization.AddWord(Started, "Hel wakes. Her trial begins!");
            localization.AddWord(Boss, "Hel sends $1!");
            localization.AddWord(Ended, "Hel's Trial ends at wave $1: $2 kills, $3 souls.");
            localization.AddWord(BreakLine, "Hel's Trial - $1, wave $2 cleared - next wave in $3 - $4 souls");
            localization.AddWord(Cleared, "Wave $1 cleared. The next comes in $2 seconds.");
            localization.AddWord(Spared, "Hel spares your skills and your gear.");
            RewardWords(localization);
            AnvilWords(localization);
            ReadyWords(localization);
            CallWords(localization);
            RecallWords(localization);
        }

        private static void RewardWords(Localization localization)
        {
            localization.AddWord(Reward, "Hel rewards you: $1.");
            localization.AddWord(RewardCoins, "$1 coins");
            localization.AddWord(RewardRunes, "$1 runes");
            localization.AddWord(RewardEssence, "$1 essence");
            localization.AddWord(RewardXp, "+$1% skill XP");
            localization.AddWord(RewardNow, "Current rewards: $1");
            localization.AddWord(RewardNone, "none yet");
        }

        private static void RecallWords(Localization localization)
        {
            localization.AddWord(ClearedFirst,
                "Wave $1 cleared. The next comes in $2 seconds. Hel pays your reward when her trial ends.");
            localization.AddWord(Recall, "Hel marks the last of the dead on your map. She recalls them in $1 seconds.");
            localization.AddWord(RecallLine, "Hel's Trial - $1, wave $2 - $3 left, recalled in $4 - $5 souls");
        }

        private static void CallWords(Localization localization)
        {
            localization.AddWord(CallHint, "Call the next wave");
            localization.AddWord(Called, "$1 calls the next wave!");
            localization.AddWord(BossWaits, "Hel waits until $1 falls.");
            localization.AddWord(WaveDone, "Wave $1 cleared.");
        }

        private static void ReadyWords(Localization localization)
        {
            localization.AddWord(ReadyHint, "F: ready ($1/$2)");
            localization.AddWord(ReadyMine, "(ready) $1/$2");
            localization.AddWord(ReadyVote, "Ready: $1 of $2");
            localization.AddWord(AllReady, "All ready - the dead come!");
        }

        private static void AnvilWords(Localization localization)
        {
            localization.AddWord(Anvil, "Hel's Anvil");
            localization.AddWord(AnvilRepair, "Repair your gear ($1 souls)");
            localization.AddWord(AnvilPoor, "Not enough souls ($1)");
            localization.AddWord(AnvilCold, "The anvil is cold while the dead come");
            localization.AddWord(AnvilDone, "Hel's Anvil mends your gear for $1 souls.");
            localization.AddWord(AnvilNothing, "Nothing you carry needs mending.");
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
            if (key == Ended)
            {
                ToChat(text);
            }
        }

        /// <summary>A line in the chat window, which opens for a moment as it does for a new chat message, so the end and
        /// the reward can be read again after the centre message fades.</summary>
        public static void ToChat(string text)
        {
            Chat chat = Chat.instance;
            if (chat == null || Localization.instance == null)
            {
                return;
            }
            chat.AddString("<color=#80ebe0>" + Localization.instance.Localize("$" + Name) + "</color>: " + text);
            chat.m_hideTimer = 0f;
        }

        /// <summary>The HUD line, with its numbers put in. Called only when a number in it changes.</summary>
        public static string HudLine(string biome, int wave, int left, string souls) => Localization.instance != null
            ? Localization.instance.Localize("$" + Line, biome, wave.ToString(), left.ToString(), souls)
            : "";

        /// <summary>The HUD line while Hel's recall of the stragglers runs: its clock after the creatures left.</summary>
        public static string HudRecall(string biome, int wave, int left, string clock, string souls) =>
            Localization.instance != null
                ? Localization.instance.Localize("$" + RecallLine, biome, wave.ToString(), left.ToString(), clock, souls)
                : "";

        /// <summary>The HUD line during a break: the wave just cleared and the seconds to the next.</summary>
        public static string HudBreak(string biome, int wave, int seconds, string souls) => Localization.instance != null
            ? Localization.instance.Localize("$" + BreakLine, biome, wave.ToString(), seconds.ToString(), souls)
            : "";

        /// <summary>A biome's name in the player's language, by the game's own words ("$biome_ashlands").</summary>
        public static string BiomeName(Heightmap.Biome biome) => Localization.instance != null
            ? Localization.instance.Localize("$biome_" + biome.ToString().ToLowerInvariant())
            : biome.ToString();

        /// <summary>The ready part of the HUD line in a rest: "F: ready (1/3)", or "(ready) 1/3" once voted.</summary>
        public static string HudReady(bool mine, int ready, int players) => Localization.instance != null
            ? Localization.instance.Localize("$" + (mine ? ReadyMine : ReadyHint), ready.ToString(), players.ToString())
            : "";

        /// <summary>The HUD line during the countdown.</summary>
        public static string HudCountdown(string biome, string clock) =>
            Localization.instance != null ? Localization.instance.Localize("$" + Countdown, biome, clock) : "";

        private static void Postfix(Localization __instance) =>
            SafeCall.Run("Hel's Trial words", static l => Add(l), __instance);
    }
}
