using DevBridge.Eval;
using DevBridge.Server;
using DevBridge.Tune;

namespace DevBridge.Routes
{
    /// <summary>/tune: change a value on a prefab and every live copy of it at once, list and revert the changes, print them as code.</summary>
    internal static class TuneRoute
    {
        private const int MaxReply = 60000;

        internal static void Register(Router router) => router.Add("/tune",
            "/tune?prefab=Troll&field=Character.m_runSpeed&value=9\n" +
            "                       a prefab's component member (field= starts with the component), on the prefab and each live instance\n" +
            "/tune?prefab=Troll&item=troll_punch&field=m_attack.m_attackRange&value=*1.2\n" +
            "                       an item's shared data: a creature's attack item (prefab= is the creature), or item=SwordIron alone\n" +
            "                       for a player's; on the item prefab and every loaded copy (creatures, players, containers, world\n" +
            "                       drops), each distinct object once. value= a number, true/false, enum name, text, x,y,z or r,g,b,a;\n" +
            "                       *1.2, /2 and +0.5 change each copy's own value. No value= reads the prefab's value and each\n" +
            "                       distinct live one; members=1&filter=text lists the members at the path\n" +
            "/tune?list=1           the changes: target, path, original, current, copies written\n" +
            "/tune?revert=all|<n>   puts the originals back (on copies spawned since with the change too)\n" +
            "/tune?code=1           the changes as C# lines to paste into a mod\n" +
            "                       local to this machine: an attack is worked out on the attacker's owner, so a tuned creature\n" +
            "                       fights differently only where this machine owns it (single player, or the host for its creatures)",
            Handle);

        private static void Handle(BridgeRequest request)
        {
            if (request.Flag("list")) request.Json(TuneBook.Rows());
            else if (request.Has("revert")) request.Json(TuneBook.Revert(request.Get("revert")));
            else if (request.Flag("code")) request.Text(TuneBook.Code());
            else Target(request, TargetFinder.Find(request.Get("prefab"), request.Get("item"), request.Get("field")));
        }

        private static void Target(BridgeRequest request, TuneTarget target)
        {
            if (request.Flag("members")) request.Text(Members(target, request.Get("filter")));
            else if (request.Has("value")) request.Json(Tuner.Set(target, request.Get("value") ?? throw new BridgeException("value= is empty; give \"\" for empty text")));
            else request.Json(Tuner.Read(target));
        }

        private static string Members(TuneTarget target, string filter)
        {
            object value = target.Path.Read(target.Templates[0]);
            string text = $"{target.Label}: {target.PathText}\n" + Describe.Full(value, true, false, filter);
            return text.Length <= MaxReply ? text : text.Substring(0, MaxReply) + "\n... cut, narrow it with filter=";
        }
    }
}
