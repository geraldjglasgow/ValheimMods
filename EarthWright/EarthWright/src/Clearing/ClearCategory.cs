using System;

namespace EarthWright.Clearing
{
    /// <summary>
    /// The kinds of world object clearing can take away, as flags so one request (the Clear entry, a console command)
    /// can name several. <see cref="ObjectKinds"/> puts every object into exactly one kind, or none when clearing must
    /// leave it alone (buildings, creature nests, containers, ...).
    /// </summary>
    [Flags]
    public enum ClearCategory
    {
        None = 0,
        Trees = 1,
        Stumps = 2,
        Logs = 4,
        /// <summary>Bushes and shrubs.</summary>
        Shrubs = 8,
        Rocks = 16,
        /// <summary>Rocks and veins whose drops include ore; only cleared where the settings allow it.</summary>
        Ores = 32,
        /// <summary>Natural pickables other than loose debris: berries, mushrooms, flowers, ...</summary>
        Pickables = 64,
        /// <summary>Loose stones, branches and flint lying on the ground (pickables too).</summary>
        Debris = 128,
    }

    /// <summary>How the Clear and Groundbreaker entries (and the clearing commands) take objects away.</summary>
    public enum ClearingMode
    {
        /// <summary>Objects vanish; nothing drops and no tool is needed.</summary>
        Remove = 0,
        /// <summary>Wood needs an axe and stone a pickaxe of a high enough tier in the inventory; wood and stone drop when "Survival Drops" is on.</summary>
        Survival = 1,
    }
}
