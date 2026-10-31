using UnityEngine;

namespace HighSeat
{
    /// <summary>
    /// What players are told about the night vote (<see cref="NightVote"/>): a top-left line to everyone through the
    /// game's routed RPC "ShowMessage" (int MessageHud.MessageType, string text), which every player's game shows with
    /// nothing installed (on a hosted game the host's own HUD shows it too; a dedicated server has none). While a vote
    /// is open its count is told when it changes, at most every <see cref="MinGapSeconds"/> seconds (the game shows
    /// top-left lines one after another, so a burst would queue up); nothing while nobody has voted. When the vote
    /// passes everyone is told, unless every player was in bed: then it is the game's own night skip, which says nothing.
    /// The words are English: the server cannot know each player's language.
    /// </summary>
    internal static class VoteNews
    {
        private const float MinGapSeconds = 5f;
        private static int _told;
        private static float _toldAt = float.NegativeInfinity;

        /// <summary>A vote is open with <paramref name="yes"/> of <paramref name="players"/> voting for it.</summary>
        public static void Progress(int yes, int players)
        {
            if (yes == 0)
            {
                _told = 0;
                return;
            }
            float now = Time.unscaledTime;
            if (yes == _told || now - _toldAt < MinGapSeconds)
            {
                return;
            }
            _told = yes;
            _toldAt = now;
            int needed = players / 2 + 1;
            Tell($"Skip the night: {yes} of {players} votes ({needed} needed). Lie in bed or say {SleepWord.Word}.");
        }

        /// <summary>The vote passed and the night is being skipped.</summary>
        public static void Passed(int yes, int players, bool everyoneInBed)
        {
            _told = 0;
            if (!everyoneInBed)
            {
                Tell($"The night is skipped ({yes} of {players} voted).");
            }
        }

        /// <summary>No vote can be open (day, or a skip running): the next one starts untold.</summary>
        public static void Quiet() => _told = 0;

        private static void Tell(string text)
        {
            ZRoutedRpc.instance.InvokeRoutedRPC(ZRoutedRpc.Everybody, "ShowMessage", (int)MessageHud.MessageType.TopLeft, text);
        }
    }
}
