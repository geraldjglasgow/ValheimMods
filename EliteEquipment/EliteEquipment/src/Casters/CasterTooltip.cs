using HarmonyLib;
using Translations;

namespace EliteEquipment.Casters
{
    /// <summary>
    /// The caster numbers in an item's tooltip, after the game's own lines: a wand's heal power (at the quality shown) and
    /// eitr, the focus's heal power, a robe piece's eitr. Each line is one word with its value as $1.
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
            else if (CasterGearWorn.IsFocus(item))
            {
                __result += Line("ee_heal_power", "+" + CasterGear.FocusHealPower);
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

        private static string Line(string key, string value) => "\n" + Lang.Format(key, "<color=orange>" + value + "</color>");
    }
}
