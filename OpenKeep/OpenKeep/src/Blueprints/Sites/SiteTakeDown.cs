using OpenKeep.Core;

namespace OpenKeep.Blueprints.Sites
{
    /// <summary>
    /// Taking a construction site down (Shift + E on its post, or 'openkeep blueprint undo'): its store is dropped at
    /// the post and the post removed, with its ghost; pieces already built stay (the hammer removes them with the game's
    /// refund). Only the site's creator or an admin may: the player's machine checks first (to answer at once), then
    /// asks the server (<see cref="AskRpc"/>), which knows the admin list and every player's id and checks again; it
    /// passes an approved request on to the machine that owns the post (<see cref="RpcName"/>), which acts only on the
    /// server's word. So the rule holds on a dedicated server whoever owns the post, and needs OpenKeep on the server.
    /// </summary>
    public static class SiteTakeDown
    {
        public const string RpcName = "OpenKeep_SiteTakeDown";
        public const string AskRpc = "OpenKeep_SiteTakeDownAsk";

        /// <summary>SiteHooks.MarkerCreated: the post answers the server's approval.</summary>
        public static void Register(SiteMarker site)
        {
            site.View.Register(RpcName, sender => BlueprintSafe.Run("OpenKeep site take down", () => OnApproved(site, sender)));
        }

        /// <summary>Shift + E on the post by the local player.</summary>
        public static void Ask(SiteMarker site, Player player)
        {
            if (!MayAsk(site, player))
            {
                player.Message(MessageHud.MessageType.Center, BlueprintWords.Format(SiteWords.NotYours, site.State.CreatorName));
                return;
            }
            Request(site.View.GetZDO().m_uid);
            player.Message(MessageHud.MessageType.Center, BlueprintWords.Format(SiteWords.TakenDown, site.State.Name));
        }

        /// <summary>The local player made the site or is an admin (the server checks again).</summary>
        public static bool MayAsk(SiteMarker site, Player player)
        {
            long creator = site.State.Creator;
            return (creator != 0L && creator == player.GetPlayerID()) || (ZNet.instance != null && ZNet.instance.LocalPlayerIsAdminOrHost());
        }

        /// <summary>Asks the server to take the site with this ZDO down.</summary>
        public static void Request(ZDOID site) => ZRoutedRpc.instance?.InvokeRoutedRPC(AskRpc, site);

        /// <summary>Server: the creator's or an admin's request goes to the post's owner (or back to the asker when nobody owns it).</summary>
        public static void OnAsk(long sender, ZDOID id)
        {
            if (ZNet.instance == null || !ZNet.instance.IsServer())
                return;
            ZDO zdo = ZDOMan.instance.GetZDO(id);
            if (zdo == null || zdo.GetPrefab() != SitePrefab.PrefabName.GetStableHashCode())
                return;
            if (!SiteRights.MayTakeDown(sender, zdo))
            {
                ZRoutedRpc.instance.InvokeRoutedRPC(sender, "ShowMessage", (int)MessageHud.MessageType.Center, SiteWords.Refused);
                return;
            }
            long owner = zdo.GetOwner();
            ZRoutedRpc.instance.InvokeRoutedRPC(owner != 0L ? owner : sender, id, RpcName);
        }

        /// <summary>The owner (or the asker, when nobody owned the post): only the server's approval counts.</summary>
        private static void OnApproved(SiteMarker site, long sender)
        {
            if (site == null || site.State == null || !site.View.IsValid() || sender != ZRoutedRpc.instance.GetServerPeerID())
                return;
            site.View.ClaimOwnership();
            int dropped = SiteStore.Drop(site.transform.position, site.State.Store);
            Plugin.Log.LogInfo($"OpenKeep: site {site.State.Name} at {site.State.Frame.Origin} taken down ({site.State.BuiltCount} pieces built, {dropped} materials handed back)");
            ZNetScene.instance.Destroy(site.gameObject);
        }
    }
}
