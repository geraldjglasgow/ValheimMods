using HarmonyLib;

namespace HighSeat
{
    /// <summary>
    /// Where the server learns that an object changed, for <see cref="Changes"/>. These run for every ZDO write on the
    /// server, so each only adds an id to a set, and only while <see cref="Changes.Recording"/> is on (one test
    /// otherwise). What the game sends a player is decided by revisions alone (ZDOPeer.ShouldSend), so these cover every
    /// way a revision moves on the server:
    /// <list type="bullet">
    /// <item>Changes made here: every ZDO setter, a move, a rotation, a new prefab, type or Distant flag goes through
    /// ZDO.IncreaseDataRevision, and an owner change through IncreaseOwnerRevision; both call ZDOMan.SetDirtySector,
    /// and nothing else in the game raises a revision on the server.</item>
    /// <item>Data received from a player (ZDOMan.RPC_ZDOData): every newer object is read by ZDO.Deserialize, which
    /// nothing else calls.</item>
    /// <item>An owner change received alone (an older or equal data revision, a newer owner revision): RPC_ZDOData then
    /// calls only ZDO.SetOwnerInternal (and the OwnerRevision setter, too small to patch safely); so does every new
    /// object (ZDOMan.CreateNewZDO), here or received.</item>
    /// </list>
    /// A change that bypasses all three (another mod setting a revision or ZDOExtraData directly) is caught by the full
    /// check in turn (<see cref="FullCheck"/>), within seconds. Two kinds are also recorded apart, as urgent
    /// (<see cref="Changes.RecordUrgent"/>), so the players watching the object let go of any hold the update rate put on
    /// it (<see cref="Holds"/>): an owner change (every one, made here or received, goes through ZDO.SetOwnerInternal: the
    /// new owner is compared with the old, and a brand-new object, with no data revision yet, is left out, as no player
    /// can have it), and a move to another sector (ZDOMan.ZDOSectorInvalidated, from ZDO.SetSector), after which the game
    /// forgets the object for every player whose active area it left (ZDOPeer.ZDOSectorInvalidated), so to them it is new.
    /// A new owner revision that keeps the same owner (taken and given back between two sends) is not seen as urgent and
    /// may wait out its hold, a few rounds at most, which no player can tell: the owner they know is still the owner.
    /// </summary>
    internal static class ChangeHooks
    {
        [HarmonyPatch(typeof(ZDOMan), nameof(ZDOMan.SetDirtySector))]
        private static class ChangedHere
        {
            private static void Postfix(ZDO zdo)
            {
                if (Changes.Recording)
                {
                    Changes.Record(zdo.m_uid);
                }
            }
        }

        [HarmonyPatch(typeof(ZDO), nameof(ZDO.Deserialize))]
        private static class Received
        {
            private static void Postfix(ZDO __instance)
            {
                if (Changes.Recording)
                {
                    Changes.Record(__instance.m_uid);
                }
            }
        }

        [HarmonyPatch(typeof(ZDO), nameof(ZDO.SetOwnerInternal))]
        private static class OwnerSet
        {
            private static void Prefix(ZDO __instance, long uid)
            {
                if (Changes.Recording && __instance.DataRevision > 0 && uid != __instance.GetOwner())
                {
                    Changes.RecordUrgent(__instance.m_uid);
                }
            }

            private static void Postfix(ZDO __instance)
            {
                if (Changes.Recording)
                {
                    Changes.Record(__instance.m_uid);
                }
            }
        }

        [HarmonyPatch(typeof(ZDOMan), nameof(ZDOMan.ZDOSectorInvalidated))]
        private static class SectorChanged
        {
            private static void Postfix(ZDO zdo)
            {
                if (Changes.Recording)
                {
                    Changes.RecordUrgent(zdo.m_uid);
                }
            }
        }
    }
}
