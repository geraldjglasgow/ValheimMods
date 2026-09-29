using DevBridge.Eval;
using DevBridge.Server;

namespace DevBridge.Routes
{
    /// <summary>/eval: read, call and assign anything in the running game by reflection.</summary>
    internal static class EvalRoute
    {
        private const int MaxReply = 60000;

        internal static void Register(Router router) => router.Add("/eval",
            "/eval?expr=<expression>&members=1&methods=1&filter=text\n" +
            "                       Type.static.member, obj.field, obj.Method(1, \"a\", true), list[0], dict[\"key\"], a.b = 5;\n" +
            "                       private members too; a GameObject or component reaches sibling components by type name\n" +
            "                       ($hover.Character); members=1 lists fields and properties with values, methods=1 signatures;\n" +
            "                       variables: " + Variables.Known,
            Handle);

        private static void Handle(BridgeRequest request)
        {
            object value = Evaluator.Run(request.Get("expr") ?? request.Get("body") ?? throw new BridgeException("give expr="));
            string text = Describe.Full(value, request.Flag("members"), request.Flag("methods"), request.Get("filter"));
            request.Text(text.Length <= MaxReply ? text : text.Substring(0, MaxReply) + "\n... cut, narrow it with filter=");
        }
    }
}
