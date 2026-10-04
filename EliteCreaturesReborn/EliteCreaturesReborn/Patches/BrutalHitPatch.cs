using EliteCreaturesReborn.Aspects;
using HarmonyLib;

namespace EliteCreaturesReborn.Patches
{
    /// <summary>
    /// A Brutal throw, decided where a hit on a player is applied: <c>Character.RPC_Damage</c> on the player's own client,
    /// which owns their body. The prefix notes whether the hit is a marked one aimed at this machine's own player and not
    /// rolled through - read first, because the game drops a dodged hit without applying it and this postfix still runs
    /// after that early return. The postfix then throws the player (<see cref="BrutalHit.Judge"/>) once the game has
    /// dealt the damage and decided any block. Any unmarked hit is one float check. It runs inside the game's hit, so a
    /// failure is reported and swallowed, and the hit stays an ordinary one.
    /// </summary>
    [HarmonyPatch(typeof(Character), "RPC_Damage")]
    public static class BrutalHitPatch
    {
        private static void Prefix(Character __instance, HitData hit, out bool __state) =>
            __state = hit != null && BrutalBlow.Carries(hit) && BrutalHit.Aimed(__instance, hit);

        private static void Postfix(Character __instance, HitData hit, bool __state)
        {
            if (__state)
            {
                SafeCall.Run("Character.RPC_Damage brutal", () => BrutalHit.Judge(__instance, hit));
            }
        }
    }

    /// <summary>
    /// A block that held against a marked hit. Inside <c>Humanoid.BlockAttack</c> the game takes the blocked damage off
    /// the hit (<c>HitData.BlockDamage</c>) exactly when the block holds - stamina left, the guard not broken, a parry
    /// included - so that call is the signal (<see cref="BrutalHit.Withstood"/>). Any unmarked hit is one float check; a
    /// failure is reported and swallowed, and the block counts as broken.
    /// </summary>
    [HarmonyPatch(typeof(HitData), nameof(HitData.BlockDamage))]
    public static class BrutalBlockPatch
    {
        private static void Postfix(HitData __instance)
        {
            if (BrutalBlow.Carries(__instance))
            {
                SafeCall.Run("HitData.BlockDamage brutal", () => BrutalHit.Withstood(__instance));
            }
        }
    }
}
