using EliteCreaturesReborn.Mutations;
using EliteCreaturesReborn.Runtime;
using EliteCreaturesReborn.Traits;
using HarmonyLib;

namespace EliteCreaturesReborn.Patches
{
    /// <summary>
    /// Switches a Devouring creature's enmity between two mutually exclusive modes. While feeding it treats other
    /// creatures it can eat (never a boss or a large creature, and their current health at most `max prey health` percent
    /// of its own, see <see cref="DevourLimits"/>) as prey and ignores players, so the game's own targeting sends it after what it can
    /// eat and it eats with its ordinary attacks; toward a creature too big to eat it keeps the game's own answer, as its
    /// kind would, so it is never left unable to hit back. Two things flip it to hunting players - and, while
    /// hunting, it ignores creatures so a closer one cannot pull it off the player: a player attacking it (a short-lived
    /// provocation), or its accumulated per-hit damage crossing the threat threshold, after which it hunts players for
    /// good. Both are tracked by the owner-side DevourBehaviour. Once it has eaten its one creature per star it is sated
    /// and, unless it is hunting players, this stands aside: the game's own enmity rules it like any creature of its
    /// kind. Asymmetric on purpose: only the devourer's own enmity is overridden, so other creatures still treat it by
    /// their normal rules. Consulted on the owner of the querying AI; the Devouring flag, the meal count and both healths
    /// are read from synced state, so this only forces enmity where the devourer is simulated. It stands aside while
    /// <see cref="GildedEnemyPatch.GameOnly"/> asks for the game's own answer.
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
            if (GildedEnemyPatch.GameOnly || a == null || b == null || a == b || b.IsBoss())
            {
                return;
            }
            EliteController? devourer = Devourer(a);
            bool? forced = devourer != null ? Enmity(a, devourer, b) : null;
            if (forced.HasValue)
            {
                __result = forced.Value;
            }
        }

        // The devourer's enmity toward b, or null to leave the game's own answer. A creature too big to swallow is not
        // prey, so toward it the game's answer stands: it still fights what its kind fights, and fights back, rather
        // than standing defenceless while its hits pass through.
        private static bool? Enmity(Character a, EliteController devourer, Character b)
        {
            if (AfterPlayers(a))
            {
                return b.IsPlayer(); // hunting or provoked: players only
            }
            if (DevourLimits.Sated(devourer))
            {
                return null; // it has eaten its fill: the game's own answer, as for any creature of its kind
            }
            if (b.IsPlayer())
            {
                return false; // feeding: players are beneath its notice
            }
            return DevourLimits.FitsInMaw(devourer, b) ? true : (bool?)null;
        }

        private static EliteController? Devourer(Character character)
        {
            EliteController controller = character.GetComponent<EliteController>();
            return controller != null && controller.Ready && controller.Traits.Has(Mutation.Devouring) ? controller : null;
        }

        // A player counts as an enemy only once it is being hunted for good, or while a recent attack still provokes it.
        private static bool AfterPlayers(Character devourer)
        {
            DevourBehaviour behaviour = devourer.GetComponent<DevourBehaviour>();
            return behaviour != null && (behaviour.HuntsPlayers || behaviour.IsProvoked);
        }
    }
}
