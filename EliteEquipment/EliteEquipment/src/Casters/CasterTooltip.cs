using HarmonyLib;

namespace EliteEquipment.Casters
{
    /// <summary>
    /// The caster numbers in an item's tooltip, after the game's own lines: a wand's heal power (at the quality shown) and
    /// eitr, the focus's heal power, a robe piece's eitr. Words stay tokens; the game localizes the whole tooltip.
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
                __result += Line(CasterWords.HealPower, power.ToString("0.#"))
                    + Line(CasterWords.EitrBonus, "+" + CasterGearWorn.WandPiece(item).Eitr);
            }
            else if (CasterGearWorn.IsFocus(item))
            {
                __result += Line(CasterWords.HealPower, "+" + CasterGear.FocusHealPower);
            }
            else
            {
                foreach (GearPiece piece in CasterGear.Robes)
                {
                    if (item.m_shared.m_name == piece.Token)
                        __result += Line(CasterWords.EitrBonus, "+" + piece.Eitr);
                }
            }
        }

        private static string Line(string word, string value) => "\n" + word + ": <color=orange>" + value + "</color>";
    }
}
