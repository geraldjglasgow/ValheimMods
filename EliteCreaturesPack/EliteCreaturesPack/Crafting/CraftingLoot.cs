using System.Collections.Generic;
using EliteCraftingLink;
using EliteCreaturesPack.Arsenal;
using EliteCreaturesPack.Core;
using EliteCreaturesPack.Crossbow;
using EliteCreaturesPack.Headsman;
using EliteCreaturesPack.Kraken;
using EliteCreaturesPack.Mimic;
using EliteCreaturesPack.RimeGiant;
using EliteCreaturesPack.Slinger;

namespace EliteCreaturesPack.Crafting
{
    /// <summary>
    /// What the mod's creatures drop of Elite Crafting's runes and rolled gear (Elite Crafting's own roll, beside the
    /// creature's drops, which stay as they are): loot profiles in the fields of its file's <c>drops.bosses</c> and
    /// <c>drops.creatures</c>. Elite Crafting reads a creature's level (1-8) from the game's spawn lists, and none of these
    /// is in them (they come from spawn entries of their own, spawners and swaps), so each gets its level here.
    /// <list type="bullet">
    /// <item>Bosses and mini-bosses get boss entries like the game's bosses in Elite Crafting's file (runes and gear every
    /// kill, a bonus rune, the boss rarity row). The Kraken, a boss of the open sea (level 4): Moder's at that level, the
    /// Serpent's rune for her Consecrated. The Rime Giant, a sleeping mini-boss, one per mountain ever (the lowest level of
    /// its spawn biomes, Mountain 4): Moder's with half her bonus. The Crypt Executioner, the burial chambers' mini-boss
    /// (Black Forest 2), back every few days: a little under the Elder.</item>
    /// <item>The skeletons that take the Black Forest Skeleton's spawns (the arsenal skeletons, the crossbowman): its
    /// level 2. The Slinger: the lowest level of its spawn biomes (Black Forest 2), as for the greydwarfs it replaces.</item>
    /// <item>The crypt mimic is a crypt chest: the level comes from where it dies (the chamber's biome), the chances are a
    /// chest's (Elite Crafting's defaults: chests 30% runes and 10% gear, a level 2 creature 5% and 1.25%), since the chest
    /// it replaced was rolled and then removed.</item>
    /// <item>The skeletons the Executioner raises drop nothing, Elite Crafting's loot included: no rune farm beside a boss.</item>
    /// </list>
    /// Only a profile that changed since it was last sent goes again, so a settings change costs at most two calls.
    /// </summary>
    internal static class CraftingLoot
    {
        private const int BlackForest = 2, Mountain = 4, Ocean = 4;

        private static readonly Dictionary<string, string> sent = new Dictionary<string, string>();

        /// <summary>Sends every profile that changed; returns how many Elite Crafting took.</summary>
        public static int Register()
        {
            int taken = 0;
            foreach (KeyValuePair<string, string> profile in Profiles())
            {
                if (sent.TryGetValue(profile.Key, out string last) && last == profile.Value)
                {
                    continue;
                }
                sent[profile.Key] = profile.Value;
                if (CraftingHooks.SetCreatureLoot(profile.Value))
                {
                    taken++;
                }
                else
                {
                    Log.Warn($"Elite Crafting refused the loot of {profile.Key}: {profile.Value}");
                }
            }
            return taken;
        }

        private static IEnumerable<KeyValuePair<string, string>> Profiles()
        {
            yield return Boss(KrakenPrefabs.Creature, Ocean, 3, "serpent", 100);
            yield return Boss(RimeGiantPrefabs.Creature, BiomeLevels.Lowest(RimeGiantSettings.Biomes, Mountain), 3, "consecrated", 50);
            yield return Boss(HeadsmanPrefabs.Creature, BlackForest, 2, "ascension", 25);
            yield return Entry(HeadsmanSummon.Name, "\"multiplier\": 0");
            yield return Entry(MimicPrefabs.Creature, "\"rune_multiplier\": 6, \"gear_multiplier\": 8");
            yield return Entry(SlingerPrefabs.Creature, Tier(BiomeLevels.Lowest(SlingerSettings.Biomes, BlackForest)));
            foreach (string skeleton in BlackForestSkeletons())
            {
                yield return Entry(skeleton, Tier(BlackForest));
            }
        }

        /// <summary>The arsenal skeletons and the crossbowmen, by prefab name.</summary>
        private static IEnumerable<string> BlackForestSkeletons()
        {
            foreach (ArsenalKind kind in ArsenalKind.All)
            {
                foreach (ArsenalWeapon weapon in ArsenalWeapon.All)
                {
                    yield return kind.Creature(weapon);
                }
            }
            foreach (XbowKind kind in XbowKind.All)
            {
                yield return kind.Creature;
            }
        }

        /// <summary>A boss entry: <paramref name="runes"/> runes and one piece of gear every kill, and a bonus rune at <paramref name="chance"/> percent.</summary>
        private static KeyValuePair<string, string> Boss(string prefab, int tier, int runes, string bonus, int chance) =>
            Entry(prefab, $"\"boss\": true, {Tier(tier)}, \"rune_rolls\": {runes}, \"gear_rolls\": 1, "
                + $"\"bonus\": [ {{ \"rune\": \"{bonus}\", \"chance\": {chance}, \"amount\": 1 }} ]");

        private static string Tier(int tier) => $"\"tier\": {tier}";

        private static KeyValuePair<string, string> Entry(string prefab, string fields) =>
            new KeyValuePair<string, string>(prefab, $"{{ \"prefab\": \"{prefab}\", {fields} }}");
    }
}
