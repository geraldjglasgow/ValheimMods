namespace GrindstoneSkills
{
    /// <summary>
    /// A tamed animal dying, on its owner (<see cref="Butchering"/>): read once when Character.OnDeath starts, before
    /// the game generates the drop list and destroys the creature.
    /// </summary>
    public sealed class ButcherContext
    {
        public Character Creature { get; set; }

        /// <summary>The creature's prefab name, for example "Boar".</summary>
        public string Prefab { get; set; }

        /// <summary>The player whose hit killed it (the game's last hit on the owner); null for any other death.</summary>
        public Player Killer { get; set; }

        /// <summary>The killer's Husbandry level (from the killer's ZDO when the owner is another machine); 0 without a killer.</summary>
        public float KillerLevel { get; set; }
    }
}
