using System;
using System.Globalization;
using DevBridge.Server;

namespace DevBridge.Events
{
    /// <summary>What /events was asked: where to start, the filter, how many, how long to wait or stream.</summary>
    internal sealed class EventQuery
    {
        private const float MaxWait = 600f;
        private const int MaxEvents = 4000;

        internal long Since;
        internal bool Reset;
        internal EventFilter Filter;
        internal int Max;
        internal float Wait;
        internal float Seconds;

        internal static EventQuery From(BridgeRequest request)
        {
            var query = new EventQuery
            {
                Filter = new EventFilter(request.Get("kinds"), request.Get("grep")),
                Max = Math.Min(MaxEvents, Math.Max(1, request.Int("max", 100))),
                Wait = Math.Min(MaxWait, Math.Max(0f, request.Float("wait", 0f))),
                Seconds = Math.Max(0f, request.Float("seconds", 0f)),
            };
            query.Since = query.Start(request.Get("since"));
            return query;
        }

        /// <summary>since= absent is now, 0 the oldest kept event, -N the last N; a number past the log's end (a cursor
        /// from before the game restarted) reads from now and says so.</summary>
        private long Start(string since)
        {
            long next = EventLog.Next;
            if (since == null) return next;
            if (!long.TryParse(since, NumberStyles.Integer, CultureInfo.InvariantCulture, out long seq))
                throw new BridgeException("since= takes next= from the last reply, 0 for every kept event, or -N for the last N");
            if (seq < 0) return Math.Max(EventLog.Oldest, next + seq);
            if (seq == 0) return EventLog.Oldest;
            Reset = seq > next;
            return Reset ? next : seq;
        }
    }
}
