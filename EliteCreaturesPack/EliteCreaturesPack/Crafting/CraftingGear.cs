using System.Collections.Generic;
using EliteCraftingLink;
using EliteCreaturesPack.Arsenal;
using EliteCreaturesPack.Core;
using EliteCreaturesPack.Crossbow;
using EliteCreaturesPack.Headsman;
using EliteCreaturesPack.Kraken;

namespace EliteCreaturesPack.Crafting
{
    /// <summary>
    /// The mod's gear in Elite Crafting's terms: the skeleton arsenal's nine weapons and the Kraken shield, each claimed for
    /// its item class and given its item level (1 Meadows to 8 Deep North). Every one already meets the class rule Elite
    /// Crafting's default file has for its game item (the bronze knife, sword, axe, mace, spear and atgeir, the finewood
    /// bow, the Arbalest, the Battleaxe, the silver shield); the claim pins it there whatever order a server's file puts
    /// the rules in (a file's own `items` list still wins). The level is where the item comes from, which its recipe
    /// cannot say, since Elite Crafting knows none of the mod's materials: the Black Forest's skeletons (their spine, the
    /// arsenal skeletons' and crossbowmen's drop) and its burial chambers (the Crypt Executioner's axehead) for the bone
    /// weapons, the open sea (the Kraken's beak; Elite Crafting rates the ocean as the Mountain) for the shield. Ammo (bone
    /// arrows, blunted bolts) and materials (spine, axehead, beak, the kraken's meat) stack, so Elite Crafting never makes
    /// them magic and they are left to it.
    /// </summary>
    internal static class CraftingGear
    {
        private const int BlackForest = 2, Ocean = 4;

        /// <summary>Elite Crafting's class of each arsenal weapon, by the weapon's key.</summary>
        private static readonly Dictionary<string, string> ArsenalClasses = new Dictionary<string, string>
        {
            ["Dagger"] = "knife", ["Sword"] = "sword_1h", ["Axe"] = "axe_1h", ["Mace"] = "mace_1h",
            ["Spear"] = "spear", ["Atgeir"] = "atgeir", ["Bow"] = "bow",
        };

        /// <summary>Claims and levels every piece; returns how many Elite Crafting took.</summary>
        public static int Register()
        {
            int taken = 0;
            foreach ((string prefab, string itemClass, int level) in Gear())
            {
                bool claimed = CraftingClasses.ClaimItems(itemClass, prefab);
                bool levelled = CraftingClasses.SetItemLevel(prefab, level);
                if (claimed && levelled)
                {
                    taken++;
                }
                else
                {
                    Log.Warn($"Elite Crafting did not take {prefab} as {itemClass} at item level {level}.");
                }
            }
            return taken;
        }

        /// <summary>Prefab, Elite Crafting class and item level of each piece.</summary>
        private static IEnumerable<(string, string, int)> Gear()
        {
            foreach (ArsenalWeapon weapon in ArsenalWeapon.All)
            {
                if (ArsenalClasses.TryGetValue(weapon.Key, out string itemClass))
                {
                    yield return (weapon.Item, itemClass, BlackForest);
                }
                else
                {
                    Log.Warn($"Elite Crafting: {weapon.Item} has no item class here; it is left to Elite Crafting's own rules.");
                }
            }
            yield return (XbowItem.PrefabName, "crossbow", BlackForest);
            yield return (GreataxeItems.AxeName, "battleaxe", BlackForest);
            yield return (KrakenLoot.Shield, "shield", Ocean);
        }
    }
}
