using System;
using HarmonyLib;
using PatchGuard;

namespace EliteCreaturesReborn.Raids
{
    /// <summary>
    /// A Raiders Chest cannot be opened while its raid is on (features/raids.md section 5): the game hands a chest to
    /// whoever opens it, and the chest's owner runs the raid, so an opening mid-raid would move the raid to the opener.
    /// The chest's own E refuses already (<see cref="RaidChest"/>); this is the owner's side, for an open asked any other
    /// way (another mod calling the container directly): the asker hears "in use", as from a chest someone has open, and
    /// ownership stays. Every other container costs one string comparison when it is asked to open. Never throws: a
    /// failure leaves the game's own answer.
    /// </summary>
    [HarmonyPatch(typeof(Container), nameof(Container.RPC_RequestOpen))]
    internal static class ChestLock
    {
        private static bool Prefix(Container __instance, long uid)
        {
            try
            {
                if (!string.Equals(__instance.m_name, ChestPrefab.ContainerName) || !Locked(__instance))
                {
                    return true;
                }
                __instance.m_nview.InvokeRPC(uid, "RPC_OpenResponse", false);
                return false;
            }
            catch (Exception e)
            {
                Guard.Report(e, "Raiders Chest lock");
                return true;
            }
        }

        private static bool Locked(Container chest)
        {
            ZNetView view = chest.m_nview;
            return view != null && view.IsValid() && view.IsOwner() && Raid.IsRunning(view);
        }
    }
}
