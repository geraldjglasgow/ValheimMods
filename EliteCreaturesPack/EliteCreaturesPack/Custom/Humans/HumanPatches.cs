using EliteCreaturesPack.Core;
using HarmonyLib;

namespace EliteCreaturesPack.Custom.Humans
{
    /// <summary>
    /// A human's bow is drawn and its crossbow reloaded before the game's attack starts, and its ammunition filled after
    /// (<see cref="HumanRanged"/>). Runs for every humanoid's attack start, an AI's every frame while it wants to attack,
    /// so a registry lookup decides first; the player's own attacks pass straight through. A failure lets the attack
    /// go as the game's.
    /// </summary>
    [HarmonyPatch(typeof(Humanoid), nameof(Humanoid.StartAttack))]
    internal static class HumanAttackPatch
    {
        private static bool Prefix(Humanoid __instance, ref bool __result, out HumanRanged? __state)
        {
            HumanRanged? ranged = HumanRegistry.Find(__instance);
            __state = ranged;
            if (ranged == null || SafeCall.Run("human attack start", static r => r.Ready(), ranged, true))
            {
                return true;
            }
            __result = false;
            return false;
        }

        private static void Postfix(bool __result, HumanRanged? __state)
        {
            if (__result && __state != null)
            {
                SafeCall.Run("human attack started", static r => r.Fired(), __state);
            }
        }
    }

    /// <summary>A human's crossbow is loaded when its <see cref="HumanRanged"/> says so; the game's Humanoid says never.</summary>
    [HarmonyPatch(typeof(Humanoid), nameof(Humanoid.IsWeaponLoaded))]
    internal static class HumanLoadedPatch
    {
        private static bool Prefix(Humanoid __instance, ref bool __result)
        {
            HumanRanged? ranged = HumanRegistry.Find(__instance);
            if (ranged == null)
            {
                return true;
            }
            __result = SafeCall.Run("human crossbow loaded", static r => r.IsLoaded, ranged, false);
            return false;
        }
    }

    /// <summary>The game unloads the weapon as a crossbow fires and as a weapon is unequipped.</summary>
    [HarmonyPatch(typeof(Humanoid), nameof(Humanoid.ResetLoadedWeapon))]
    internal static class HumanUnloadPatch
    {
        private static void Postfix(Humanoid __instance)
        {
            HumanRanged? ranged = HumanRegistry.Find(__instance);
            if (ranged != null)
            {
                ranged.Unload();
            }
        }
    }

    /// <summary>
    /// As a human is armed (Humanoid.Start gives every non-player its default items, on every peer): its weapons' AI
    /// values fitted and its ammunition stocked (<see cref="HumanBody.Arm"/>).
    /// </summary>
    [HarmonyPatch(typeof(Humanoid), nameof(Humanoid.Start))]
    internal static class HumanArmPatch
    {
        private static void Postfix(Humanoid __instance)
        {
            if (HumanRegistry.Find(__instance) != null)
            {
                SafeCall.Run("human weapons", static human => HumanBody.Arm(human), __instance);
            }
        }
    }

    /// <summary>The player's ragdoll a human leaves gets the human's body, skin and hair (<see cref="HumanDeath"/>).</summary>
    [HarmonyPatch(typeof(Humanoid), nameof(Humanoid.OnRagdollCreated))]
    internal static class HumanRagdollPatch
    {
        private static void Postfix(Humanoid __instance, Ragdoll ragdoll)
        {
            if (HumanRegistry.Find(__instance) != null)
            {
                SafeCall.Run("human ragdoll", static (human, body) => HumanDeath.Dress(human, body), __instance, ragdoll);
            }
        }
    }

    /// <summary>
    /// A human's eitr never runs out, as its stamina never does (every non-player has endless stamina): the game lets a
    /// staff fire only for a character with an eitr pool, which only players have. Asked only as an attack that costs
    /// eitr starts, since Player has its own.
    /// </summary>
    [HarmonyPatch(typeof(Character), nameof(Character.GetMaxEitr))]
    internal static class HumanEitrPatch
    {
        private const float Endless = 9999f;

        private static bool Prefix(Character __instance, ref float __result)
        {
            if (HumanRegistry.Find(__instance) == null)
            {
                return true;
            }
            __result = Endless;
            return false;
        }
    }
}
