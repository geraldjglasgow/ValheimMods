using EliteCreaturesReborn.Mutations;
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

        // Every creature walking off to despawn comes through here on every AI tick, on the server too: the first test
        // is two field reads and one ZDO lookup, and only a creature carrying stolen goods goes any further.
        private static void Prefix(BaseAI __instance)
        {
            ZNetView nview = __instance.m_nview;
            if (nview != null && nview.IsValid() && nview.IsOwner() && PouchStore.Count(nview.GetZDO()) > 0)
            {
                Guard.Run("BaseAI.MoveAwayAndDespawn thieving", static ai => DropPouch(ai), __instance);
            }
        }

        private static void DropPouch(BaseAI ai)
        {
            Character character = ai.m_character;
            if (character == null || Player.GetClosestPlayer(character.transform.position, DespawnRange) != null)
            {
                return; // only walking away this tick: the destroy has not come yet
            }
            PouchDrop.DropAll(PouchStore.Load(ai.m_nview.GetZDO()), character.transform.position);
        }
    }
}
