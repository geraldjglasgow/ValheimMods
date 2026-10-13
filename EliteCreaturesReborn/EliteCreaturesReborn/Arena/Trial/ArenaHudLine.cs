using System;
using System.Globalization;
using EliteCreaturesReborn.Util;
using PatchGuard;
using UnityEngine;

namespace EliteCreaturesReborn.Arena.Trial
{
    /// <summary>
    /// The trial's line under the minimap for every living player in the hall while it runs (features/arena.md section 3,
    /// "Score"): "Hel's Trial - wave 12 - 7 left - 3,420", or "Hel's Trial - the first wave in 0:07" in the countdown. It
    /// is the game's own event bar, drawn the way the raids draw theirs (<see cref="Raids.RaidHudLine"/>), from the core's
    /// ZDO alone; nothing is sent for it. The one hook on <c>Hud.UpdateEvent</c> asks here first, then the raids
    /// (<see cref="Raids.RaidHudPatch"/>). The text is rebuilt only when a number in it changes; a boss's health bar hides
    /// it as it hides the game's own line.
    /// </summary>
    internal static class ArenaHudLine
    {
        private static long _shownKey = -1L;
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
            bool countdown = state.Phase == ArenaPhase.Countdown;
            int seconds = Seconds(state.Next - NetTime.NowMs());
            int wave = state.Wave, left = state.Left;
            long score = state.Score;
            long key = countdown ? -2L - seconds : Key(wave, left, score);
            if (_shown && key == _shownKey)
            {
                return;
            }
            _shown = true;
            _shownKey = key;
            hud.m_eventName.text = countdown
                ? ArenaWords.HudCountdown(Raids.RaidText.Clock(seconds))
                : ArenaWords.HudLine(wave, left, score.ToString("N0", CultureInfo.InvariantCulture));
        }

        private static int Seconds(long ms) => ms <= 0L ? 0 : (int)((ms + 999L) / 1000L);

        // Every number the line shows, packed into one value so a frame with nothing new costs one comparison.
        private static long Key(int wave, int left, long score) =>
            (long)Mathf.Clamp(wave, 0, 4095) | (long)Mathf.Clamp(left, 0, 1023) << 12 | Math.Min(score, 1L << 40) << 22;
    }
}
