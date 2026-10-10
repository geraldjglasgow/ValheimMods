using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace DevBridge.Studio
{
    /// <summary>One way to make something: its prefab and the prefabs it takes, the station it is made at among them.</summary>
    internal sealed class TierSource
    {
        internal string Product;
        internal List<string> Inputs;
    }

    /// <summary>
    /// What the game's own data says about where things come from: recipes (ingredients and the crafting station),
    /// stations (their build cost), conversions (smelters, kilns, cooking stations and fermenters, with their fuel) and
    /// creature drops (each creature's spawn biomes, a boss's altar biome).
    /// </summary>
    internal static class TierSources
    {
        internal static List<TierSource> All()
        {
            var sources = new List<TierSource>();
            if (ObjectDB.instance) sources.AddRange(ObjectDB.instance.m_recipes.Where(r => r && r.m_enabled && r.m_item).Select(Recipe));
            foreach (GameObject prefab in ZNetScene.instance.m_prefabs.Where(p => p))
            {
                Station(prefab, sources);
                Conversions(prefab, sources);
            }
            return sources;
        }

        private static TierSource Recipe(Recipe recipe)
        {
            List<string> inputs = Needs(recipe.m_resources);
            if (recipe.m_craftingStation) inputs.Add(Name(recipe.m_craftingStation.gameObject));
            return new TierSource { Product = Name(recipe.m_item.gameObject), Inputs = inputs };
        }

        private static void Station(GameObject prefab, List<TierSource> sources)
        {
            Piece piece = prefab.GetComponent<Piece>();
            bool station = prefab.GetComponent<CraftingStation>() || prefab.GetComponent<Smelter>()
                || prefab.GetComponent<CookingStation>() || prefab.GetComponent<Fermenter>();
            if (piece && station) sources.Add(new TierSource { Product = Name(prefab), Inputs = Needs(piece.m_resources) });
        }

        private static void Conversions(GameObject prefab, List<TierSource> sources)
        {
            string at = Name(prefab);
            if (prefab.TryGetComponent(out Smelter smelter))
                sources.AddRange(smelter.m_conversion.Where(c => c.m_from && c.m_to).Select(c => Convert(c.m_from, c.m_to, at, smelter.m_fuelItem)));
            if (prefab.TryGetComponent(out CookingStation cooking))
                sources.AddRange(cooking.m_conversion.Where(c => c.m_from && c.m_to).Select(c => Convert(c.m_from, c.m_to, at, null)));
            if (prefab.TryGetComponent(out Fermenter fermenter))
                sources.AddRange(fermenter.m_conversion.Where(c => c.m_from && c.m_to).Select(c => Convert(c.m_from, c.m_to, at, null)));
        }

        private static TierSource Convert(ItemDrop from, ItemDrop to, string station, ItemDrop fuel)
        {
            var inputs = new List<string> { Name(from.gameObject), station };
            if (fuel) inputs.Add(Name(fuel.gameObject));
            return new TierSource { Product = Name(to.gameObject), Inputs = inputs };
        }

        private static List<string> Needs(Piece.Requirement[] needs) =>
            (needs ?? new Piece.Requirement[0]).Where(n => n != null && n.m_resItem && n.m_amount > 0).Select(n => Name(n.m_resItem.gameObject)).ToList();

        /// <summary>Each item a creature drops, with the earliest biome that creature spawns in (or its altar's).</summary>
        internal static Dictionary<string, Tier> Drops()
        {
            var found = new Dictionary<string, Tier>();
            SpawnSystem spawns = ZoneSystem.instance && ZoneSystem.instance.m_zoneCtrlPrefab
                ? ZoneSystem.instance.m_zoneCtrlPrefab.GetComponent<SpawnSystem>() : null;
            IEnumerable<SpawnSystem.SpawnData> spawners = spawns ? spawns.m_spawnLists.Where(l => l).SelectMany(l => l.m_spawners) : Enumerable.Empty<SpawnSystem.SpawnData>();
            foreach (SpawnSystem.SpawnData spawn in spawners.Where(s => s != null && s.m_enabled && s.m_prefab))
                Dropped(spawn.m_prefab, TierSeeds.Of(spawn.m_biome), found);
            foreach (KeyValuePair<string, Heightmap.Biome> boss in TierSeeds.Bosses)
                Dropped(ZNetScene.instance.GetPrefab(boss.Key), TierSeeds.Of(boss.Value), found);
            return found;
        }

        private static void Dropped(GameObject creature, Tier? tier, Dictionary<string, Tier> found)
        {
            CharacterDrop drops = creature ? creature.GetComponent<CharacterDrop>() : null;
            if (!drops || tier == null) return;
            foreach (CharacterDrop.Drop drop in drops.m_drops.Where(d => d != null && d.m_prefab))
            {
                string item = Name(drop.m_prefab);
                if (!found.TryGetValue(item, out Tier known) || tier.Value.Rank < known.Rank) found[item] = tier.Value;
            }
        }

        internal static string Name(GameObject prefab) => Utils.GetPrefabName(prefab);
    }
}
