using System.Collections.Generic;

namespace HighSeat
{
    /// <summary>
    /// Object ids, each recorded once, in the order they came, while the ids recorded up to the last swap are handed out
    /// from a second list (<see cref="Changes"/>): a swap makes what was recorded the list to hand out and starts recording
    /// afresh, so a change made while a round's ids are handed out waits for the next round instead of being chased within
    /// this one. The game writes one object several times in a row (a move sets position, rotation and velocity), so a
    /// repeat of the last id costs one compare instead of a set lookup. Nothing is allocated once the lists have grown;
    /// after a burst of more than <see cref="BigBurst"/> (a zone loading beside a crowd) the set lets go of its memory.
    /// </summary>
    internal sealed class ChangeSet
    {
        private const int BigBurst = 16384;

        private readonly HashSet<ZDOID> _seen = new HashSet<ZDOID>();
        private List<ZDOID> _recording = new List<ZDOID>();
        private List<ZDOID> _out = new List<ZDOID>();
        private ZDOID _last = ZDOID.None;
        private int _at;

        /// <summary>True while ids from the last swap wait to be handed out.</summary>
        public bool Left => _at < _out.Count;

        public void Add(ZDOID id)
        {
            if (id == _last)
            {
                return;
            }
            _last = id;
            if (_seen.Add(id))
            {
                _recording.Add(id);
            }
        }

        /// <summary>What was recorded becomes the list to hand out, and recording starts afresh (once nothing is left).</summary>
        public void Swap()
        {
            List<ZDOID> spent = _out;
            spent.Clear();
            _out = _recording;
            _recording = spent;
            _at = 0;
            Restart();
        }

        /// <summary>The next id to hand out; false once all are (the list is then emptied).</summary>
        public bool Next(out ZDOID id)
        {
            if (_at < _out.Count)
            {
                id = _out[_at++];
                return true;
            }
            if (_out.Count > 0)
            {
                _out.Clear();
                _at = 0;
            }
            id = ZDOID.None;
            return false;
        }

        /// <summary>Drops what was recorded since the last swap (nobody is on to send it to).</summary>
        public void Forget()
        {
            if (_seen.Count > 0)
            {
                _recording.Clear();
                Restart();
            }
        }

        /// <summary>Drops everything, recorded and left to hand out.</summary>
        public void Clear()
        {
            _recording.Clear();
            Restart();
            _out.Clear();
            _at = 0;
        }

        // The set emptied for a new recording (its memory let go after a burst), and the last id forgotten.
        private void Restart()
        {
            bool burst = _seen.Count > BigBurst;
            _seen.Clear();
            if (burst)
            {
                _seen.TrimExcess();
            }
            _last = ZDOID.None;
        }
    }
}
