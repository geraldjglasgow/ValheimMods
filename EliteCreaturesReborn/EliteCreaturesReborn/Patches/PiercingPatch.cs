using EliteCreaturesReborn.Mutations;
using EliteCreaturesReborn.Traits;
using HarmonyLib;
using UnityEngine;

namespace EliteCreaturesReborn.Patches
{
    /// <summary>
    /// Piercing's armour cut (<see cref="Piercing"/>), inside the game's hit on the struck player's own machine: before
    /// the hit (<see cref="HitPatch"/>) a hit from a Piercing creature on a player this machine owns marks that player,
    /// for this frame only; the player's body armour, which the game reads in the middle of the hit, comes back cut
    /// while the mark lasts; after the hit the mark goes. Body armour read anywhere else - the inventory's armour number,
    /// another player's hit - is never touched, and a hit that fails half way leaves a mark that expires with its frame.
    /// </summary>
    [HarmonyPatch(typeof(Player), nameof(Player.GetBodyArmor))]
    public static class PiercingPatch
    {
        private static Player? _marked;
        private static int _frame = -1;
        private static float _keep = 1f;

        /// <summary>Before a hit: marks the struck player when a Piercing creature's hit lands on them here.</summary>
        internal static void Arm(Struck struck)
        {
            _marked = null;
            if (struck.Victim is Player player && struck.Owned && struck.AttackerElite != null
                && struck.AttackerElite.Traits.Has(Mutation.Piercing))
            {
                _marked = player;
                _frame = Time.frameCount;
                _keep = 1f - Piercing.Share(struck.AttackerElite.Rules, struck.AttackerElite.Traits);
            }
        }

        /// <summary>After the hit: the mark goes.</summary>
        internal static void Disarm() => _marked = null;

        private static void Postfix(Player __instance, ref float __result)
        {
            if (_marked != null && ReferenceEquals(__instance, _marked) && Time.frameCount == _frame)
            {
                __result *= _keep;
            }
        }
    }
}
