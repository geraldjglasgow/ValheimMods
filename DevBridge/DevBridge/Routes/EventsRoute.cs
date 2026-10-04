using DevBridge.Events;
using DevBridge.Server;

namespace DevBridge.Routes
{
    /// <summary>/events: what happens in the game (hits, deaths, spawns, the player, bosses, log warnings, console lines and
    /// what other features publish), read with a long poll or streamed, served on the HTTP thread (Events/EventServe).</summary>
    internal static class EventsRoute
    {
        internal static void Register(Router router)
        {
            router.Add("/events",
                "/events?since=N&kinds=death,hit&grep=Troll&wait=30&max=100\n" +
                "                       game events after since= (default: from now; 0 every kept one, -N the last N), oldest first,\n" +
                "                       of kinds= (-kind leaves one out) with grep= text in the event's JSON, waiting up to wait=\n" +
                "                       seconds for one; pass the reply's next= back as since=. kinds=list explains each kind\n" +
                "/events?stream=1&kinds=death&seconds=N\n" +
                "                       the same as NDJSON, one event per line as it happens, until the caller hangs up or\n" +
                "                       seconds= pass (curl -sN); a space every 15 s quiet checks the caller is still there",
                _ => throw new BridgeException("/events is served on the HTTP thread"));
            HttpBridge.ServeDirect("/events", EventServe.Answer);
            EventLog.SetMainThread();
            LogEvents.Install();
        }
    }
}
