using UnityEngine;

namespace EliteCreaturesReborn.Raids
{
    /// <summary>
    /// Holding E on a Raiders Chest during a raid to stop it (features/raids.md, "Stopping": three seconds), on the
    /// holding player's own machine. The game sends a held E as a press and then a repeat every 0.2 seconds while it stays
    /// down, and nothing when it is let go, so a gap longer than the repeats means the key was released and the count
    /// starts again. One stop per hold: the stop goes out once, and a new press is needed for another. The hover shows how
    /// far the hold has come (<see cref="Step"/>).
    /// </summary>
    internal sealed class ChestHold
    {
        /// <summary>How long E is held to stop a raid.</summary>
        public const float Seconds = 3f;

        // Longer than the game's 0.2 s repeat with room for a slow frame, short enough that a let-go shows at once.
        private const float Gap = 0.5f;

        private float _started;
        private float _last = float.NegativeInfinity;
        private bool _spent = true;

        /// <summary>A press (<paramref name="repeat"/> false) or a repeat of E; true the moment it has been held long enough.</summary>
        public bool Held(bool repeat)
        {
            float now = Time.time;
            if (!repeat || now - _last > Gap)
            {
                _started = now;
                _spent = false;
            }
            _last = now;
            if (_spent || now - _started < Seconds)
            {
                return false;
            }
            _spent = true;
            return true;
        }

        /// <summary>How far the hold has come, in tenths (0 to 10); -1 while E is not held on the chest.</summary>
        public int Step
        {
            get
            {
                float now = Time.time;
                if (_spent || now - _last > Gap)
                {
                    return -1;
                }
                return Mathf.Clamp((int)((now - _started) / Seconds * 10f), 0, 10);
            }
        }
    }
}
