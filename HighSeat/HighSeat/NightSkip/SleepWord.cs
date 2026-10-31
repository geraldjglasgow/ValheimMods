using System;
using System.Collections.Generic;
using UnityEngine;

namespace HighSeat
{
    /// <summary>
    /// Who said !sleep in chat, and when (real time, so the racing clock of a night skip does not age a vote): the whole
    /// message, trimmed, in any case, counts as a vote for <see cref="VoteSeconds"/> seconds. Kept by sender id (the
    /// player's peer id, the host's session id), so the copies of one message the server sees (the game sends a chat
    /// message to each player separately: <see cref="PeerChat"/>) count once. Messages are heard by
    /// <see cref="PeerChat"/> (players' chat passing through the server) and <see cref="HostChat"/> (the host's own);
    /// <see cref="NightVote"/> counts them. A message longer than <see cref="MaxCallBytes"/> (its whole parameter block:
    /// position, talk type, name, platform id, text) cannot be a !sleep and is not read.
    /// </summary>
    internal static class SleepWord
    {
        public const string Word = "!sleep";
        private const float VoteSeconds = 60f;
        private const int MaxCallBytes = 256;
        private static readonly Dictionary<long, float> Said = new Dictionary<long, float>();
        private static readonly List<long> Expired = new List<long>();

        /// <summary>True while <paramref name="sender"/>'s last !sleep is a vote.</summary>
        public static bool SaidWithin(long sender, float now) => Said.TryGetValue(sender, out float at) && now - at <= VoteSeconds;

        public static void Clear() => Said.Clear();

        /// <summary>Records a vote when <paramref name="text"/> is the word.</summary>
        public static void Heard(long sender, string text)
        {
            if (text != null && string.Equals(text.Trim(), Word, StringComparison.OrdinalIgnoreCase))
            {
                Said[sender] = Time.unscaledTime;
            }
        }

        /// <summary>
        /// The text of a chat package: "ChatMessage" (Vector3 position, int type, UserInfo, string text) when
        /// <paramref name="positioned"/>, "Say" (int type, UserInfo, string text) otherwise, UserInfo being two strings
        /// (name, platform id). Read from a copy, so the game's own package is never moved; null when it is too long.
        /// </summary>
        public static string ReadText(ZPackage parameters, bool positioned)
        {
            if (parameters.Size() > MaxCallBytes)
            {
                return null;
            }
            ZPackage copy = new ZPackage(parameters.GetArray());
            if (positioned)
            {
                copy.ReadVector3();
            }
            copy.ReadInt();
            copy.ReadString();
            copy.ReadString();
            return copy.ReadString();
        }

        /// <summary>Drops votes older than <see cref="VoteSeconds"/>.</summary>
        public static void Forget(float now)
        {
            if (Said.Count == 0)
            {
                return;
            }
            Expired.Clear();
            foreach (KeyValuePair<long, float> said in Said)
            {
                if (now - said.Value > VoteSeconds)
                {
                    Expired.Add(said.Key);
                }
            }
            foreach (long sender in Expired)
            {
                Said.Remove(sender);
            }
        }
    }
}
