using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// The biome tiers the ship cannons and their ammunition are graded by, in the order the world is met: Meadows 1 to
    /// Ashlands 7. An item is made at its tier's crafting station unless it names its own (<see cref="Station"/>). Only
    /// the tiers up to <see cref="Offered"/> are offered for now (<see cref="Shown"/>).
    /// </summary>
    public static class KwTiers
    {
        public const int Meadows = 1, BlackForest = 2, Swamp = 3, Mountains = 4, Plains = 5, Mistlands = 6, Ashlands = 7;

        /// <summary>
        /// The highest tier offered for now (the user, 2026-10-26: "right now we should only be supporting the Meadows and
        /// Black Forest ... We should not have any of the higher tier stuff yet"; raised to the Swamp on 2026-10-28 to release
        /// the small iron ship cannon and the iron cannonball). A cannon of a higher tier stays out of the
        /// hammer (<see cref="KwCannonHammer"/>) and an item of one has no recipe (<see cref="KwItems"/>), but both stay
        /// registered with the game, so the cannons and items a world already has still load. A later release raises it.
        /// </summary>
        public const int Offered = Swamp;

        /// <summary>Whether a cannon or an item of the tier is offered now: in the hammer, or crafted.</summary>
        public static bool Shown(int tier) => tier <= Offered;

        // Each tier's station and level: the workbench, the forge as it grows, the black forge.
        private static readonly (string Station, int Level)[] Stations =
        {
            ("piece_workbench", 1), ("piece_workbench", 1), ("forge", 1), ("forge", 2), ("forge", 3), ("forge", 4), ("blackforge", 1), ("blackforge", 2),
        };

        /// <summary>The crafting station prefab and level an item of the tier is made at.</summary>
        public static (string Station, int Level) Station(int tier) => Stations[Mathf.Clamp(tier, Meadows, Ashlands)];
    }
}
