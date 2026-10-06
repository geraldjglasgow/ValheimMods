using System.Collections.Generic;
using EarthWright.Core;
using UnityEngine;

namespace EarthWright.Terrain
{
    /// <summary>What the receiver of an edit did with it.</summary>
    public enum EditAnswer : byte
    {
        Applied = 0,
        /// <summary>An owner guard refused it; the reason travels with the answer and is shown to the sender.</summary>
        Refused = 1,
        /// <summary>The receiver does not own the compiler (ownership moved while the edit travelled) or has not loaded it yet.</summary>
        Retry = 2,
    }

    /// <summary>
    /// Every edit part sent to a compiler carries a request id from a counter, and whoever receives it answers the sender
    /// (<see cref="AnswerRpc"/>): applied, refused (with the reason), or retry. A sender keeps each part until its answer
    /// or <see cref="PendingSeconds"/>; a retry is sent again after <see cref="RetryDelay"/> to whoever owns the compiler
    /// then (an unowned compiler is claimed first, as the game does with one it creates), at most <see cref="MaxAttempts"/>
    /// times, after which the player is told the edit did not arrive. An id already answered is ignored, so duplicate
    /// answers do nothing.
    /// </summary>
    public static class EditAnswers
    {
        public const string AnswerRpc = "EW_EditAnswer";

        private const float PendingSeconds = 10f;
        private const float RetryDelay = 0.25f;
        private const int MaxAttempts = 3;

        private sealed class Pending
        {
            public ZDOID Comp;
            public TerrainEdit Part;
            public int Attempts;
            public float Expires;
            public float RetryAt;
        }

        private static readonly Dictionary<int, Pending> pending = new Dictionary<int, Pending>();
        private static readonly List<Pending> retries = new List<Pending>();
        private static readonly List<int> expired = new List<int>();
        private static ZRoutedRpc registeredOn;
        private static int nextId;
        private static float nextPrune;

        public static string NotDelivered { get; private set; } = "$ew_refused_notdelivered";

        internal static void Initialize()
        {
            NotDelivered = Language.Add("ew_refused_notdelivered", "The ground changed hands before your edit arrived; try again");
            Ticker.OnUpdate("EarthWright edit retries", Tick);
        }

        /// <summary>Registers the answer RPC for this session; a new session forgets every waiting edit.</summary>
        public static void EnsureRegistered()
        {
            ZRoutedRpc rpc = ZRoutedRpc.instance;
            if (rpc == null || registeredOn == rpc)
                return;
            registeredOn = rpc;
            pending.Clear();
            retries.Clear();
            rpc.Register<ZPackage>(AnswerRpc, (sender, pkg) => Safe.Run("EarthWright edit answer", Read, pkg, sender));
        }

        /// <summary>Sender: gives the part a new request id and keeps it until it is answered.</summary>
        internal static void Track(TerrainComp comp, TerrainEdit part, int attempts = 0)
        {
            part.RequestId = ++nextId == 0 ? ++nextId : nextId;
            pending[part.RequestId] = new Pending
            {
                Comp = comp.m_nview.GetZDO().m_uid, Part = part, Attempts = attempts, Expires = Time.time + PendingSeconds,
            };
        }

        /// <summary>Receiver: answers the edit's sender (directly when that is this machine). Id 0 waits for no answer.</summary>
        internal static void Send(TerrainEdit edit, EditAnswer answer, string reason = null)
        {
            if (edit.RequestId == 0)
                return;
            long peer = edit.SenderPeer;
            if (peer == 0L || peer == ZNet.GetUID() || ZRoutedRpc.instance == null)
            {
                OnAnswer(edit.RequestId, answer, reason);
                return;
            }
            EnsureRegistered();
            ZPackage pkg = new ZPackage();
            pkg.Write(edit.RequestId);
            pkg.Write((byte)answer);
            pkg.Write(reason ?? "");
            ZRoutedRpc.instance.InvokeRoutedRPC(peer, AnswerRpc, pkg);
        }

        private static void Read(ZPackage pkg, long sender)
        {
            int id = pkg.ReadInt();
            EditAnswer answer = (EditAnswer)pkg.ReadByte();
            OnAnswer(id, answer, pkg.ReadString());
        }

        private static void OnAnswer(int id, EditAnswer answer, string reason)
        {
            if (!pending.TryGetValue(id, out Pending sent))
                return;
            pending.Remove(id);
            if (answer == EditAnswer.Refused && !string.IsNullOrEmpty(reason))
                Refusals.Show(reason);
            else if (answer == EditAnswer.Retry)
                QueueRetry(sent);
        }

        private static void QueueRetry(Pending sent)
        {
            if (sent.Attempts + 1 >= MaxAttempts)
            {
                Refusals.Show(NotDelivered);
                return;
            }
            sent.RetryAt = Time.time + RetryDelay;
            retries.Add(sent);
        }

        private static void Tick()
        {
            if (retries.Count > 0)
                RunRetries();
            if (Time.time >= nextPrune && pending.Count > 0)
                Prune();
        }

        private static void RunRetries()
        {
            for (int i = retries.Count - 1; i >= 0; i--)
            {
                Pending retry = retries[i];
                if (Time.time < retry.RetryAt)
                    continue;
                retries.RemoveAt(i);
                Resend(retry);
            }
        }

        /// <summary>Sends a retried part again to whoever owns its compiler now; a compiler no longer loaded here ends it.</summary>
        private static void Resend(Pending retry)
        {
            GameObject found = ZNetScene.instance != null ? ZNetScene.instance.FindInstance(retry.Comp) : null;
            TerrainComp comp = found != null ? found.GetComponent<TerrainComp>() : null;
            if (comp == null || comp.m_nview == null || !comp.m_nview.IsValid())
            {
                Refusals.Show(NotDelivered);
                return;
            }
            if (GeneralSettings.DebugLog.Value)
                Plugin.Log.LogInfo($"Edit {retry.Part.Source} sent again to the compiler at {comp.transform.position} (attempt {retry.Attempts + 2})");
            Dispatcher.SendPart(comp, retry.Part, retry.Attempts + 1);
        }

        /// <summary>Forgets parts nobody answered in time (once a second): a receiver that left takes its answer with it.</summary>
        private static void Prune()
        {
            nextPrune = Time.time + 1f;
            expired.Clear();
            foreach (KeyValuePair<int, Pending> pair in pending)
            {
                if (Time.time >= pair.Value.Expires)
                    expired.Add(pair.Key);
            }
            foreach (int id in expired)
                pending.Remove(id);
        }
    }
}
