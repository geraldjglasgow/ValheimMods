using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Newtonsoft.Json;

namespace DevBridge.Events
{
    /// <summary>Events as /events sends them: each one a line of JSON (seq, kind, clock, game time, data), made once.</summary>
    internal static class EventJson
    {
        /// <summary>The event as one line; whichever reader needs it first makes it, on its own thread.</summary>
        internal static string Line(GameEvent e) => e.Json ?? (e.Json = Make(e));

        private static string Make(GameEvent e)
        {
            var shape = new Dictionary<string, object>
            {
                ["seq"] = e.Seq,
                ["kind"] = e.Kind,
                ["clock"] = e.Clock.ToString("HH:mm:ss.fff", CultureInfo.InvariantCulture),
            };
            if (e.GameTime.HasValue) shape["time"] = e.GameTime.Value;
            shape["data"] = e.Data;
            return JsonConvert.SerializeObject(shape, Formatting.None);
        }

        /// <summary>Any small object as one line of JSON (the stream's own open, missed and end lines).</summary>
        internal static string Of(Dictionary<string, object> value) => JsonConvert.SerializeObject(value, Formatting.None);

        /// <summary>The long-poll reply: one JSON object, its events one per line so it reads as well as it parses.</summary>
        internal static string Reply(EventPage page, Dictionary<string, object> facts)
        {
            facts["next"] = page.Next;
            facts["more"] = page.More;
            if (page.Missed > 0) facts["missed"] = page.Missed;
            facts["count"] = page.Events.Count;
            string head = Of(facts);
            var text = new StringBuilder(head, 0, head.Length - 1, 256 + page.Events.Count * 200);
            text.Append(",\"events\":[");
            if (page.Events.Count > 0) text.Append('\n').Append(string.Join(",\n", page.Events.Select(Line))).Append('\n');
            return text.Append("]}").ToString();
        }
    }
}
