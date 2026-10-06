using System.Collections.Generic;
using HarmonyLib;
using PatchGuard;
using UnityEngine;

namespace OpenKeep.Core
{
    /// <summary>
    /// A chest another client owns changes hands only when its owner hands it over, the way the game grants an open:
    /// the asker sends <c>OpenKeep_HandOver</c> (no payload) to the ZDO's owner, which, when nobody uses the chest,
    /// force-sends its latest data to the asker and then sets the asker as owner. The asker acts on a later call, once
    /// it owns the chest, so it never writes over a change the last owner made and it has not received. Ship and cart
    /// storage never changes hands this way (it is the vehicle's network object: taking it would take the vehicle from
    /// the person driving it). A chest is asked for at most once per interval (a player's own action asks sooner than
    /// background work), so a loop that keeps needing a chest another client keeps using does not pass it back and
    /// forth every tick.
    /// </summary>
    public static class HandOver
    {
        public const string Rpc = "OpenKeep_HandOver";

        /// <summary>A player's own action (a craft, a placement, a station fed by hand) asks again after this long, seconds.</summary>
        public const float ActionSeconds = 1f;

        /// <summary>Background work (Auto Fuel, Auto Feed, pets, Auto Tidy) asks again after this long, seconds.</summary>
        public const float BackgroundSeconds = 30f;

        private static readonly Dictionary<Container, float> asked = new Dictionary<Container, float>();

        /// <summary>Asks the chest's owner to hand it over; false when it needs no asking, may not be asked or was asked lately.</summary>
        public static bool Ask(Container chest, float every)
        {
            ZNetView view = chest != null ? chest.m_nview : null;
            if (view == null || !view.IsValid() || view.IsOwner() || !view.GetZDO().HasOwner())
                return false;
            if (ContainerScan.IsShip(chest) || ContainerScan.IsCart(chest))
                return false;
            if (asked.TryGetValue(chest, out float at) && Time.time - at < every)
                return false;
            if (asked.Count > 256)
                asked.Clear();
            asked[chest] = Time.time;
            view.InvokeRPC(Rpc);
            return true;
        }

        /// <summary>The owner's side: hands a chest nobody uses over to the asker, latest data first.</summary>
        private static void Handle(Container chest, long sender)
        {
            ZNetView view = chest.m_nview;
            if (view == null || !view.IsValid() || !view.IsOwner() || sender == ZDOMan.GetSessionID())
                return;
            if (InUse(chest) || ContainerScan.IsShip(chest) || ContainerScan.IsCart(chest))
                return;
            ZDO zdo = view.GetZDO();
            ZDOMan.instance.ForceSendZDO(sender, zdo.m_uid);
            zdo.SetOwner(sender);
            Plugin.Log.LogDebug($"OpenKeep: handed {ContainerScan.PrefabName(chest)} over to peer {sender}");
        }

        private static bool InUse(Container chest)
        {
            if (chest.IsInUse() || (chest.m_wagon != null && chest.m_wagon.InUse()))
                return true;
            return chest.m_nview.GetZDO().GetInt(ZDOVars.s_inUse) == 1;
        }

        [HarmonyPatch(typeof(Container), nameof(Container.Awake))]
        private static class RegisterPatch
        {
            [HarmonyPostfix]
            private static void Postfix(Container __instance)
            {
                ZNetView view = __instance.m_nview;
                if (view == null || view.GetZDO() == null || view.m_functions == null || view.m_functions.ContainsKey(Rpc.GetStableHashCode()))
                    return;
                Container chest = __instance;
                view.Register(Rpc, sender => Guard.Run("hand over", () => Handle(chest, sender)));
            }
        }
    }
}
