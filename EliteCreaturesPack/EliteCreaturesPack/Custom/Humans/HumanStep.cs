using System.Collections.Generic;
using System.Linq;
using EliteCreaturesPack.Custom.Build;
using EliteCreaturesPack.Custom.Definitions;

namespace EliteCreaturesPack.Custom.Humans
{
    /// <summary>
    /// The human base, the first step of every pass. For a creature whose chain's root is <c>Human</c> the shell is
    /// already the player's body with the human base's look ranges (<see cref="HumanBody.Build"/>, made by the pipeline);
    /// this step:
    /// <list type="bullet">
    /// <item>lays each definition's <c>human:</c> ranges on it (<see cref="HumanBody.Dress"/>), base-most first, so a later
    /// definition overrides what it names and keeps the rest. A hair or beard that is neither "none" nor a Customization
    /// item of that kind (the game's, or a mod's) fails the creature, the message listing the game's own.</item>
    /// <item>in the chain's first pass, before any combat step has run, takes the bare kit (club and rags) off when any
    /// definition of the chain gives gear (<c>gear:</c> with items, or <c>replace</c>): a human carries the kit only
    /// while nothing in its chain says what it carries. New attacks alone leave the kit on: they join the club.</item>
    /// </list>
    /// A <c>human:</c> block on a creature that is not a human fails it.
    /// </summary>
    internal sealed class HumanStep : ICreatureStep
    {
        public string Name => "human";

        public void Apply(CreatureBuild build)
        {
            if (!build.IsHuman)
            {
                if (build.Definition.Human != null)
                {
                    build.Report.Fail("a human: block needs a creature whose base is Human (or comes down from one)", "human");
                }
                return;
            }
            if (ReferenceEquals(build.Definition, build.Chain[0]) && build.Chain.Any(GivesGear))
            {
                HumanShell.Unkit(build.Shell);
            }
            HumanLook? look = build.Definition.Human;
            if (look != null && Known(build, look.Hair, HumanHairs.HairKind, "human.hair")
                && Known(build, look.Beard, HumanHairs.BeardKind, "human.beard"))
            {
                HumanBody.Dress(build.Shell, look);
            }
        }

        /// <summary>Whether every name is "none" or a hair (beard) item; otherwise the creature fails, naming the field.</summary>
        private static bool Known(CreatureBuild build, List<string>? names, string kind, string field)
        {
            List<string> unknown = names == null ? new List<string>() : HumanHairs.Unknown(names, kind);
            if (unknown.Count == 0)
            {
                return true;
            }
            string which = string.Join(", ", unknown.Select(name => $"'{name}'"));
            string noun = kind.ToLowerInvariant();
            build.Report.Fail($"{which}: not a {noun} of the game or any mod. The game's own: {HumanHairs.Choices(kind)}", field);
            return false;
        }

        /// <summary>Whether the definition says what the creature carries (any item, or the base's gear thrown away).</summary>
        private static bool GivesGear(CreatureDefinition definition)
        {
            GearBlock? gear = definition.Gear;
            return gear != null && (gear.Replace || gear.Always.Count > 0
                || gear.PickOneFrom.Any(list => list.Count > 0) || gear.OneSetFrom.Any(set => set.Count > 0));
        }
    }
}
