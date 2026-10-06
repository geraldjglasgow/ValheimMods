using System.Collections.Generic;
using UnityEngine;

namespace OpenKeep.Blueprints.Sites
{
    /// <summary>
    /// A site the dedicated server owns (it lies near the server's own reference point) needs a player's machine to
    /// build it. The machines that have it loaded ask the server (routed <see cref="Rpc"/>, at most every
    /// <see cref="AskSeconds"/>), and the server, as the owner, hands it to the first that asks: its latest data is
    /// force-sent to that machine, then that machine is set as owner. Later asks find the site no longer the server's
    /// and change nothing, so two machines never both build it.
    /// </summary>
    public static class SiteTakeOver
    {
        public const string Rpc = "OpenKeep_SiteTakeOver";

        private const float AskSeconds = 5f;

        private static readonly Dictionary<ZDOID, float> asked = new Dictionary<ZDOID, float>();

        /// <summary>A player's machine: asks the server for a site it owns.</summary>
        public static void Ask(SiteMarker site)
        {
            ZDOID id = site.View.GetZDO().m_uid;
            if (asked.TryGetValue(id, out float at) && Time.time - at < AskSeconds)
                return;
            if (asked.Count > 64)
                asked.Clear();
            asked[id] = Time.time;
            ZRoutedRpc.instance.InvokeRoutedRPC(Rpc, id);
        }

        /// <summary>The server: a site it still owns goes to the asker, latest data first.</summary>
        public static void OnAsk(long sender, ZDOID id)
        {
            if (ZNet.instance == null || !ZNet.instance.IsServer() || ZDOMan.instance == null)
                return;
            ZDO zdo = ZDOMan.instance.GetZDO(id);
            if (zdo == null || zdo.GetPrefab() != SitePrefab.PrefabName.GetStableHashCode())
                return;
            if (zdo.GetOwner() != ZDOMan.GetSessionID() || sender == ZDOMan.GetSessionID())
                return;
            ZDOMan.instance.ForceSendZDO(sender, id);
            zdo.SetOwner(sender);
            Plugin.Log.LogDebug($"OpenKeep: site {id} handed to peer {sender} to build");
        }
    }
}
