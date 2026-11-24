using System.Collections.Generic;
using HaloRing;
using Translations;
using UnityEngine;

namespace EliteEquipment.Casters
{
    /// <summary>
    /// The spell ring's choices: every page in the held spellbook, each with its spell's icon, name and eitr cost (the
    /// cost as the cast would take it, worked out as the ring opens), disabled while the caster's eitr is short of it.
    /// The first page sits at twelve o'clock and the rest follow clockwise.
    /// </summary>
    public static class SpellRingItems
    {
        public static HaloRingItem[] For(Player player, List<Spell> pages)
        {
            int count = pages.Count;
            var items = new HaloRingItem[count];
            for (int i = 0; i < count; i++)
                items[(count - i) % count] = Item(player, pages[i]);
            return items;
        }

        /// <summary>The ring's size and look: the HaloRing library's (the retired HaloMenu's), with the first segment's middle at the top.</summary>
        public static HaloRingStyle Style(int count) => new HaloRingStyle { StartAngle = HaloRingStyle.TopFirst(count) };

        private static HaloRingItem Item(Player player, Spell spell)
        {
            float cost = SpellCost.Of(player, spell);
            Sprite icon = CasterBundles.Sprite(CasterBundles.Spells, spell.Icon);
            return new HaloRingItem(spell.Key, Lang.Get(spell.Token), icon)
            {
                Sublabel = Lang.Format("ee_spellbook_cost", Mathf.CeilToInt(cost)),
                IsEnabled = () => player != null && player.HaveEitr(cost),
                Tag = spell,
            };
        }
    }
}
