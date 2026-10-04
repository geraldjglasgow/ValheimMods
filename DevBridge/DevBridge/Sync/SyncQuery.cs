using System;
using System.Collections.Generic;
using System.Linq;
using DevBridge.Server;

namespace DevBridge.Sync
{
    /// <summary>The one question /sync asks every DevBridge, this one included, and how two answers compare.</summary>
    internal sealed class SyncQuery
    {
        internal string What;
        internal string Path;
        internal Func<PeerAnswer, Dictionary<string, object>> Head;
        internal Func<PeerAnswer, PeerAnswer, Dictionary<string, object>> Compare;

        /// <summary>An /eval error is an answer like any other; for the rest an error means there is nothing to compare.</summary>
        private bool errorsCompare;

        internal bool Usable(PeerAnswer answer) => answer.Ok || (errorsCompare && (answer.Code == 400 || answer.Code == 500));

        internal static SyncQuery Zdo(ZDO zdo, string keys) => new SyncQuery
        {
            What = "zdo",
            Path = "/zdo?full=1&id=" + Escape(zdo.m_uid.ToString()) + (keys == null ? "" : "&keys=" + Escape(keys)),
            Head = ZdoCompare.Head,
            Compare = ZdoCompare.Compare,
        };

        internal static SyncQuery Eval(string expr, BridgeRequest request) => new SyncQuery
        {
            What = "eval",
            Path = "/eval?expr=" + Escape(expr) + Passed(request, "members", "methods", "filter"),
            Head = here => new Dictionary<string, object> { ["expr"] = expr, ["value"] = TextCompare.Result(here) },
            Compare = TextCompare.Compare,
            errorsCompare = true,
        };

        internal static SyncQuery Config(string mod, string section) => new SyncQuery
        {
            What = "config",
            Path = "/config?mod=" + Escape(mod) + (section == null ? "" : "&section=" + Escape(section)),
            Head = ConfigCompare.Head,
            Compare = ConfigCompare.Compare,
        };

        private static string Passed(BridgeRequest request, params string[] names) =>
            string.Concat(names.Where(request.Has).Select(name => "&" + name + "=" + Escape(request.Get(name, ""))));

        private static string Escape(string text) => Uri.EscapeDataString(text);
    }
}
