using System.Collections.Generic;
using EliteCreaturesPack.Custom.Nature;
using EliteCreaturesPack.Custom.Combat;
using EliteCreaturesPack.Custom.Elite;
using EliteCreaturesPack.Custom.Humans;
using EliteCreaturesPack.Custom.Look;

namespace EliteCreaturesPack.Custom.Build
{
    /// <summary>
    /// The steps in the order they run on each definition of a chain. The human body comes first (it is dressed before
    /// anything is put on it), then the character (what it is), its combat (what it carries and hits with), its look (how
    /// it is drawn, items included) and last its Elite Creatures Reborn lines (which may name its finished attacks). Each
    /// folder owns one step and may split it into as many classes as it likes behind it.
    /// </summary>
    internal static class CreatureSteps
    {
        public static readonly IReadOnlyList<ICreatureStep> All = new ICreatureStep[]
        {
            new HumanStep(),
            new CharacterStep(),
            new CombatStep(),
            new LookStep(),
            new EliteStep(),
        };
    }
}
