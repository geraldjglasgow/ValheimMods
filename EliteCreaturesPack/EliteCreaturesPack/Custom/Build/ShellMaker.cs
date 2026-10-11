using System;
using System.Collections.Generic;
using BundlePrefabs;
using EliteCreaturesPack.Core;
using EliteCreaturesPack.Custom.Humans;
using EliteCreaturesPack.Custom.Saves;
using UnityEngine;

namespace EliteCreaturesPack.Custom.Build
{
    /// <summary>
    /// Makes every creature's shell before any step runs, so the steps can name one another: an inactive copy of the
    /// chain's root prefab (<c>PrefabBench.Copy</c>: no Awake, no ZDO) renamed to the creature, or for a human the player's
    /// body from <see cref="HumanBody.Build"/>. Each shell carries a <see cref="CustomTag"/>, which marks its creatures in
    /// the save. A shell that cannot be made leaves its creature out with a message; the others go on.
    /// </summary>
    internal static class ShellMaker
    {
        private const string PlayerPrefab = "Player";

        public static List<ShellRecord> Make(ZNetScene scene, List<CreatureChain> chains, PrefabLookup find)
        {
            List<ShellRecord> records = new List<ShellRecord>(chains.Count);
            foreach (CreatureChain chain in chains)
            {
                ShellRecord? record = MakeOne(scene, chain);
                if (record != null)
                {
                    records.Add(record);
                    find.AddCustom(chain.Creature.Name, record.Shell);
                }
            }
            return records;
        }

        private static ShellRecord? MakeOne(ZNetScene scene, CreatureChain chain)
        {
            try
            {
                GameObject? root = scene.GetPrefab(chain.Human ? PlayerPrefab : chain.RootBase);
                GameObject? shell = chain.Human ? HumanBody.Build(chain.Creature.Name) : PrefabBench.Copy(root!, chain.Creature.Name);
                if (shell == null)
                {
                    return null;
                }
                shell.AddComponent<CustomTag>();
                return new ShellRecord(chain, shell, root);
            }
            catch (Exception e)
            {
                Log.Error($"{BuildReport.Describe(chain.Creature)}: its prefab could not be made ({e.Message}). The creature is left out.");
                return null;
            }
        }
    }
}
