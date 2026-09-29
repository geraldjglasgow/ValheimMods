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
    /// A greataxe swing, on the attacker's machine as the game starts it: the combo step's own swing sound
    /// (<see cref="GreataxeSwings"/>, the secondary the slash's), and the second step, the spin, hits all round.
    /// </summary>
    [HarmonyPatch(typeof(Attack), nameof(Attack.Start))]
    public static class GreataxeSwing
    {
        private const string Primary = "battleaxe_attack";

        private static void Postfix(Attack __instance, ItemDrop.ItemData weapon, bool __result)
        {
            if (!__result || weapon?.m_shared.m_name != "$item_" + GreataxeItems.AxeWord)
            {
                return;
            }
            bool primary = __instance.m_attackAnimation == Primary;
            int step = primary ? __instance.m_currentAttackCainLevel : 0;
            __instance.m_trailStartEffect = GreataxeSwings.For(step);
            if (primary && step == 1)
            {
                __instance.m_attackAngle = 360f;
            }
        }
    }
}
