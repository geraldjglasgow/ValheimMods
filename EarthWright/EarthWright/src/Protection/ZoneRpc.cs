using EarthWright.Core;
using HarmonyLib;

namespace EarthWright.Protection
{
    /// <summary>
    /// The zone requests between an admin's machine and the server, as routed RPCs.
    /// <list type="bullet">
    /// <item><c>EW_ZoneRequest</c> (player to server): add or remove a zone. The server checks the sending peer against
    /// its admin list (<see cref="Side.PeerIsAdmin"/>) before <see cref="ZoneServer"/> changes anything.</item>
    /// <item><c>EW_ZoneReply</c> (server to that player): the outcome, printed in the console and shown top left.</item>
    /// </list>
    /// On a host or in single player the request goes to this machine itself.
    /// </summary>
    public static class ZoneRpc
    {
        public const string RequestRpc = "EW_ZoneRequest";
        public const string ReplyRpc = "EW_ZoneReply";

        private const byte OpAdd = 1;
        private const byte OpRemove = 2;

        /// <summary>The instance registered on: every session makes a new ZRoutedRpc with empty handler tables.</summary>
        private static ZRoutedRpc registeredOn;

        public static void EnsureRegistered()
        {
            ZRoutedRpc rpc = ZRoutedRpc.instance;
            if (rpc == null || registeredOn == rpc)
                return;
            registeredOn = rpc;
            rpc.Register<ZPackage>(RequestRpc, (sender, pkg) => Safe.Run("EarthWright zone request", () => OnRequest(sender, pkg)));
            rpc.Register<string>(ReplyRpc, (sender, text) => Safe.Run("EarthWright zone reply", () => OnReply(text)));
        }

        /// <summary>Player: asks the server to add (or replace) a zone. False when there is no session.</summary>
        public static bool SendAdd(AdminZone zone) => Send(OpAdd, zone);

        /// <summary>Player: asks the server to remove the zone with this name. False when there is no session.</summary>
        public static bool SendRemove(string name) => Send(OpRemove, new AdminZone { Name = name ?? "" });

        private static bool Send(byte op, AdminZone zone)
        {
            EnsureRegistered();
            if (ZRoutedRpc.instance == null || ZNet.instance == null)
                return false;
            ZPackage pkg = new ZPackage();
            pkg.Write(op);
            pkg.Write(zone.Name ?? "");
            pkg.Write(zone.X);
            pkg.Write(zone.Z);
            pkg.Write(zone.Radius);
            pkg.Write(zone.Player ?? "");
            ZRoutedRpc.instance.InvokeRoutedRPC(RequestRpc, pkg);
            return true;
        }

        /// <summary>Server: checks the sender, applies the change, answers.</summary>
        private static void OnRequest(long sender, ZPackage pkg)
        {
            if (!Side.IsServer)
                return;
            string reply = Side.PeerIsAdmin(sender) ? Handle(pkg) : ZoneWords.NotAdmin;
            ZRoutedRpc.instance.InvokeRoutedRPC(sender, ReplyRpc, reply);
        }

        private static string Handle(ZPackage pkg)
        {
            byte op = pkg.ReadByte();
            AdminZone zone = new AdminZone
            {
                Name = pkg.ReadString().Trim(),
                X = pkg.ReadSingle(),
                Z = pkg.ReadSingle(),
                Radius = pkg.ReadSingle(),
                Player = pkg.ReadString(),
            };
            return op == OpRemove ? ZoneServer.Remove(zone.Name) : ZoneServer.Add(zone);
        }

        private static void OnReply(string text)
        {
            string shown = Language.Localize(text);
            if (global::Console.instance != null)
                global::Console.instance.AddString("EarthWright: " + shown);
            Messages.TopLeft(shown);
        }
    }

    /// <summary>Registers the zone RPCs on every machine when a session starts, next to the game's own.</summary>
    [HarmonyPatch(typeof(Game), nameof(Game.Start))]
    public static class ZoneRpcStartPatch
    {
        [HarmonyPostfix]
        public static void Postfix() => ZoneRpc.EnsureRegistered();
    }
}
