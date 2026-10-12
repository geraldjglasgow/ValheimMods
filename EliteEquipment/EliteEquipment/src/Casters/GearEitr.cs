using HarmonyLib;

namespace EliteEquipment.Casters
{
    /// <summary>
    /// Eitr before the Mistlands: the game starts a player's eitr at 0 and adds only food eitr
    /// (<c>Player.GetTotalFoodValue</c>, read 2026-10-09); this adds the worn robes' and the wielded wand's. The game
    /// recalculates it on its food update (about once a second), on the player's own machine, so taking a robe off lowers
    /// the bar there and the game shares the value as it does for food.
    /// </summary>
    [HarmonyPatch(typeof(Player), "GetTotalFoodValue")]
    public static class GearEitr
    {
        [HarmonyPostfix]
        private static void Postfix(Player __instance, ref float eitr) => eitr += CasterGearWorn.Eitr(__instance);
    }
}
