using System.Collections.Generic;

namespace HighSeat
{
    /// <summary>
    /// The objects one player's update rate holds back (<see cref="Throttle"/>), each with the round the player last got
    /// it in, its rate then (once in so many rounds) and its squared distance then. Until its round comes the object,
    /// still in the player's pending set, costs their send one lookup here instead of a look-up in the game, a check
    /// against what the player has and a new distance; it counts for the crowd ranks at the distance it was held at
    /// (<see cref="CrowdRank"/>), and if those ranks give that distance a faster rate than it was held at (another object
    /// went further off, or left), it is looked at in full again as soon as the faster rate lets it go, as it would have
    /// been without the hold. So it goes in the round the update rate sends it in, rated at the distance it was held at
    /// (a few rounds old at most). A hold goes when its round comes, when the object's owner changes or it moves to another
    /// sector (handed out first by <see cref="Changes"/>: an owner change, and an object that may have become new to the
    /// player, never wait), when the pending set is set aside (<see cref="Backlog"/>) and when the player leaves. New
    /// objects and force-sent ones are never held. Holds left behind by objects that left the pending set without a look
    /// here (sent as force-sent) all go once there are more than <see cref="Strays"/> of them, which only means a full look
    /// at each again. Used by the thread that prepares this player's send (<see cref="SendPrep"/>) and by the main thread
    /// between sends, never by both at once.
    /// </summary>
    internal sealed class Holds
    {
        private const int Strays = 64;

        /// <summary>One held object: the round the player last got it in, its rate then, its squared distance then.</summary>
        public struct Hold
        {
            public int Sent;
            public int Every;
            public float Distance2;

            /// <summary>The first round it may go in.</summary>
            public int From => Sent + Every;
        }

        private readonly Dictionary<ZDOID, Hold> _held = new Dictionary<ZDOID, Hold>();

        /// <summary>True while the object waits in this round; a hold whose round has come goes (look at it in full).</summary>
        public bool Waiting(ZDOID id, int round, out Hold hold)
        {
            if (_held.Count == 0 || !_held.TryGetValue(id, out hold))
            {
                hold = default;
                return false;
            }
            if (hold.From > round)
            {
                return true;
            }
            _held.Remove(id);
            return false;
        }

        public void Add(ZDOID id, Hold hold)
        {
            _held[id] = hold;
        }

        /// <summary>The object may go at once (its owner changed, it may be new to the player, or its rate got faster).</summary>
        public void Release(ZDOID id)
        {
            if (_held.Count > 0)
            {
                _held.Remove(id);
            }
        }

        /// <summary>After a send's walk, which kept <paramref name="live"/> holds: lets all go when too many were not met.</summary>
        public void Prune(int live)
        {
            if (_held.Count - live > Strays)
            {
                _held.Clear();
            }
        }

        public void Clear()
        {
            if (_held.Count > 0)
            {
                _held.Clear();
            }
        }
    }
}
