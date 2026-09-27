namespace GrindstoneSkills
{
    /// <summary>One item of the Forage file: whether its picks roll stars, when its plant is at its best, and its experience factor.</summary>
    public sealed class ForageEntry
    {
        public ForageEntry(string item, bool stars, BestTime best, float experience)
        {
            Item = item;
            Stars = stars;
            Best = best;
            Experience = experience;
        }

        /// <summary>The item's prefab name, as the file lists it.</summary>
        public string Item { get; }

        public bool Stars { get; }

        public BestTime Best { get; }

        /// <summary>The factor on Experience Per Pick, 0 or more.</summary>
        public float Experience { get; }
    }
}
