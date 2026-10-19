using System;
using System.Collections.Generic;
using System.Globalization;
using EliteCreaturesReborn.Util;
using PatchGuard;
using Translations;
using UnityEngine;

namespace EliteCreaturesReborn.Arena.Trial
{
    /// <summary>
    /// The trial's line under the minimap for every living player in the hall while it runs (features/arena.md section 3,
    /// "Souls"): "Hel's Trial - Ashlands, wave 12 - 7 left - 342 souls", "Hel's Trial - Ashlands, wave 12 cleared - next
    /// wave in 14 - 342 souls" in a break (the player's own souls), or "Hel's Trial - Ashlands - the first wave in 0:07"
    /// in the countdown, "Hel's Trial - Ashlands, wave 12 - 2 left, recalled in 0:45 - 342 souls" while Hel's recall of
    /// the stragglers runs (<see cref="ArenaRecall"/>); in a break
    /// or the countdown "- F: ready (1/3)" after it, "- (ready) 1/3" once this player has pressed F
    /// (<see cref="ArenaReady"/>). It is the game's own event bar, drawn the way the raids draw theirs (<see cref="Raids.RaidHudLine"/>), from the core's ZDO alone; nothing is sent for it. The one
    /// hook on <c>Hud.UpdateEvent</c> asks here first, then the raids (<see cref="Raids.RaidHudPatch"/>). The text is
    /// rebuilt only when a number in it or the player's language changes; a boss's health bar hides it as it hides the
    /// game's own line.
    /// </summary>
    internal static class ArenaHudLine
    {
        private static long _shownKey = -1L;
        private static int _shownExtra = -1;
        private static int _shownLanguage = -1;
        private static bool _shown;
        private static bool _broken;

        /// <summary>Draws the trial's line when the player is in the hall during a trial; true when it did.</summary>
        public static bool Draw(Hud hud, Player player)
        {
            ArenaCore? core = _broken || player == null ? null : ArenaCore.Current;
            ZDO? zdo = core != null ? core.Zdo : null;
            if (zdo == null || !ArenaState.IsRunning(zdo) || !ArenaHall.Contains(player!.transform.position)
                || hud.m_eventBar == null || hud.m_eventName == null
                || (EnemyHud.instance != null && EnemyHud.instance.ShowingBossHud()))
            {
                _shown = false; // the game writes its own text now: write ours again when we come back
                return false;
            }
            if (!hud.m_eventBar.activeSelf)
            {
                hud.m_eventBar.SetActive(true);
            }
            Write(hud, new ArenaState(zdo));
            return true;
        }

        /// <summary>A failure turns the line off for the session, reported once: the game's own line carries on.</summary>
        public static void Fail(Exception e)
        {
            if (!_broken)
            {
                _broken = true;
                Guard.Report(e, "Hel's Trial HUD line");
            }
        }

        private static void Write(Hud hud, ArenaState state)
        {
            ArenaPhase phase = state.Phase;
            int wave = state.Wave;
            long now = NetTime.NowMs();
            int count = phase == ArenaPhase.Fighting ? state.Left : Seconds(state.Next - now);
            long souls = SoulWallets.Souls(state.Zdo, ZDOMan.GetSessionID()); // this player's own
            long key = Key(phase, wave, count, souls);
            int start = state.StartBiome;
            int extra = (phase == ArenaPhase.Fighting ? Recall(state, now) : Ready(state.Zdo)) | (start & 7) << 20;
            if (_shown && key == _shownKey && extra == _shownExtra && _shownLanguage == Lang.Version)
            {
                return;
            }
            _shown = true;
            _shownKey = key;
            _shownExtra = extra;
            _shownLanguage = Lang.Version;
            string biome = ArenaWords.BiomeName(ArenaCreatures.Biome(ArenaTable.BiomeIndex(start, Math.Max(1, wave))));
            string text = Text(phase, biome, wave, count, souls, extra & 0xFFFF);
            hud.m_eventName.text = phase == ArenaPhase.Fighting ? text
                : ArenaWords.HudReady(text, (extra & 0x10000) != 0, extra & 0xFF, extra >> 8 & 0xFF);
        }

        // In a wave, Hel's recall of the stragglers, packed: its seconds left plus one; 0 while none runs.
        private static int Recall(ArenaState state, long now)
        {
            long at = state.RecallAt;
            return at > 0L ? 1 + Mathf.Min(Seconds(at - now), 0xFFFE) : 0;
        }

        // In a rest, who is ready, packed: those ready among the players alive in the arena, how many those are, and
        // whether this player is one of the ready.
        private static int Ready(ZDO zdo)
        {
            List<long> inHall = HallPlayers.PeersNow();
            int ready = Mathf.Min(ArenaReady.Count(zdo, inHall), 255);
            int players = Mathf.Min(inHall.Count, 255);
            bool mine = ArenaReady.Has(zdo, ZDOMan.GetSessionID());
            return ready | players << 8 | (mine ? 0x10000 : 0);
        }

        // The countdown's clock, a break's seconds to the next wave, or a wave's creatures left (and the recall's clock
        // while it runs: its seconds plus one in recall, 0 for none).
        private static string Text(ArenaPhase phase, string biome, int wave, int count, long souls, int recall)
        {
            string points = souls.ToString("N0", CultureInfo.InvariantCulture);
            switch (phase)
            {
                case ArenaPhase.Countdown: return ArenaWords.HudCountdown(biome, Raids.RaidText.Clock(count));
                case ArenaPhase.Break: return ArenaWords.HudBreak(biome, wave, count, points);
                default:
                    return recall > 0 ? ArenaWords.HudRecall(biome, wave, count, Raids.RaidText.Clock(recall - 1), points)
                        : ArenaWords.HudLine(biome, wave, count, points);
            }
        }

        private static int Seconds(long ms) => ms <= 0L ? 0 : (int)((ms + 999L) / 1000L);

        // Every number the line shows, packed into one value so a frame with nothing new costs one comparison.
        private static long Key(ArenaPhase phase, int wave, int count, long souls) =>
            ((long)phase & 3L) | (long)Mathf.Clamp(wave, 0, 4095) << 2 | (long)Mathf.Clamp(count, 0, 1023) << 14
            | Math.Min(Math.Max(souls, 0L), 1L << 38) << 24;
    }
}
