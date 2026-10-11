using System.Collections.Generic;
using System.Linq;
using BundlePrefabs;
using EliteCreaturesPack.Core;
using EliteCreaturesPack.Custom.Definitions;
using EliteCreaturesPack.Custom.Elite;
using UnityEngine;

namespace EliteCreaturesPack.Custom.Build
{
    /// <summary>
    /// One build of every custom creature for one world, on one peer, from one set of definitions (the server's own
    /// files, or on a client the files the server built from). In order: the last world's prefabs go; the names and bases
    /// are checked and every chain resolved (<see cref="BaseChains"/>); every shell is made (<see cref="ShellMaker"/>);
    /// the steps run on each (<see cref="StepRunner"/>); a creature that needs one that failed fails too; the rest are
    /// registered with ZNetScene, the failed ones destroyed. Then one summary line, and the ECR notice.
    /// </summary>
    internal static class BuildPass
    {
        public static void Run(ZNetScene scene, IReadOnlyList<CreatureDefinition> definitions, string source)
        {
            CustomPrefabs.Clear();
            PrefabLookup find = new PrefabLookup(scene);
            List<ShellRecord> records = ShellMaker.Make(scene, BaseChains.Resolve(scene, definitions), find);
            foreach (ShellRecord record in records)
            {
                StepRunner.Run(record, find);
            }
            FailWithout(records);
            List<CustomCreature> built = Register(scene, records);
            Combat.PartItems.Register(built);
            Summarize(definitions, built, source);
            EliteNotice.AfterBuild(built);
        }

        /// <summary>Fails every creature that needs a custom creature that is not built, until none is left to fail.</summary>
        private static void FailWithout(List<ShellRecord> records)
        {
            bool changed = true;
            while (changed)
            {
                HashSet<string> standing = new HashSet<string>(records.Where(r => !r.Failed).Select(r => r.Chain.Creature.Name));
                changed = false;
                foreach (ShellRecord record in records.Where(r => !r.Failed))
                {
                    string? missing = record.Needs.FirstOrDefault(name => !standing.Contains(name));
                    if (missing != null)
                    {
                        new BuildReport(record, record.Chain.Creature).Fail($"it needs the custom creature '{missing}', which is not built");
                        changed = true;
                    }
                }
            }
        }

        private static List<CustomCreature> Register(ZNetScene scene, List<ShellRecord> records)
        {
            List<CustomCreature> built = new List<CustomCreature>();
            foreach (ShellRecord record in records)
            {
                if (record.Failed)
                {
                    record.Discard();
                    continue;
                }
                NetPrefabs.Register(scene, record.Shell);
                foreach (KeyValuePair<GameObject, bool> part in record.Parts.Where(part => part.Value && part.Key != null))
                {
                    NetPrefabs.Register(scene, part.Key);
                }
                CustomCreature creature = new CustomCreature(record);
                CustomPrefabs.Add(creature);
                built.Add(creature);
            }
            return built;
        }

        private static void Summarize(IReadOnlyList<CreatureDefinition> definitions, List<CustomCreature> built, string source)
        {
            int off = definitions.Count(definition => !definition.Enabled);
            int leftOut = definitions.Count - off - built.Count;
            string names = built.Count == 0 ? "" : ": " + string.Join(", ", built.Select(creature => creature.Definition.Name));
            Log.Info($"Custom creatures ({source}): {built.Count} built{names}; {off} switched off, {leftOut} left out.");
        }
    }
}
