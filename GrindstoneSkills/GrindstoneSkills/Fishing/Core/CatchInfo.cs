using UnityEngine;

namespace GrindstoneSkills
{
    /// <summary>
    /// A fish being landed, captured before FishingFloat.Catch picks it up (the pickup destroys the fish's object): who
    /// caught it, what it is, how big, and its weight on the scale (<see cref="CatchWeight"/>). Features fill in what
    /// they found (a new record) and what they changed (the bonus-item table, put back afterwards).
    /// </summary>
    public sealed class CatchInfo
    {
        public CatchInfo(Player player, Fish fish, FishingFloat fishingFloat)
        {
            Player = player;
            Fish = fish;
            Float = fishingFloat;
            Prefab = FishInfo.Prefab(fish);
            Name = FishInfo.Name(fish);
            NameToken = fish.m_name;
            Level = FishInfo.Level(fish);
            SpeciesScale = FishInfo.SpeciesScale(fish);
            Position = fish.transform.position;
            ItemDrop item = FishInfo.Item(fish);
            Weight = item == null ? 0f : CatchWeight.Roll(item.m_itemData.m_shared, Level);
        }

        public Player Player { get; }
        public Fish Fish { get; }

        /// <summary>The float that landed it, or null when the catch came from outside a float's step.</summary>
        public FishingFloat Float { get; }

        public string Prefab { get; }
        public string Name { get; }

        /// <summary>The fish's name as the game keeps it (Fish.m_name, a word token), for a message read on another machine.</summary>
        public string NameToken { get; }

        public int Level { get; }
        public float SpeciesScale { get; }
        public float Weight { get; }
        public Vector3 Position { get; }

        public bool Legendary => FishInfo.IsLegendary(Level);

        /// <summary>The catch beat the angler's record for the species (not set for the first catch of one).</summary>
        public bool NewRecord { get; set; }

        // The fish's bonus-item table as it was, while BonusItems has raised it.
        internal DropTable BonusTable;
        internal float BonusChance;
        internal int BonusMax;
    }
}
