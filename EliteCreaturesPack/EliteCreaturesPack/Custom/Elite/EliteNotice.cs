using System.Collections.Generic;
using System.Linq;
using BepInEx.Bootstrap;
using EliteCreaturesLink;
using EliteCreaturesPack.Core;
using EliteCreaturesPack.Custom.Build;

namespace EliteCreaturesPack.Custom.Elite
{
    /// <summary>
    /// The end of every build of the custom creatures, on every peer (<see cref="BuildPass"/> calls it once the prefabs
    /// are registered). With Elite Creatures Reborn the checked elite lines go to it (<see cref="EliteRegistrar"/>).
    /// Without it - not installed, or too old to have its API - nothing is registered, the creatures work as defined, and
    /// one warning per load names the creatures whose elite lines do nothing (features/custom-creatures.md section 1). A
    /// creature counts when any definition of its chain has elite lines. Nothing else in custom creatures looks for ECR.
    /// </summary>
    internal static class EliteNotice
    {
        public static void AfterBuild(IReadOnlyList<CustomCreature> built)
        {
            EliteStep.EndBuild();
            if (EliteLink.Present)
            {
                EliteRegistrar.Commit(built);
                return;
            }
            List<string> names = built.Where(creature => creature.Chain.Any(definition => definition.HasEliteLines))
                .Select(creature => creature.Definition.Name).ToList();
            if (names.Count > 0)
            {
                Log.Warn($"Elite Creatures Reborn {Absence()}: the elite lines of {names.Count} custom creature(s) do "
                    + $"nothing ({string.Join(", ", names)}). The creatures themselves work as defined.");
            }
        }

        /// <summary>Why the link answers absent: an installed Elite Creatures Reborn without its API is too old.</summary>
        private static string Absence() =>
            Chainloader.PluginInfos.ContainsKey(EliteLink.Guid) ? "is too old to take them (it has no API version 1)" : "is not installed";
    }
}
