using EliteCreaturesReborn.Mutations;
using HarmonyLib;
using PatchGuard;

namespace EliteCreaturesReborn.Patches
{
    /// <summary>
    /// A Cloning decoy's blow emptied at the swing, on the decoy's owner where its attack runs: the game builds every melee,
    /// thrown and area blow's damage in <c>Attack.ModifyDamage</c>, so whatever it lands on - a wall, a tree, a cart -
    /// takes nothing from it. Hits on players and creatures are emptied again where they land (<see cref="CloneHitPatch"/>),
    /// which also catches what never passes through here, such as an area its projectile leaves behind.
    /// </summary>
    [HarmonyPatch(typeof(Attack), "ModifyDamage")]
    public static class CloneAttackPatch
    {
        private static void Postfix(Attack __instance, HitData hitData) =>
            Guard.Run("Attack.ModifyDamage cloning", () => Disarm(__instance, hitData));

        private static void Disarm(Attack attack, HitData hit)
        {
            if (hit != null && CloneStore.IsDecoy(attack.m_character))
            {
                CloneHits.Harmless(hit);
            }
        }
    }
}
