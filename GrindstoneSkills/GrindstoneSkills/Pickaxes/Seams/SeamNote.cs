using System.Collections.Generic;
using UnityEngine;

namespace GrindstoneSkills
{
    /// <summary>
    /// What one swing did to one rock, on the miner's own client (<see cref="SeamSwing"/>): the chunks it touched, where
    /// it first hit, and the chain link of its clean strike, if it struck the seam.
    /// </summary>
    internal sealed class SeamNote
    {
        public SeamNote(Rock rock, ZDOID id, Vector3 firstPoint)
        {
            Rock = rock;
            Id = id;
            FirstPoint = firstPoint;
        }

        public Rock Rock { get; }

        public ZDOID Id { get; }

        /// <summary>Where the swing first hit this rock: the roll's seam opens near it.</summary>
        public Vector3 FirstPoint { get; }

        /// <summary>Every chunk the swing touched, as area indices; a new seam never lands on one of them.</summary>
        public List<int> Areas { get; } = new List<int>(4);

        /// <summary>The chain link of the swing's clean strike on this rock (1 = the first of a chain); 0 when none.</summary>
        public int Link { get; private set; }

        /// <summary>Where the clean strike hit: the chain's next seam opens near it.</summary>
        public Vector3 StrikePoint { get; private set; }

        public void Strike(int link, Vector3 point)
        {
            Link = link;
            StrikePoint = point;
        }
    }
}
