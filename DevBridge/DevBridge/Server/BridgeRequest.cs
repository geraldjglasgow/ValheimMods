using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net;
using System.Threading;
using Newtonsoft.Json;

namespace DevBridge.Server
{
    /// <summary>One HTTP call: its path and arguments, and the reply the main thread fills in.</summary>
    internal sealed class BridgeRequest
    {
        private readonly Dictionary<string, string> args;
        private readonly ManualResetEvent done = new ManualResetEvent(false);

        internal string Path { get; }
        internal Reply Result { get; private set; }

        /// <summary>When the call came in: the HTTP wait (Patience) runs from here, not from when the main thread picks it up.</summary>
        internal readonly DateTime Arrived = DateTime.UtcNow;
        internal bool Answered => Result != null;

        private BridgeRequest(string path, Dictionary<string, string> args)
        {
            Path = path;
            this.args = args;
        }

        /// <summary>Arguments come from the query string and, for a POST, from a form body (curl --data-urlencode).</summary>
        internal static BridgeRequest From(HttpListenerRequest request)
        {
            var args = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            Form.Parse(request.Url.Query.TrimStart('?'), args);
            if (request.HasEntityBody) ReadBody(request, args);
            return new BridgeRequest(request.Url.AbsolutePath.TrimEnd('/').ToLowerInvariant(), args);
        }

        /// <summary>A call made inside the bridge (a scenario step), read with the same path and argument rules as an HTTP call.</summary>
        internal static BridgeRequest Make(string path, IDictionary<string, string> args)
        {
            var copy = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (KeyValuePair<string, string> pair in args) copy[pair.Key] = pair.Value ?? "";
            return new BridgeRequest(("/" + path.Trim().TrimStart('/')).TrimEnd('/').ToLowerInvariant(), copy);
        }

        private static void ReadBody(HttpListenerRequest request, Dictionary<string, string> args)
        {
            string body;
            using (var reader = new StreamReader(request.InputStream, request.ContentEncoding)) body = reader.ReadToEnd();
            if ((request.ContentType ?? "").Contains("x-www-form-urlencoded")) Form.Parse(body, args);
            else args["body"] = body;
        }

        internal bool Has(string name) => args.ContainsKey(name);

        internal string Get(string name, string fallback = null) =>
            args.TryGetValue(name, out string value) && value.Length > 0 ? value : fallback;

        internal int Int(string name, int fallback) =>
            int.TryParse(Get(name), NumberStyles.Integer, CultureInfo.InvariantCulture, out int value) ? value : fallback;

        internal float Float(string name, float fallback) =>
            float.TryParse(Get(name), NumberStyles.Float, CultureInfo.InvariantCulture, out float value) ? value : fallback;

        /// <summary>True for "1", "true" or "yes", and for a bare flag with no value (?all).</summary>
        internal bool Flag(string name)
        {
            if (!args.TryGetValue(name, out string value)) return false;
            value = value.ToLowerInvariant();
            return value.Length == 0 || value == "1" || value == "true" || value == "yes";
        }

        internal string Require(string name) =>
            Get(name) ?? throw new BridgeException($"missing argument {name}=");

        /// <summary>How long the HTTP thread waits for the main thread: timeout= or seconds= (a scenario 300 s by default; /perf both), plus a margin.</summary>
        internal TimeSpan Patience
        {
            get
            {
                // /perf probes every mod before it samples seconds=, so its wait is both
                float wait = Path == "/perf" ? Float("seconds", 5f) + Float("timeout", 30f)
                    : Math.Max(Float("timeout", Path == "/scenario" ? 300f : 30f), Float("seconds", 0f));
                return TimeSpan.FromSeconds(Math.Min(600f, wait + 5f));
            }
        }

        internal void Json(object value) =>
            Finish(new Reply(200, "application/json", JsonConvert.SerializeObject(value, Formatting.Indented)));

        internal void Text(string text) => Finish(new Reply(200, "text/plain", text));

        internal void Fail(string message, int status = 400) => Finish(Reply.Error(status, message));

        internal void Finish(Reply reply)
        {
            if (Result != null) return;
            Result = reply;
            done.Set();
        }

        internal bool Wait(TimeSpan patience) => done.WaitOne(patience);
    }

    /// <summary>application/x-www-form-urlencoded parsing, which is also the query string format.</summary>
    internal static class Form
    {
        internal static void Parse(string text, IDictionary<string, string> into)
        {
            if (string.IsNullOrEmpty(text)) return;
            foreach (string pair in text.Split('&'))
            {
                if (pair.Length == 0) continue;
                int equals = pair.IndexOf('=');
                string key = Decode(equals < 0 ? pair : pair.Substring(0, equals));
                into[key] = equals < 0 ? "" : Decode(pair.Substring(equals + 1));
            }
        }

        private static string Decode(string text) => Uri.UnescapeDataString(text.Replace('+', ' '));
    }
}
