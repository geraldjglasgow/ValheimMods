using System;
using EliteCreaturesReborn.Util;
using PatchGuard;
using UnityEngine;

namespace EliteCreaturesReborn.Raids
{
    /// <summary>
    /// The raid's line under the minimap while a player is within its 96 m (features/raids.md section 4.5): "Fuling raid
    /// (Brutal) - wave 2 of 3 - 5 left - 8:40". It is the game's own event line - the bar the game shows its raids in,
    /// in the game's look - so the mod adds no HUD element of its own. Each client draws it from the host's ZDO alone;
    /// nothing is sent for it. The nearest running host comes from <see cref="RaidHosts"/> (never a scene search) and
    /// the text is rebuilt only when a number in it changes, about once a second; the game's own event takes the line
    /// back the moment the player leaves the raid, and a boss's health bar hides it as it hides the game's.
    /// </summary>
    internal static class RaidHudLine
    {
        private static RaidRunner? _shown;
        private static string? _shownName;
        private static long _shownKey = -1L;
        private static bool _broken;

        /// <summary>Draws the raid's line when the player is at a raid; true when it did (the game's line is skipped).</summary>
        public static bool Draw(Hud hud, Player player)
        {
            RaidRunner? host = _broken || player == null ? null
                : RaidHosts.NearestRunning(player.transform.position, RaidTable.RaidRadius);
            if (host == null || hud.m_eventBar == null || hud.m_eventName == null
                || (EnemyHud.instance != null && EnemyHud.instance.ShowingBossHud()))
            {
                _shown = null; // the game writes its own text now: write ours again when we come back
                return false;
            }
            if (!hud.m_eventBar.activeSelf)
            {
                hud.m_eventBar.SetActive(true);
            }
            Write(hud, host);
            return true;
        }

        /// <summary>A failure turns the line off for the session, reported once: the game's own line carries on.</summary>
        public static void Fail(Exception e)
        {
            if (!_broken)
            {
                _broken = true;
                Guard.Report(e, "raid HUD line");
            }
        }

        private static void Write(Hud hud, RaidRunner host)
        {
            RaidState state = host.State;
            long now = NetTime.NowMs();
            int secondsLeft = Seconds(state.Deadline - now), secondsToNext = Seconds(state.PhaseUntil - now);
            RaidPhase phase = state.Phase;
            int wave = state.Wave, left = state.Left, band = state.Band;
            string name = state.DisplayName;
            long key = Key(phase, wave, left, band, secondsLeft, secondsToNext);
            if (ReferenceEquals(host, _shown) && ReferenceEquals(name, _shownName) && key == _shownKey)
            {
                return;
            }
            _shown = host;
            _shownName = name;
            _shownKey = key;
            hud.m_eventName.text = RaidText.Line(name, RaidTable.Band(band), phase, wave, left, secondsLeft, secondsToNext);
        }

        private static int Seconds(long ms) => ms <= 0L ? 0 : (int)((ms + 999L) / 1000L);

        // Every number the line shows, packed into one value so a frame with nothing new costs one comparison.
        private static long Key(RaidPhase phase, int wave, int left, int band, int secondsLeft, int secondsToNext) =>
            (long)phase | (long)Mathf.Clamp(wave, 0, 15) << 4 | (long)Mathf.Clamp(left, 0, 1023) << 8
            | (long)Mathf.Clamp(band, 0, 15) << 18 | (long)Mathf.Clamp(secondsLeft, 0, 4095) << 22
            | (long)Mathf.Clamp(secondsToNext, 0, 4095) << 34;
    }
}
