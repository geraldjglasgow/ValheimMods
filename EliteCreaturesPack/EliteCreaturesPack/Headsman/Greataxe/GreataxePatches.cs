using EliteCreaturesPack.Core;
using HarmonyLib;

namespace EliteCreaturesPack.Headsman
{
    /// <summary>Every player carries <see cref="GreataxeHold"/>, on every peer.</summary>
    [HarmonyPatch(typeof(Player), nameof(Player.Awake))]
    public static class GreataxeHolder
    {
        private static void Postfix(Player __instance) => SafeCall.Run("Player.Awake greataxe", () =>
        {
            if (__instance.GetComponent<GreataxeHold>() == null)
            {
                __instance.gameObject.AddComponent<GreataxeHold>();
            }
        });
    }

    /// <summary>
    /// A greataxe swing, on the attacker's machine as the game starts it: the second step of the combo, the spin, hits
    /// all round. Every swing sounds as the Battleaxe's do (the item's own trail and hit sounds, the game's).
    /// </summary>
    [HarmonyPatch(typeof(Attack), nameof(Attack.Start))]
    public static class GreataxeSwing
    {
        private const string Primary = "battleaxe_attack";

        private static void Postfix(Attack __instance, ItemDrop.ItemData weapon, bool __result)
        {
            if (__result && weapon?.m_shared.m_name == "$item_" + GreataxeItems.AxeWord
                && __instance.m_attackAnimation == Primary && __instance.m_currentAttackCainLevel == 1)
            {
                __instance.m_attackAngle = 360f;
            }
        }
    }
}
