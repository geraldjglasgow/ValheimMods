using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using DevBridge.Server;

namespace DevBridge.Events
{
    /// <summary>/events, served on the HTTP thread instead of the main thread, so a long poll or a stream never holds up a
    /// frame and keeps working while the game is busy loading.</summary>
    internal static class EventServe
    {
        /// <summary>A reply for a long poll or the kinds list; null once a stream has written and closed the response.</summary>
        internal static Reply Answer(BridgeRequest request, HttpListenerResponse response)
        {
            EventLog.StartRecording();
            if (string.Equals(request.Get("kinds"), "list", StringComparison.OrdinalIgnoreCase))
                return new Reply(200, "text/plain", EventKinds.List());
            EventQuery query = EventQuery.From(request);
            if (!request.Flag("stream")) return LongPoll(query);
            EventStream.Run(query, response);
            return null;
        }

        private static Reply LongPoll(EventQuery query)
        {
            Stopwatch watch = Stopwatch.StartNew();
            TimeSpan patience = TimeSpan.FromSeconds(query.Wait);
            EventPage page = EventLog.Read(query.Since, query.Max, query.Filter.Keep);
            long missed = page.Missed;
            while (page.Events.Count == 0 && watch.Elapsed < patience)
            {
                EventLog.WaitFor(page.Next, patience - watch.Elapsed);
                page = EventLog.Read(page.Next, query.Max, query.Filter.Keep);
                missed += page.Missed;
            }
            page.Missed = missed;
            var facts = new Dictionary<string, object> { ["waited"] = Fmt.R((float)watch.Elapsed.TotalSeconds) };
            if (query.Reset) facts["reset"] = "since= was past the end of the log (the game restarted?), so this read from now";
            return new Reply(200, "application/json", EventJson.Reply(page, facts));
        }
    }
}
