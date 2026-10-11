using System;
using System.Collections.Generic;
using EliteCreaturesLink;
using EliteCreaturesPack.Custom.Build;

namespace EliteCreaturesPack.Custom.Elite
{
    /// <summary>
    /// The elite step: a creature's `elite:` lines for Elite Creatures Reborn (features/custom-creatures.md section 8).
    /// Each pass of the chain hands its definition's lists to <see cref="EliteLines"/> (a later definition overrides an
    /// earlier one). On the creature's own pass, the last, its other steps have all run, so with ECR there the merged
    /// lines are checked against ECR's names and the finished creature (<see cref="EliteCheck"/>) and queued under the
    /// creature's prefab name, never a base definition's. <see cref="EliteRegistrar"/> hands them to ECR after the build has
    /// registered the prefabs, since ECR checks a portal attack against the registered prefab. Without ECR nothing is
    /// checked or queued, and <see cref="EliteNotice"/> warns once (section 1). Runs on every peer: the server builds from
    /// its files and each client from the server's, so every peer registers the same things.
    /// </summary>
    internal sealed class EliteStep : ICreatureStep
    {
        /// <summary>The lines of each creature whose passes are under way, by prefab name.</summary>
        private static readonly Dictionary<string, EliteLines> merging = new Dictionary<string, EliteLines>(StringComparer.Ordinal);

        public string Name => "elite";

        public void Apply(CreatureBuild build)
        {
            string creature = build.Creature.Name;
            if (build.Chain.Count == 0 || ReferenceEquals(build.Definition, build.Chain[0]))
            {
                // The chain's first pass: whatever a creature of this name queued before is forgotten.
                merging[creature] = new EliteLines();
                EliteRegistrar.Forget(creature);
            }
            if (!merging.TryGetValue(creature, out EliteLines lines))
            {
                return;
            }
            lines.Take(build);
            if (build.IsLeaf)
            {
                merging.Remove(creature);
                Queue(build, lines);
            }
        }

        /// <summary>The build is over: lines of creatures that failed before their own pass are dropped.</summary>
        public static void EndBuild() => merging.Clear();

        private static void Queue(CreatureBuild build, EliteLines lines)
        {
            if (lines.Any && EliteLink.Present)
            {
                EliteRegistrar.Queue(build.Creature.Name, EliteCheck.Checked(build, lines));
            }
        }
    }
}
