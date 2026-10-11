using System;
using EliteCreaturesPack.Custom.Definitions;
using PatchGuard;

namespace EliteCreaturesPack.Custom.Build
{
    /// <summary>
    /// Runs the steps on one creature: for each definition of its chain, base-most first, every step in
    /// <see cref="CreatureSteps.All"/>'s order. A step that throws fails the creature (the exception is logged in full)
    /// and nothing escapes: this runs while the game sets up its prefabs, where a throw leaves the world loading for ever.
    /// Once a creature has failed, its remaining steps are skipped.
    /// </summary>
    internal static class StepRunner
    {
        public static void Run(ShellRecord record, PrefabLookup find)
        {
            foreach (CreatureDefinition link in record.Chain.Links)
            {
                CreatureBuild build = new CreatureBuild(record, link, find);
                foreach (ICreatureStep step in CreatureSteps.All)
                {
                    if (record.Failed)
                    {
                        return;
                    }
                    Apply(step, build);
                }
            }
        }

        private static void Apply(ICreatureStep step, CreatureBuild build)
        {
            try
            {
                step.Apply(build);
            }
            catch (Exception e)
            {
                Guard.Report(e, $"custom creature '{build.Creature.Name}', {step.Name} step");
                build.Report.Fail($"the {step.Name} step failed: {e.Message}");
            }
        }
    }
}
