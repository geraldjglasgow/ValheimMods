using EliteCrafting.Affixes;
using EliteCrafting.Config;
using EliteCrafting.Items;
using EliteCrafting.Rules;

namespace EliteCrafting.Stones
{
    /// <summary>
    /// The rune a Magic or Rare item may give back when another mod salvages it (OpenKeep's Salvage asks through the
    /// API): the first enabled promote rune that works on the rarity below the item's, so an Awakening Rune from a Magic
    /// item and an Ascension Rune from a Rare one, at <see cref="Chance"/>. None for a Normal item, an unknown rarity, a
    /// disabled rune or while <c>Runes from salvage</c> is off. The salvaging mod rolls the chance.
    /// </summary>
    internal static class SalvageRunes
    {
        /// <summary>The chance, 0-1, that a salvage gives the rune back.</summary>
        public const float Chance = 0.25f;

        /// <summary>The rune's prefab name (<c>ECF_Awakening</c>), or null when the item gives none back.</summary>
        public static string? PrefabFor(ItemDrop.ItemData? item)
        {
            if (item == null || !ModSettings.SalvageRunes.Value)
            {
                return null;
            }
            ItemState state = ItemState.Read(item);
            if (!state.IsMagic || state.Rarity == null)
            {
                return null;
            }
            StoneDef? rune = RaisedBy(ActiveRules.Current.Economy, state.Rarity);
            return rune != null && StonePrefabs.IsRegistered(rune.Prefab) ? rune.Prefab : null;
        }

        /// <summary><see cref="Chance"/> when the item gives a rune back, else 0.</summary>
        public static float ChanceFor(ItemDrop.ItemData? item) => PrefabFor(item) != null ? Chance : 0f;

        /// <summary>The first enabled promote rune that works on the rarity below <paramref name="rarity"/>.</summary>
        private static StoneDef? RaisedBy(EconomyRules economy, RarityDef rarity)
        {
            RarityDef? below = economy.Previous(rarity);
            if (below == null)
            {
                return null;
            }
            foreach (StoneDef rune in economy.Stones)
            {
                if (rune.Enabled && rune.Verb == StoneVerb.Promote && rune.AppliesToRarity(below.Id))
                {
                    return rune;
                }
            }
            return null;
        }
    }
}
