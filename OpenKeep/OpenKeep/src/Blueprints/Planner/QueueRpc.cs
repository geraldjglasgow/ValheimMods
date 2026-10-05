using System.Collections.Generic;
using OpenKeep.Blueprints.Sites;

namespace OpenKeep.Blueprints.Planner
{
    /// <summary>
    /// RPC "OpenKeep_SiteQueue" on a site marker's network view: the whole new build queue (<see cref="SiteState.WriteQueue"/>
    /// bytes in a package), sent to the ZDO's owner. Registered on every machine for every marker that wakes (the owner
    /// can change); only the owner writes, after <see cref="QueueCheck.Clean"/>, and the ZDO carries the queue to
    /// everyone else.
    /// </summary>
    public static class QueueRpc
    {
        public const string Name = "OpenKeep_SiteQueue";

        /// <summary>On <see cref="SiteHooks.MarkerCreated"/>.</summary>
        public static void Register(SiteMarker marker)
        {
            ZNetView view = marker.View;
            if (view == null || view.m_functions.ContainsKey(Name.GetStableHashCode()))
                return;
            view.Register<ZPackage>(Name, (sender, pkg) => BlueprintSafe.Run("OpenKeep site queue", () => Receive(marker, pkg)));
        }

        public static void Send(SiteMarker site, List<SiteSelection> queue)
        {
            if (site == null || site.View == null || !site.View.IsValid())
                return;
            site.View.InvokeRPC(Name, new ZPackage(SiteState.WriteQueue(queue)));
        }

        private static void Receive(SiteMarker marker, ZPackage pkg)
        {
            if (marker == null || marker.View == null || !marker.View.IsValid() || !marker.View.IsOwner())
                return;
            Blueprint bp = marker.State?.Blueprint;
            if (bp == null || pkg == null)
                return;
            List<SiteSelection> sent = SiteState.ReadQueue(pkg.GetArray());
            marker.State.SetQueue(QueueCheck.Clean(sent, bp.Pieces.Count));
        }
    }
}
