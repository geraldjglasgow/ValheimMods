using System.Collections.Generic;
using HarmonyLib;
using Translations;

namespace EliteEquipment.Casters
{
    /// <summary>
    /// The caster numbers in an item's tooltip, after the game's own lines: a wand's heal power (at the quality shown) and
    /// eitr, a spellbook's own heal power and its pages ("4/2" for a book holding more than its room), a robe piece's eitr.
    /// Each line is one word with its values as $1 and $2.
    /// </summary>
    [HarmonyPatch(typeof(ItemDrop.ItemData), nameof(ItemDrop.ItemData.GetTooltip),
        typeof(ItemDrop.ItemData), typeof(int), typeof(bool), typeof(float), typeof(int), typeof(bool))]
    public static class CasterTooltip
    {
        [HarmonyPostfix]
        private static void Postfix(ItemDrop.ItemData item, int qualityLevel, ref string __result)
        {
            if (item == null || !CastersSettings.On)
                return;
            if (CasterGearWorn.IsWand(item))
            {
                float power = HealPower.Base(item, qualityLevel);
                __result += Line("ee_heal_power", power.ToString("0.#"))
                    + Line("ee_eitr_bonus", "+" + CasterGearWorn.WandPiece(item).Eitr);
            }
            else if (CasterGearWorn.IsBook(item))
            {
                __result += Line("ee_heal_power", "+" + CasterGearWorn.BookPiece(item).HealPower) + Pages(item);
            }
            else
            {
                foreach (GearPiece piece in CasterGear.Robes)
                {
                    if (item.m_shared.m_name == piece.Token)
                        __result += Line("ee_eitr_bonus", "+" + piece.Eitr);
                }
            }
        }

        private static string Pages(ItemDrop.ItemData book)
        {
            var names = new List<string>();
            foreach (Spell spell in BookPages.Of(book))
                names.Add(Lang.Get(spell.Token));
            string list = names.Count > 0 ? string.Join(", ", names) : Lang.Get("ee_book_empty");
            return "\n" + Lang.Format("ee_book_pages", names.Count + "/" + BookPages.Room(book), "<color=orange>" + list + "</color>");
        }

        private static string Line(string key, string value) => "\n" + Lang.Format(key, "<color=orange>" + value + "</color>");
    }
}
