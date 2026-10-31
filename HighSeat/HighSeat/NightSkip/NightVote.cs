using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using HarmonyLib;
using UnityEngine;

namespace HighSeat
{
    /// <summary>
    /// Night skip by vote (Night skip vote). The game's Game.UpdateSleeping, on the server every two seconds, skips the
    /// night only when every player's character is in bed (EverybodyIsTryingToSleep: the character ZDO's inBed). With
    /// the vote, a player votes yes by lying in bed or by saying !sleep in chat within the last minute
    /// (<see cref="SleepWord"/>), and the night is skipped once the yes votes are more than half of the players online:
    /// the players whose character is in the world (the host's own on a hosted game, then every ready peer's), as the
    /// game counts them, so a player still loading or dead and not yet respawned does not count either way. The game's
    /// own conditions stay: afternoon or night, no skip running, and 10 s after the last one ended (Game.m_lastSleepTime).
    /// It then skips as the game does (EnvMan.SkipToMorning, Game.m_sleeping), puts to sleep only the players in bed and
    /// wakes those same players when the morning comes (<see cref="SleepRoute"/>), and tells everyone how the vote
    /// stands (<see cref="VoteNews"/>). Off, or for a skip the game's own rule began, the game runs unchanged; a skip the
    /// vote began is still ended here if the setting is turned off meanwhile. If anything here fails, the game's own
    /// rule takes over (logged once).
    /// </summary>
    [HarmonyPatch(typeof(Game), nameof(Game.UpdateSleeping))]
    internal static class NightVote
    {
        private const double SleepGapSeconds = 10.0;
        private static readonly List<long> InBed = new List<long>();
        private static int _players;
        private static int _yes;
        private static bool _failed;

        private static bool Prefix(Game __instance)
        {
            if (ZNet.instance == null || !ZNet.instance.IsServer() || !Ours(__instance))
            {
                return true;
            }
            try
            {
                Tick(__instance);
                return false;
            }
            catch (Exception e)
            {
                Fail(e);
                return true;
            }
        }

        // A skip the vote began, or no skip running and the vote on. Without a skip running a vote's sleepers are stale
        // and dropped, so a skip the game's own rule begins is always ended by the game, for everyone it put to sleep.
        private static bool Ours(Game game)
        {
            if (game.m_sleeping)
            {
                return SleepRoute.Started;
            }
            if (SleepRoute.Started)
            {
                SleepRoute.Forget();
            }
            return Settings.NightSkipVote.Value;
        }

        private static void Tick(Game game)
        {
            SleepWord.Forget(Time.unscaledTime);
            if (game.m_sleeping)
            {
                Wake(game);
                return;
            }
            if (!CanSkip(game))
            {
                VoteNews.Quiet();
                return;
            }
            Count();
            if (_yes * 2 > _players)
            {
                Skip(game);
                return;
            }
            VoteNews.Progress(_yes, _players);
        }

        // The game's own end of a skip: once the clock has reached the morning and no dream cutscene plays on the host.
        private static void Wake(Game game)
        {
            if (EnvMan.instance.IsTimeSkipping() || CutscenePlaying())
            {
                return;
            }
            game.m_lastSleepTime = ZNet.instance.GetTimeSeconds();
            game.m_sleeping = false;
            SleepRoute.End();
        }

        // The game's own conditions for a skip: afternoon or night, none running, 10 s since the last one ended.
        private static bool CanSkip(Game game)
        {
            return !EnvMan.instance.IsTimeSkipping() && (EnvMan.IsAfternoon() || EnvMan.IsNight())
                && ZNet.instance.GetTimeSeconds() - game.m_lastSleepTime >= SleepGapSeconds;
        }

        // A dedicated server plays no cutscene, and its own build may have no cutscene player to ask: the game class is
        // only touched (and compiled against) on a hosted game.
        private static bool CutscenePlaying() => !ZNet.instance.IsDedicated() && HostCutscene();

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static bool HostCutscene() => CinematicsManager.IsPlaying();

        private static void Count()
        {
            InBed.Clear();
            _players = 0;
            _yes = 0;
            float now = Time.unscaledTime;
            ZNet net = ZNet.instance;
            Add(ZDOMan.GetSessionID(), net.LocalPlayerCharacterID, now);
            foreach (ZNetPeer peer in net.GetPeers())
            {
                if (peer.IsReady())
                {
                    Add(peer.m_uid, peer.m_characterID, now);
                }
            }
        }

        // One player whose character is in the world; none for the host's own on a dedicated server.
        private static void Add(long uid, ZDOID character, float now)
        {
            ZDO zdo = ZDOMan.instance.GetZDO(character);
            if (zdo == null)
            {
                return;
            }
            _players++;
            bool inBed = zdo.GetBool(ZDOVars.s_inBed);
            if (inBed)
            {
                InBed.Add(uid);
            }
            if (inBed || SleepWord.SaidWithin(uid, now))
            {
                _yes++;
            }
        }

        private static void Skip(Game game)
        {
            bool everyoneInBed = InBed.Count == _players;
            EnvMan.instance.SkipToMorning();
            game.m_sleeping = true;
            SleepRoute.Begin(InBed);
            SleepWord.Clear();
            VoteNews.Passed(_yes, _players, everyoneInBed);
        }

        private static void Fail(Exception e)
        {
            SleepRoute.Forget();
            if (!_failed)
            {
                _failed = true;
                HighSeat.Log.LogWarning($"night skip vote failed, the game's own rule runs: {e}");
            }
        }
    }
}
