using System.Collections.Generic;

namespace EliteCreaturesReborn.Recap
{
    /// <summary>
    /// One hit the local player took, as it landed: after armour, resistances and the game's damage rates. A hit's fire,
    /// poison and spirit are not in it: the game turns those into burning and poison that tick on their own, and each
    /// tick is a hit of its own here.
    /// </summary>
    public sealed class HitRecord
    {
        /// <summary>Game time (<c>Time.time</c>) the hit landed, the clock the video frames are stamped with.</summary>
        public float Time;

        /// <summary>Who dealt it: a creature's full name with its mutation words and stars, a player's name, or empty.</summary>
        public string Attacker = "";

        /// <summary>How it came: melee, ranged, burning, poison, fall, drowning and the game's other causes.</summary>
        public string Source = "";

        public float Damage;
        public float Health;
        public float MaxHealth;

        /// <summary>The damage by type, largest first, each above a tenth of a point.</summary>
        public List<KeyValuePair<string, float>> Parts = new List<KeyValuePair<string, float>>();
    }
}
