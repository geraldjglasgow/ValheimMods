using System;
using Translations;

namespace EliteCreaturesReborn.Arena.Trial
{
    /// <summary>
    /// The keys of the trial's words - the HUD lines, the start, boss, cleared, recall and end messages (the end also in
    /// the chat window), Hel's Anvil, the reward and the word after an arena death, with "$1", "$2" ... for the values put
    /// in them - and the text built from them. Their English is in <c>translations/English.yml</c>; the Translations
    /// library gives them in the player's language. A message from the trial's owner travels as its key and its values
    /// (<see cref="LangWire"/>), so each player reads it in their own language (<see cref="Receive"/>).
    /// </summary>
    internal static class ArenaWords
    {
        public const string Name = "ecr_arena_name";
        public const string Line = "ecr_arena_line";
        public const string LineReady = "ecr_arena_line_ready";
        public const string Countdown = "ecr_arena_countdown";
        public const string Started = "ecr_arena_started";
        public const string Boss = "ecr_arena_boss";
        public const string Ended = "ecr_arena_ended";
        public const string BreakLine = "ecr_arena_break_line";
        public const string Cleared = "ecr_arena_cleared";
        public const string Spared = "ecr_arena_spared";
        public const string ChatLine = "ecr_arena_chat_line";
        public const string Reward = "ecr_arena_reward";
        public const string RewardItem = "ecr_arena_reward_item";
        public const string RewardJoin = "ecr_arena_reward_list";
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
        public const string LevelLine = "ecr_arena_level_line";
        public const string Won = "ecr_arena_won";

        /// <summary>A message from the trial's owner (<see cref="ArenaNet"/>), in this player's language, in the centre;
        /// the end also in the chat window. A boss's name arrives as its name key ("$enemy_dragon") and is read here.</summary>
        public static void Receive(string message)
        {
            string text = LangWire.Read(message);
            Show(text);
            if (message.StartsWith(LangWire.Message(Ended, ""), StringComparison.Ordinal)
                || message.StartsWith(LangWire.Message(Won, ""), StringComparison.Ordinal))
            {
                ToChat(text);
            }
        }

        /// <summary>A centre message on this player's screen, already in their language.</summary>
        public static void Show(string text)
        {
            if (MessageHud.instance != null && text.Length > 0)
            {
                MessageHud.instance.ShowMessage(MessageHud.MessageType.Center, text);
            }
        }

        /// <summary>A line in the chat window, which opens for a moment as it does for a new chat message, so the end and
        /// the reward can be read again after the centre message fades.</summary>
        public static void ToChat(string text)
        {
            Chat chat = Chat.instance;
            if (chat == null)
            {
                return;
            }
            chat.AddString(Lang.Format(ChatLine, "<color=#80ebe0>" + Lang.Get(Name) + "</color>", text));
            chat.m_hideTimer = 0f;
        }

        /// <summary>The HUD line, with its numbers put in. Called only when a number in it changes.</summary>
        public static string HudLine(string biome, int wave, int left, string souls) =>
            Lang.Format(Line, biome, wave.ToString(), left.ToString(), souls);

        /// <summary>The HUD line while Hel's recall of the stragglers runs: its clock after the creatures left.</summary>
        public static string HudRecall(string biome, int wave, int left, string clock, string souls) =>
            Lang.Format(RecallLine, biome, wave.ToString(), left.ToString(), clock, souls);

        /// <summary>The HUD line during a break: the wave just cleared and the seconds to the next.</summary>
        public static string HudBreak(string biome, int wave, int seconds, string souls) =>
            Lang.Format(BreakLine, biome, wave.ToString(), seconds.ToString(), souls);

        /// <summary>A biome's name in the player's language, by the game's own words ("$biome_ashlands").</summary>
        public static string BiomeName(Heightmap.Biome biome) => Localization.instance != null
            ? Localization.instance.Localize("$biome_" + biome.ToString().ToLowerInvariant())
            : biome.ToString();

        /// <summary>A rest's HUD line with its ready part after it: "... - F: ready (1/3)", or "... - (ready) 1/3" once
        /// this player has voted.</summary>
        public static string HudReady(string line, bool mine, int ready, int players) => Lang.Format(LineReady, line,
            Lang.Format(mine ? ReadyMine : ReadyHint, ready.ToString(), players.ToString()));

        /// <summary>The biome on the Infinite trial's HUD line, with its level: "Meadows, level 3".</summary>
        public static string HudLevel(string biome, int level) => Lang.Format(LevelLine, biome, level.ToString());

        /// <summary>The HUD line during the countdown.</summary>
        public static string HudCountdown(string biome, string clock) => Lang.Format(Countdown, biome, clock);
    }
}
