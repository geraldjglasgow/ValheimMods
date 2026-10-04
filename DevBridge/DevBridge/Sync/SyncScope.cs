using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.NetworkInformation;
using BepInEx.Configuration;
using DevBridge.Server;

namespace DevBridge.Sync
{
    /// <summary>Which ports /sync asks and how long it waits; read on the main thread before the work leaves it.</summary>
    internal sealed class SyncScope
    {
        /// <summary>As many ports as HttpBridge tries from the configured one.</summary>
        private const int PortsToTry = 10;

        internal const int ProbeMs = 5000;

        internal int OwnPort;
        internal List<int> Ports;
        internal int QueryMs;

        /// <summary>timeout= (default 30 s) bounds the whole call: the /status round, then the question itself.</summary>
        internal static SyncScope From(BridgeRequest request)
        {
            int own = DevBridgePlugin.Instance.Port;
            float total = request.Float("timeout", 30f);
            return new SyncScope
            {
                OwnPort = own,
                Ports = Wanted(request.Get("ports"), own),
                QueryMs = (int)(Math.Max(2f, total - ProbeMs / 1000f) * 1000f),
            };
        }

        internal string Range => Ports.Count > 1 && Ports.Last() - Ports.First() == Ports.Count - 1
            ? $"{Ports.First()}-{Ports.Last()}"
            : string.Join(", ", Ports);

        /// <summary>
        /// The wanted ports something listens on, from the system's table: Windows takes two seconds to refuse a
        /// connection to a closed port. All of them when the table cannot be read or lacks our own port.
        /// </summary>
        internal List<int> Listening()
        {
            try
            {
                var open = new HashSet<int>(IPGlobalProperties.GetIPGlobalProperties().GetActiveTcpListeners().Select(end => end.Port));
                if (open.Contains(OwnPort)) return Ports.Where(open.Contains).ToList();
            }
            catch (Exception)
            {
                // no table on this runtime: ask every port
            }
            return Ports;
        }

        private static List<int> Wanted(string only, int own)
        {
            IEnumerable<int> ports = only == null ? Enumerable.Range(FirstPort(), PortsToTry) : only.Split(',').Select(Port);
            return ports.Concat(new[] { own }).Distinct().OrderBy(port => port).ToList();
        }

        private static int Port(string text) =>
            int.TryParse(text.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int port) && port > 0 && port < 65536
                ? port
                : throw new BridgeException($"ports= takes port numbers separated by commas, not '{text}'");

        private static int FirstPort() =>
            DevBridgePlugin.Instance.Config.TryGetEntry("Server", "Port", out ConfigEntry<int> port) ? port.Value : 7780;
    }
}
