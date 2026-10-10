using System;
using System.Collections.Generic;
using System.Reflection;

namespace DevBridge.Server
{
    /// <summary>The HTTP answer to one request.</summary>
    internal sealed class Reply
    {
        internal readonly int Status;
        internal readonly string ContentType;
        internal readonly string Body;

        /// <summary>Extra response headers (the /studio page's framing and content rules).</summary>
        internal readonly Dictionary<string, string> Headers = new Dictionary<string, string>();

        internal Reply(int status, string contentType, string body)
        {
            Status = status;
            ContentType = contentType;
            Body = body ?? "";
        }

        internal static Reply Error(int status, string message) => new Reply(status, "text/plain", "error: " + message);

        /// <summary>A caller mistake (BridgeException) is a 400 with its message; anything else a 500 with the stack.</summary>
        internal static Reply FromException(Exception error)
        {
            if (error is TargetInvocationException wrapped && wrapped.InnerException != null) error = wrapped.InnerException;
            if (error is BridgeException) return Error(400, error.Message);
            return Error(500, error.GetType().Name + ": " + error.Message + "\n" + error.StackTrace);
        }
    }

    /// <summary>Thrown for bad arguments or a missing target; the message goes back to the caller as is.</summary>
    internal sealed class BridgeException : Exception
    {
        internal BridgeException(string message) : base(message)
        {
        }
    }
}
