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

        /// <summary>How long the HTTP thread waits for the main thread: timeout= or seconds=, plus a margin.</summary>
        internal TimeSpan Patience =>
            TimeSpan.FromSeconds(Math.Min(600f, Math.Max(Float("timeout", 30f), Float("seconds", 0f)) + 5f));

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
