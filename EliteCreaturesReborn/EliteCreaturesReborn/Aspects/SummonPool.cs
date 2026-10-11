using System.Collections.Generic;
using EliteCreaturesReborn.Rules;
using EliteCreaturesReborn.Runtime;
using EliteCreaturesReborn.Traits;

namespace EliteCreaturesReborn.Aspects
{
    /// <summary>
    /// What a Summoner calls, in order of precedence: the list another mod registered for its prefab
    /// (<see cref="Registrations.SummonsOf"/>), with the stars it names per creature; the rule file's `summons` for its
    /// prefab under `per boss`; and for a creature the file names nothing for - another mod's boss, a creature given
    /// Summoner through the API or `elite spawn` - the creatures of its own biome: the rule file's list for the game's
    /// boss of that biome, so an admin's edit to that list reaches it too. Its biome is the one it was rolled in, or where
    /// it stands when it has none. The Ocean and the Deep North have no boss of their own, so a Summoner rolled there with
    /// no list calls nothing. Read on the owner, once per wave.
    /// </summary>
    internal static class SummonPool
    {
        private static readonly Dictionary<Heightmap.Biome, string> BiomeBoss = new Dictionary<Heightmap.Biome, string>
        {
            [Heightmap.Biome.Meadows] = "Eikthyr",
            [Heightmap.Biome.BlackForest] = "gd_king",
            [Heightmap.Biome.Swamp] = "Bonemass",
            [Heightmap.Biome.Mountain] = "Dragon",
            [Heightmap.Biome.Plains] = "GoblinKing",
            [Heightmap.Biome.Mistlands] = "SeekerQueen",
            [Heightmap.Biome.AshLands] = "Fader",
        };

        public static List<SummonPick> For(EliteController summoner)
        {
            string prefab = Utils.GetPrefabName(summoner.gameObject);
            SummonPick[]? own = Registrations.SummonsOf(prefab);
            if (own != null)
            {
                return new List<SummonPick>(own);
            }
            AspectRules rules = RuleState.Active.Boss.Aspects;
            List<string> named = rules.SummonsFor(prefab);
            if (named.Count == 0 && BiomeBoss.TryGetValue(BiomeOf(summoner), out string boss))
            {
                named = rules.SummonsFor(boss);
            }
            return named.ConvertAll(name => new SummonPick(name, SummonPick.OwnStars));
        }

        private static Heightmap.Biome BiomeOf(EliteController summoner)
        {
            ZDO? zdo = summoner.View != null && summoner.View.IsValid() ? summoner.View.GetZDO() : null;
            Heightmap.Biome rolled = zdo != null ? TraitStore.GetBiome(zdo) : Heightmap.Biome.None;
            return rolled != Heightmap.Biome.None ? rolled : Heightmap.FindBiome(summoner.transform.position);
        }
    }
}
