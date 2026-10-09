using HarmonyLib;
using OpenKeep.Core;

namespace OpenKeep.Blueprints.Bench
{
    /// <summary>
    /// The Blueprint Bench's routed RPCs. Requests go to the server (<see cref="BenchServer"/>), which keeps the pool and
    /// answers the asking machine only; a change to the pool is announced to everybody (<see cref="Changed"/>), so open
    /// bench windows list it again. A machine takes answers only from the server. Registered on every machine when the
    /// network starts; the pool needs OpenKeep on the server.
    /// </summary>
    public static class BenchRpc
    {
        public const string List = "OpenKeep_BenchList";
        public const string Listing = "OpenKeep_BenchListing";
        public const string Put = "OpenKeep_BenchPut";
        public const string PutDone = "OpenKeep_BenchPutDone";
        public const string Get = "OpenKeep_BenchGet";
        public const string File = "OpenKeep_BenchFile";
        public const string Remove = "OpenKeep_BenchRemove";
        public const string Move = "OpenKeep_BenchMove";
        public const string MakeFolder = "OpenKeep_BenchMakeFolder";
        public const string Say = "OpenKeep_BenchSay";
        public const string Changed = "OpenKeep_BenchChanged";

        public static void Register(ZRoutedRpc rpc)
        {
            rpc.Register(List, sender => Guard("list", () => BenchServer.OnList(sender)));
            rpc.Register<ZPackage>(Put, (sender, part) => Guard("share", () => BenchServer.OnPut(sender, part)));
            rpc.Register<long, string>(Get, (sender, owner, path) => Guard("take", () => BenchServer.OnGet(sender, owner, path)));
            rpc.Register<long, string, bool>(Remove,
                (sender, owner, path, folder) => Guard("remove", () => BenchServer.OnRemove(sender, owner, path, folder)));
            rpc.Register<long, string, string, bool>(Move,
                (sender, owner, from, to, folder) => Guard("move", () => BenchServer.OnMove(sender, owner, from, to, folder)));
            rpc.Register<long, string>(MakeFolder, (sender, owner, path) => Guard("folder", () => BenchServer.OnMakeFolder(sender, owner, path)));
            rpc.Register<ZPackage>(Listing, (sender, pool) => Guard("listing", () => BenchPool.OnListing(sender, pool)));
            rpc.Register<string, string>(PutDone, (sender, path, word) => Guard("shared", () => BenchShare.OnDone(sender, path, word)));
            rpc.Register<ZPackage>(File, (sender, part) => Guard("file", () => BenchTake.OnFile(sender, part)));
            rpc.Register<string, string, string>(Say, (sender, word, a, b) => Guard("message", () => OnSay(sender, word, a, b)));
            rpc.Register(Changed, sender => BenchPool.MarkStale());
        }

        /// <summary>The answer came from the server (the only machine that answers bench requests).</summary>
        public static bool FromServer(long sender) => ZRoutedRpc.instance != null && sender == ZRoutedRpc.instance.GetServerPeerID();

        /// <summary>Server: a message for the asking player, a word with up to two values, shown in their language.</summary>
        public static void Tell(long target, string word, string a = "", string b = "") =>
            ZRoutedRpc.instance.InvokeRoutedRPC(target, Say, word, a ?? "", b ?? "");

        private static void OnSay(long sender, string word, string a, string b)
        {
            if (FromServer(sender) && word.StartsWith("$ok_"))
                Messages.TopLeft(BlueprintWords.Format(word, a, b));
        }

        private static void Guard(string what, System.Action action) => BlueprintSafe.Run("OpenKeep blueprint bench " + what, action);
    }

    /// <summary>ZNet.Awake postfix: the routed RPC table is new with every network, so the bench RPCs are registered on it each time.</summary>
    [HarmonyPatch(typeof(ZNet), nameof(ZNet.Awake))]
    public static class BenchRpcPatch
    {
        [HarmonyPostfix]
        public static void Postfix()
        {
            if (ZRoutedRpc.instance != null)
                BlueprintSafe.Run("OpenKeep blueprint bench RPCs", () => BenchRpc.Register(ZRoutedRpc.instance));
        }
    }
}
