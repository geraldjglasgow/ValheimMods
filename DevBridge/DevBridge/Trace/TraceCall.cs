using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace DevBridge.Trace
{
    /// <summary>One call a tracepoint kept: when, how long, on what, with which arguments, and how it ended.</summary>
    internal sealed class TraceCall
    {
        internal Tracepoint Point;
        internal int N;
        internal bool OnMain;
        internal float GameTime = -1f;
        internal int Frame = -1;
        internal string ThreadName;
        internal string Instance;
        internal string[] Args;
        internal string[] Stack;
        internal long Started;
        internal double Ms;
        internal string Result;
        internal string Error;
        internal bool Skipped;
        internal bool Ended;

        /// <summary>What where= is matched against: the instance and the arguments.</summary>
        internal string Subject => Instance + " " + string.Join(" ", Args);

        internal Dictionary<string, object> ToJson()
        {
            var json = new Dictionary<string, object> { ["n"] = N };
            if (OnMain) { json["time"] = Fmt.R(GameTime); json["frame"] = Frame; }
            else json["thread"] = ThreadName;
            json["ms"] = Math.Round(Ms, 3);
            if (Instance != null) json["instance"] = Instance;
            json["args"] = Args;
            if (Result != null) json["result"] = Result;
            if (Error != null) json["exception"] = Error;
            if (Skipped) json["skipped"] = "a prefix skipped the original method";
            if (Stack != null) json["callers"] = Stack;
            return json;
        }

        /// <summary>The call as one log line: #n time frame ms instance (args) -> result.</summary>
        internal string ToLine()
        {
            var line = new StringBuilder("#").Append(N).Append(' ');
            line.Append(OnMain ? FormattableString.Invariant($"t={Fmt.R(GameTime)} f={Frame} ") : $"thread {ThreadName} ");
            line.Append(Math.Round(Ms, 3).ToString(CultureInfo.InvariantCulture)).Append("ms ");
            if (Instance != null) line.Append(Instance).Append(' ');
            line.Append('(').Append(string.Join(", ", Args)).Append(')');
            if (Result != null) line.Append(" -> ").Append(Result);
            if (Error != null) line.Append(" threw ").Append(Error);
            if (Skipped) line.Append(" (original skipped)");
            if (Stack != null) line.Append(" from ").Append(string.Join(" < ", Stack));
            return line.ToString();
        }
    }
}
