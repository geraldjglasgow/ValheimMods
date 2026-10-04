using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;

namespace DevBridge.Sync
{
    /// <summary>What one DevBridge on this machine said to a request, or why it said nothing.</summary>
    internal sealed class PeerAnswer
    {
        internal int Port;
        internal int Code;
        internal string Body = "";
        internal bool Refused;
        internal string Failure;

        internal bool Answered => Code != 0;
        internal bool Ok => Code == 200;

        /// <summary>Null after a 200; otherwise the error the bridge sent, or why nothing came back.</summary>
        internal string Problem => Ok ? null : Answered ? Fmt.Clip(Body.StartsWith("error: ") ? Body.Substring(7) : Body, 300) : Failure;
    }

    /// <summary>
    /// Plain HTTP GETs to the DevBridges on 127.0.0.1, one thread per port so a slow or dead port costs no more than
    /// one. Never runs on the game's main thread: it waits for other processes, and for this one's own main thread.
    /// </summary>
    internal static class PeerClient
    {
        internal static List<PeerAnswer> GetAll(IEnumerable<int> ports, string pathAndQuery, int timeoutMs)
        {
            List<PeerAnswer> answers = ports.Select(port => new PeerAnswer { Port = port }).ToList();
            List<Thread> threads = answers
                .Select(answer => new Thread(() => Fill(answer, pathAndQuery, timeoutMs)) { IsBackground = true, Name = "DevBridge peer" })
                .ToList();
            threads.ForEach(thread => thread.Start());
            threads.ForEach(thread => thread.Join());
            return answers;
        }

        /// <summary>Catches everything: an exception escaping a thread of its own would take the game down.</summary>
        private static void Fill(PeerAnswer answer, string pathAndQuery, int timeoutMs)
        {
            try
            {
                Exchange(answer, pathAndQuery, timeoutMs);
            }
            catch (Exception error)
            {
                Fail(answer, error, timeoutMs);
            }
        }

        private static void Exchange(PeerAnswer answer, string pathAndQuery, int timeoutMs)
        {
            var request = (HttpWebRequest)WebRequest.Create($"http://127.0.0.1:{answer.Port}{pathAndQuery}");
            request.Proxy = null;
            request.KeepAlive = false;
            request.Timeout = timeoutMs;
            request.ReadWriteTimeout = timeoutMs;
            try
            {
                using (var response = (HttpWebResponse)request.GetResponse()) Read(answer, response);
            }
            catch (WebException error) when (error.Response is HttpWebResponse response)
            {
                using (response) Read(answer, response);
            }
        }

        private static void Read(PeerAnswer answer, HttpWebResponse response)
        {
            using (var reader = new StreamReader(response.GetResponseStream() ?? Stream.Null, Encoding.UTF8)) answer.Body = reader.ReadToEnd();
            answer.Code = (int)response.StatusCode;
        }

        private static void Fail(PeerAnswer answer, Exception error, int timeoutMs)
        {
            WebExceptionStatus status = error is WebException web ? web.Status : WebExceptionStatus.UnknownError;
            answer.Code = 0;
            answer.Refused = status == WebExceptionStatus.ConnectFailure;
            if (answer.Refused) answer.Failure = "nothing listening";
            else if (status == WebExceptionStatus.Timeout) answer.Failure = $"no answer in {timeoutMs / 1000f:0.#} s";
            else answer.Failure = error.GetType().Name + ": " + error.Message;
        }
    }
}
