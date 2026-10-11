namespace EliteCreaturesPack.Custom.Build
{
    /// <summary>
    /// One area of a definition put on a creature's prefab: its character, its combat, its look, its human body, its Elite
    /// Creatures Reborn lines. The steps run in <see cref="CreatureSteps.All"/>'s order, once per definition in the
    /// creature's chain (base-most first, the creature's own last; see <see cref="CreatureBuild"/>), on every peer, while
    /// the game sets up its prefabs.
    /// <para>
    /// A step changes only <see cref="CreatureBuild.Shell"/> (and parts it makes through <see cref="CreatureBuild.CopyPart"/>),
    /// never the base prefab or any other prefab of the game. It reports a problem through <see cref="CreatureBuild.Report"/>:
    /// <c>Warn</c> keeps the creature, <c>Fail</c> leaves it out. An unknown prefab, item, effect or status effect named by
    /// the definition is a <c>Fail</c>. A step may throw (the runner catches it and fails the creature), but should not.
    /// Runtime components a step adds decide on the creature's owner and keep their state in its ZDO.
    /// </para>
    /// </summary>
    public interface ICreatureStep
    {
        /// <summary>The step's name in messages: "character", "combat", "look", "human", "elite".</summary>
        string Name { get; }

        /// <summary>Puts <see cref="CreatureBuild.Definition"/>'s part of the definition on <see cref="CreatureBuild.Shell"/>.</summary>
        void Apply(CreatureBuild build);
    }
}
