using EliteCreaturesReborn.Mutations;
using EliteCreaturesReborn.Runtime;
using EliteCreaturesReborn.Traits;
using HarmonyLib;

namespace EliteCreaturesReborn.Patches
{
    /// <summary>
    /// Switches a Devouring creature's enmity between two mutually exclusive modes. While feeding it treats other
    /// non-boss creatures as prey and ignores players, so the game's own targeting sends it after creatures and it eats
    /// with its ordinary attacks. Two things flip it to hunting players - and, while hunting, it ignores creatures so a
    /// closer one cannot pull it off the player: a player attacking it (a short-lived provocation), or its accumulated
    /// per-hit damage crossing the threat threshold, after which it hunts players for good. Both are tracked by the
    /// owner-side DevourBehaviour. Asymmetric on purpose: only the devourer's own enmity is overridden, so other
    /// creatures still treat it by their normal rules. Consulted on the owner of the querying AI; the Devouring flag is
    /// read from synced traits, so this only forces enmity where the devourer is simulated.
    /// </summary>
    [HarmonyPatch(typeof(BaseAI), "IsEnemy", new[] { typeof(Character), typeof(Character) })]
    public static class EnemyPatch
    {
        // Vanilla makes a monster an enemy of players by default, so this must be free to force enmity EITHER way rather
        // than only up (no early return on __result): while feeding it ignores players and eats creatures; while hunting
        // or provoked it does the reverse, focusing on players and leaving creatures be - so a closer creature can never
        // pull it off the player it turned on. Asymmetric: only the devourer's own enmity is overridden.
        private static void Postfix(Character a, Character b, ref bool __result)
        {
            if (a == null || b == null || a == b || b.IsBoss() || !HasDevouring(a))
            {
                return;
            }
            bool afterPlayers = AfterPlayers(a);
            __result = b.IsPlayer() ? afterPlayers : !afterPlayers;
        }

        private static bool HasDevouring(Character character)
        {
            EliteController controller = character.GetComponent<EliteController>();
            return controller != null && controller.Ready && controller.Traits.Has(Mutation.Devouring);
        }

        // A player counts as an enemy only once it is being hunted for good, or while a recent attack still provokes it.
        private static bool AfterPlayers(Character devourer)
        {
            DevourBehaviour behaviour = devourer.GetComponent<DevourBehaviour>();
            return behaviour != null && (behaviour.HuntsPlayers || behaviour.IsProvoked);
        }
    }
}
