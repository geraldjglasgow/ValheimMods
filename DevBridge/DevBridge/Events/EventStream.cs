using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Text;

namespace DevBridge.Events
{
    /// <summary>
    /// /events?stream=1: NDJSON over a chunked response, written on the HTTP thread, each matching event flushed as it
    /// arrives, until the caller hangs up or seconds= pass. After 15 s with nothing to send it writes one space, which
    /// finds a caller that has gone (the write fails) and which JSON readers skip and line watchers never see as a line.
    /// </summary>
    internal static class EventStream
    {
        private static readonly TimeSpan Heartbeat = TimeSpan.FromSeconds(15);

        internal static void Run(EventQuery query, HttpListenerResponse response)
        {
            bool ended = false;
            try
            {
                response.StatusCode = 200;
                response.ContentType = "application/x-ndjson; charset=utf-8";
                response.SendChunked = true;
                Write(response, Opening(query));
                Pump(query, response);
                ended = true;
            }
            catch (Exception)
            {
                // the caller hung up, or the bridge stopped: there is nobody left to tell
            }
            finally
            {
                Finish(response, ended);
            }
        }

        private static void Pump(EventQuery query, HttpListenerResponse response)
        {
            Stopwatch running = Stopwatch.StartNew(), quiet = Stopwatch.StartNew();
            long cursor = query.Since;
            while (query.Seconds <= 0f || running.Elapsed.TotalSeconds < query.Seconds)
            {
                EventPage page = EventLog.Read(cursor, int.MaxValue, query.Filter.Keep);
                cursor = page.Next;
                string lines = Lines(page);
                if (lines.Length == 0 && quiet.Elapsed >= Heartbeat) lines = " ";
                if (lines.Length > 0)
                {
                    Write(response, lines);
                    quiet.Restart();
                }
                EventLog.WaitFor(cursor, Pause(query, running, quiet));
            }
            var end = new Dictionary<string, object> { ["stream"] = "end", ["next"] = cursor, ["seconds"] = query.Seconds };
            Write(response, EventJson.Of(end) + "\n");
        }

        /// <summary>Until the next heartbeat is due, or the end of seconds= if that comes first.</summary>
        private static TimeSpan Pause(EventQuery query, Stopwatch running, Stopwatch quiet)
        {
            TimeSpan pause = Heartbeat - quiet.Elapsed;
            TimeSpan left = TimeSpan.FromSeconds(query.Seconds) - running.Elapsed;
            if (query.Seconds > 0f && left < pause) pause = left;
            return pause > TimeSpan.Zero ? pause : TimeSpan.Zero;
        }

        private static string Lines(EventPage page)
        {
            var text = new StringBuilder();
            if (page.Missed > 0)
            {
                var missed = new Dictionary<string, object> { ["missed"] = page.Missed, ["why"] = "the log dropped them before they were read" };
                text.Append(EventJson.Of(missed)).Append('\n');
            }
            foreach (GameEvent e in page.Events) text.Append(EventJson.Line(e)).Append('\n');
            return text.ToString();
        }

        private static string Opening(EventQuery query)
        {
            var facts = new Dictionary<string, object> { ["stream"] = "open" };
            foreach (KeyValuePair<string, object> part in query.Filter.Describe()) facts[part.Key] = part.Value;
            facts["since"] = query.Since;
            facts["seconds"] = query.Seconds;
            if (query.Reset) facts["reset"] = "since= was past the end of the log (the game restarted?), so this reads from now";
            return EventJson.Of(facts) + "\n";
        }

        private static void Write(HttpListenerResponse response, string text)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(text);
            response.OutputStream.Write(bytes, 0, bytes.Length);
            response.OutputStream.Flush();
        }

        private static void Finish(HttpListenerResponse response, bool ended)
        {
            try
            {
                if (ended) response.Close();
                else response.Abort();
            }
            catch (Exception)
            {
                // already gone
            }
        }
    }
}
