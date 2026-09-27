using EliteCreaturesReborn.Mutations;
using EliteCreaturesReborn.Runtime;
using EliteCreaturesReborn.Traits;
using HarmonyLib;
using PatchGuard;

namespace EliteCreaturesReborn.Patches
{
    /// <summary>
    /// The one path that removes a marked creature without ever running <c>Character.OnDeath</c>: vanilla's own
    /// "despawn in day" AI exit (<c>MonsterAI.DespawnInDay</c>) walks the creature away and then calls
    /// <c>BaseAI.MoveAwayAndDespawn</c>, which network-destroys it directly. Named here rather than left as a silent
    /// hole, per the spec's "goods always drop" table - "Despawned by the game: Goods drop where it stood." A prefix,
    /// so the pouch is read while the ZDO is still valid, exactly as <c>Commands.PurgeCommand</c> already does before
    /// its own destroy call. The game calls this every AI tick while the creature walks away and destroys it only on
    /// the tick no player is within <see cref="DespawnRange"/>, so the pouch drops on that tick alone - dropping it on
    /// every walk-away tick duplicated the stolen goods.
    /// </summary>
    [HarmonyPatch(typeof(BaseAI), "MoveAwayAndDespawn")]
    public static class ThievingDespawnPatch
    {
        /// <summary>BaseAI.MoveAwayAndDespawn's own range: with a player this close it walks away instead of vanishing.</summary>
        private const float DespawnRange = 40f;

        private static void Prefix(BaseAI __instance) =>
            Guard.Run("BaseAI.MoveAwayAndDespawn thieving", () => DropPouch(__instance));

        private static void DropPouch(BaseAI ai)
        {
            Character? character = Traverse.Create(ai).Field("m_character").GetValue<Character>();
            ZNetView? nview = character != null ? character.GetComponent<ZNetView>() : null;
            EliteController? controller = character != null ? character.GetComponent<EliteController>() : null;
            if (character == null || nview == null || !nview.IsValid() || !nview.IsOwner()
                || controller == null || !controller.Ready || !controller.Traits.Has(Mutation.Thieving)
                || Player.GetClosestPlayer(character.transform.position, DespawnRange) != null)
            {
                return; // not a thief, or only walking away this tick: the destroy has not come yet
            }
            PouchDrop.DropAll(PouchStore.Load(nview.GetZDO()), character.transform.position);
        }
    }
}
