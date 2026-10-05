using System.Collections.Generic;
using HarmonyLib;
using PatchGuard;
using UnityEngine;

namespace OpenKeep.Stow
{
    /// <summary>
    /// Auto Tidy moves items only between chests one client owns. When the nearest home of a stray belongs to another
    /// client, that client is asked (<c>OpenKeep_TidyHandOver</c>, no payload, sent to the ZDO's owner) to hand the
    /// chest over, and it does so the way the game grants an open: the chest is not in use, its latest data is force
    /// sent to the asker, then the owner is set. A chest in use stays where it is; the asker tries again on a later look
    /// (at most once per <see cref="AskSeconds"/> per chest), so two clients never write one chest at once.
    /// </summary>
    public static class TidyHandOver
    {
        public const string Rpc = "OpenKeep_TidyHandOver";

        private const float AskSeconds = 30f;

        private static readonly Dictionary<Container, float> asked = new Dictionary<Container, float>();

        /// <summary>Asks the chest's owner to hand it over; false when it was asked lately or needs no asking.</summary>
        internal static bool Ask(Container chest)
        {
            ZNetView view = chest.m_nview;
            if (view == null || !view.IsValid() || view.IsOwner())
                return false;
            if (asked.TryGetValue(chest, out float at) && Time.time - at < AskSeconds)
                return false;
            if (asked.Count > 64)
                asked.Clear();
            asked[chest] = Time.time;
            view.InvokeRPC(Rpc);
            return true;
        }

        private static void Handle(Container chest, long sender)
        {
            ZNetView view = chest.m_nview;
            if (!StowSettings.Enabled.Value || !StowSettings.AutoTidy.Value || view == null || !view.IsValid() || !view.IsOwner())
                return;
            if (sender == ZDOMan.GetSessionID() || chest.IsInUse() || (chest.m_wagon != null && chest.m_wagon.InUse()))
                return;
            ZDO zdo = view.GetZDO();
            ZDOMan.instance.ForceSendZDO(sender, zdo.m_uid);
            zdo.SetOwner(sender);
            Plugin.Log.LogDebug($"OpenKeep: tidy handed {Core.ContainerScan.PrefabName(chest)} over to peer {sender}");
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
                view.Register(Rpc, sender => Guard.Run("tidy hand over", () => Handle(chest, sender)));
            }
        }
    }
}
