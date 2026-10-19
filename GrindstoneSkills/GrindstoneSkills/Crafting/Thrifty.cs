using System.Collections.Generic;
using Translations;
using UnityEngine;

namespace GrindstoneSkills
{
    /// <summary>
    /// Thrifty: each batch of a craft or upgrade at a station that trains Crafting (1, or each craft of a multi-craft, as
    /// the game rolls its own extra item) has up to a 10% chance at Crafting 100, linear from 0, to give back one unit of
    /// one material it paid, picked at random among the materials <see cref="StationCraft"/> saw paid. The unit is a new
    /// item of the material's prefab, of the quality paid when the payment named one, added by the game's
    /// Inventory.AddItem(name, ...), which drops it at the crafter's feet when the inventory is full; a message top left
    /// names it. Runs on the crafter's client after the craft. Building never rolls: a removed piece gives its full cost
    /// back, so a refund there would make materials.
    /// </summary>
    public static class Thrifty
    {
        /// <summary>Chance per batch at Crafting 100.</summary>
        private const float ChanceAt100 = 0.10f;

        /// <summary>One material a craft paid: the requirement's item prefab and the quality asked (-1: any).</summary>
        public readonly struct Material
        {
            public Material(ItemDrop prefab, int quality)
            {
                Prefab = prefab;
                Quality = quality;
            }

            public ItemDrop Prefab { get; }
            public int Quality { get; }
        }

        /// <summary>The chance per batch at a Crafting level, 0 to 0.10.</summary>
        public static float Chance(float level) => level > 0f ? ChanceAt100 * Mathf.Clamp01(level / 100f) : 0f;

        public static void Roll(Player player, int batches, float level, List<Material> paid)
        {
            float chance = Chance(level);
            if (player == null || chance <= 0f || paid.Count == 0)
                return;
            for (int batch = 0; batch < batches; batch++)
            {
                if (Random.value < chance)
                    GiveBack(player, paid[Random.Range(0, paid.Count)]);
            }
        }

        private static void GiveBack(Player player, Material material)
        {
            ItemDrop.ItemData shown = material.Prefab.m_itemData;
            int quality = material.Quality > 0 ? material.Quality : shown.m_quality;
            if (player.GetInventory().AddItem(material.Prefab.gameObject.name, 1, quality, shown.m_variant, 0L, "", false) == null)
                return;
            string name = Localization.instance.Localize(shown.m_shared.m_name);
            player.Message(MessageHud.MessageType.TopLeft, Lang.Format("gs_craft_thrifty_saved", Lang.Get("gs_perk_thrifty"), name), 0, shown.GetIcon());
        }
    }
}
