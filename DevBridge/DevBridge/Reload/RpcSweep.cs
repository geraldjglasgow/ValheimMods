using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;

namespace DevBridge.Reload
{
    /// <summary>
    /// RPC handlers of the old copy: routed RPCs (ZRoutedRpc, which refuses a second registration of a name), the
    /// direct RPCs on each connected peer (ZRpc) and the per-object RPCs of loaded network objects (ZNetView, which also
    /// refuses duplicates). Taken out so the new copy can register the same names again; until it does, this machine
    /// ignores them.
    /// </summary>
    internal static class RpcSweep
    {
        private static readonly FieldInfo RoutedTable = AccessTools.Field(typeof(ZRoutedRpc), "m_functions");
        private static readonly FieldInfo PeerTable = AccessTools.Field(typeof(ZRpc), "m_functions");
        private static readonly FieldInfo ViewTable = AccessTools.Field(typeof(ZNetView), "m_functions");

        internal static void Run(Assembly old, Report report)
        {
            var names = new List<string>();
            ZRoutedRpc routed = ZRoutedRpc.instance;
            if (routed != null)
            {
                report.Count("routed RPCs", Strip(RoutedTable.GetValue(routed) as IDictionary, old, names));
                routed.m_onNewPeer = (System.Action<long>)OldCode.Without(routed.m_onNewPeer, old, out int peerHandlers);
                report.Count("new-peer handlers", peerHandlers);
            }
            if (ZNet.instance != null)
                foreach (ZNetPeer peer in ZNet.instance.GetPeers().Where(p => p?.m_rpc != null))
                    report.Count("peer RPCs", Strip(PeerTable.GetValue(peer.m_rpc) as IDictionary, old, names));
            Views(old, report, names);
            if (names.Count > 0) report.Leave("RPC handlers removed, back when the new copy registers them: " + Report.Tally(names));
        }

        private static void Views(Assembly old, Report report, List<string> names)
        {
            if (ZNetScene.instance == null) return;
            int objects = 0;
            foreach (ZNetView view in ZNetScene.instance.m_instances.Values.ToList())
            {
                if (view == null) continue;
                int removed = Strip(ViewTable.GetValue(view) as IDictionary, old, names);
                report.Count("object RPCs", removed);
                if (removed > 0) objects++;
            }
            if (objects > 0) report.Leave($"{objects} loaded objects lost the old copy's RPC handlers; they get the new copy's when they load again");
        }

        /// <summary>Removes the table's entries whose handler is the old copy's code; returns how many.</summary>
        private static int Strip(IDictionary table, Assembly old, List<string> names)
        {
            if (table == null) return 0;
            List<object> keys = table.Keys.Cast<object>().Where(k => OldCode.Holds(table[k], old)).ToList();
            foreach (object key in keys)
            {
                names.Add(OldCode.HeldName(table[key], old));
                table.Remove(key);
            }
            return keys.Count;
        }
    }
}
