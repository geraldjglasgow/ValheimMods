using EliteCreaturesReborn.Rules;
using EliteCreaturesReborn.Traits;
using EliteCreaturesReborn.Util;
using UnityEngine;

namespace EliteCreaturesReborn.Mutations
{
    /// <summary>
    /// Splintering's split. When a Splintering creature dies it breaks into two copies of its own prefab, each rolling
    /// its own star count from the fixed reduction table and keeping the parent's other mutations and biome. A copy
    /// landing on 0 stars drops Splintering and terminates its branch. The two optional caps - read from the rules,
    /// 0 meaning off - truncate a cascade here rather than preventing the mutation. Runs on the dying creature's owner
    /// (the death patch is owner-gated). It takes its inputs as a snapshot from <see cref="Patches.DeathPatch"/> rather
    /// than the parent's ZDO, because by the time OnDeath's postfix runs the parent's ZDO has been reset to null - the
    /// very fault that made the old build silently never split. It Instantiates the copies here, so they are owned by
    /// this machine and replicate like any spawn, and writes each copy's traits to its ZDO before the copy's own
    /// controller wakes - so the copy is born already-resolved and its owner never rolls it. Every client then reads it.
    /// </summary>
    public static class Splitter
    {
        public static void Split(Vector3 pos, Quaternion rot, int prefabHash, CreatureTraits parentTraits,
            BiomeRules rules, int generation, string root, Heightmap.Biome biome)
        {
            GameObject? prefab = ZNetScene.instance != null ? ZNetScene.instance.GetPrefab(prefabHash) : null;
            if (prefab == null)
            {
                Log.Diag($"split aborted: no prefab for hash {prefabHash} (scene missing or prefab unknown)");
                return;
            }
            int nextGen = generation + 1;
            if (Truncated(rules, nextGen, root))
            {
                Log.Diag($"split truncated at gen {nextGen}, root {root} (a cascade cap is on)");
                return;
            }
            SpawnCopy(prefab, pos, rot, parentTraits, nextGen, root, biome);
            SpawnCopy(prefab, pos, rot, parentTraits, nextGen, root, biome);
        }

        private static bool Truncated(BiomeRules rules, int nextGen, string root)
        {
            int maxGen = (int)rules.PowerOf(Mutation.Splintering, Fields.MaxGenerations);
            int maxDesc = (int)rules.PowerOf(Mutation.Splintering, Fields.MaxDescendants);
            if (maxGen > 0 && nextGen > maxGen)
            {
                return true;
            }
            return maxDesc > 0 && DescendantRegistry.Count(root) >= maxDesc;
        }

        private static void SpawnCopy(GameObject prefab, Vector3 parentPos, Quaternion rot,
            CreatureTraits parentTraits, int gen, string root, Heightmap.Biome biome)
        {
            int stars = SplinterTable.RollChildStars(parentTraits.Stars);
            int mask = stars == 0 ? parentTraits.Mask & ~(1 << (int)Mutation.Splintering) : parentTraits.Mask;
            Vector3 pos = parentPos + Random.insideUnitSphere * 0.5f;
            pos.y = parentPos.y;
            GameObject copy = Object.Instantiate(prefab, pos, rot);
            Configure(copy, new CreatureTraits(stars, mask), gen, root, biome);
        }

        private static void Configure(GameObject copy, CreatureTraits traits, int gen, string root, Heightmap.Biome biome)
        {
            ZNetView nview = copy.GetComponent<ZNetView>();
            Character character = copy.GetComponent<Character>();
            if (nview == null || character == null || nview.GetZDO() == null)
            {
                Log.Diag("split copy has no valid ZNetView/ZDO after Instantiate - the copy will roll fresh, not inherit");
                return;
            }
            ZDO zdo = nview.GetZDO();
            TraitStore.Save(zdo, traits);
            TraitStore.SetBiome(zdo, biome);
            zdo.Set(TraitKeys.Generation, gen);
            zdo.Set(TraitKeys.CascadeRoot, root);
            character.SetLevel(1);
            DescendantRegistry.Register(root);
            Log.Diag($"split copy spawned: {copy.name} stars={traits.Stars} mask={traits.Mask} gen={gen} splinters={traits.Has(Mutation.Splintering)}");
        }
    }
}
