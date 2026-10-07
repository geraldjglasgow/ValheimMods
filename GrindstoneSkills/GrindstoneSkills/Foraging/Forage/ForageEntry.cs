namespace GrindstoneSkills
{
    /// <summary>One item of the Forage file: its prefab name and its experience factor.</summary>
    public sealed class ForageEntry
    {
        public ForageEntry(string item, float experience)
        {
            Item = item;
            Experience = experience;
        }

        /// <summary>The item's prefab name, as the file lists it.</summary>
        public string Item { get; }

        /// <summary>The factor on Experience Per Pick, 0 or more.</summary>
        public float Experience { get; }
    }
}
