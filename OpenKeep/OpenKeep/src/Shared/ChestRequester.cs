using System;
using System.Collections.Generic;
using HarmonyLib;
using OpenKeep.Core;
using UnityEngine;

namespace OpenKeep.Shared
{
    /// <summary>
    /// The requester's bookkeeping: every request sent gets an id from a counter and waits for its reply. A reply
    /// "yes" applies the item side of the request (<c>apply</c>: what the owner sent goes into the inventory, what it
    /// did not take comes back) and reports through <c>done</c>; "not owner" is retried once (the owner is resolved
    /// again at send time); every other "no" undoes the item side (<c>undo</c>: what was held back comes back) and
    /// counts as denied with the centre message of SPEC 9.4. A request without a reply after <c>Request Timeout</c>
    /// seconds is reported as not answered, but it stays known for <see cref="LateSeconds"/> more: the owner may have
    /// applied it and only its answer is slow, so a late "yes" still applies the item side (a take still arrives, a
    /// put still keeps what the chest took) and a late "no", or no answer at all, undoes it. Nothing is ever applied or
    /// undone twice. A slot with a request under way, late ones included, is known (<see cref="IsPending"/>) so a second
    /// click on it is ignored instead of being denied. Timeouts are checked after the local player's Update.
    /// </summary>
    public static class ChestRequester
    {
        /// <summary>How long a request that was not answered in time still waits for a late answer, seconds.</summary>
        public const float LateSeconds = 30f;

        private sealed class Pending
        {
            public long Id;
            public string Rpc;
            public Container Container;
            public Vector2i Slot;
            public ZPackage Packet;
            public float SentAt;
            public bool Retried;
            public bool Late;
            public Func<ZPackage, bool> Apply;
            public Action Undo;
            public Action<bool> Done;
        }

        private static readonly Dictionary<long, Pending> pending = new Dictionary<long, Pending>();
        private static readonly List<Pending> expired = new List<Pending>();
        private static long nextId = 1;

        /// <summary>A request that concerns this slot of the container is waiting for its reply.</summary>
        public static bool IsPending(Container container, Vector2i slot)
        {
            foreach (Pending request in pending.Values)
            {
                if (request.Container == container && request.Slot == slot)
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Sends a request. <paramref name="apply"/> takes the owner's payload on "yes" and returns the result for
        /// <paramref name="done"/>; <paramref name="undo"/> (may be null) returns what the request held back on "no".
        /// </summary>
        public static void Send(Container container, string rpc, Vector2i slot, Action<ZPackage> writeBody, Func<ZPackage, bool> apply,
            Action undo, Action<bool> done)
        {
            long id = nextId++;
            ZPackage pkg = ChestRequests.NewRequest(id);
            writeBody(pkg);
            Pending request = new Pending
            {
                Id = id, Rpc = rpc, Container = container, Slot = slot, Packet = pkg, SentAt = Time.time,
                Apply = apply, Undo = undo, Done = done,
            };
            if (!ChestRequests.Send(container, rpc, id, pkg))
            {
                Refuse(request, ChestRequests.Timeout);
                return;
            }
            pending[id] = request;
        }

        /// <summary>OpenKeep_Reply on the requester.</summary>
        public static void Receive(Container container, long sender, ZPackage pkg)
        {
            long id = pkg.ReadLong();
            bool ok = pkg.ReadBool();
            string reason = pkg.ReadString();
            ZPackage payload = pkg.ReadPackage();
            if (!pending.TryGetValue(id, out Pending request))
            {
                Plugin.Log.LogDebug($"OpenKeep: reply {id} from peer {sender} matches no pending request");
                return;
            }
            Plugin.Log.LogDebug($"OpenKeep: request {id} {request.Rpc} answered{(request.Late ? " late" : "")} by peer {sender}: {(ok ? "yes" : "no, " + reason)}");
            if (ok)
                Grant(request, payload);
            else if (reason == ChestRequests.NotOwner && !request.Retried && !request.Late)
                Retry(request);
            else
            {
                pending.Remove(id);
                Refuse(request, reason);
            }
        }

        private static void Grant(Pending request, ZPackage payload)
        {
            pending.Remove(request.Id);
            bool result = request.Apply(payload);
            if (!request.Late)
                request.Done(result);
        }

        private static void Retry(Pending request)
        {
            request.Retried = true;
            request.SentAt = Time.time;
            Plugin.Log.LogDebug($"OpenKeep: request {request.Id} {request.Rpc} retried after resolving the owner again");
            if (!ChestRequests.Send(request.Container, request.Rpc, request.Id, request.Packet))
            {
                pending.Remove(request.Id);
                Refuse(request, ChestRequests.Timeout);
            }
        }

        /// <summary>A "no": what the request held back comes back; a request already reported late says nothing more.</summary>
        private static void Refuse(Pending request, string reason)
        {
            request.Undo?.Invoke();
            if (request.Late)
                return;
            Messages.Center(MessageFor(reason));
            request.Done(false);
        }

        private static string MessageFor(string reason)
        {
            switch (reason)
            {
                case ChestRequests.Timeout: return SharedWords.NoAnswer;
                case ChestRequests.ChestFull: return SharedWords.ChestFull;
                case ChestRequests.Nothing: return "$msg_stackall_none";
                default: return SharedWords.Denied;
            }
        }

        /// <summary>
        /// A request without a reply after the timeout is reported as not answered and waits on as a late one; a late
        /// one whose wait is over, or whose container is gone (no answer can arrive any more), is undone and dropped.
        /// </summary>
        public static void CheckTimeouts()
        {
            if (pending.Count == 0)
                return;
            float timeout = SharedSettings.RequestTimeout.Value;
            float now = Time.time;
            foreach (Pending request in pending.Values)
            {
                if (request.Container == null || now - request.SentAt > (request.Late ? LateSeconds : timeout))
                    expired.Add(request);
            }
            foreach (Pending request in expired)
                Expire(request, timeout);
            expired.Clear();
        }

        private static void Expire(Pending request, float timeout)
        {
            if (!request.Late)
            {
                Plugin.Log.LogDebug($"OpenKeep: request {request.Id} {request.Rpc} got no answer within {timeout:0.#} s; waiting {LateSeconds:0} s for a late one");
                Messages.Center(SharedWords.NoAnswer);
                request.Late = true;
                request.SentAt = Time.time;
                request.Done(false);
                if (request.Container != null)
                    return;
            }
            pending.Remove(request.Id);
            Plugin.Log.LogDebug($"OpenKeep: request {request.Id} {request.Rpc} was never answered; what it held back is returned");
            request.Undo?.Invoke();
        }

        [HarmonyPatch(typeof(Player), nameof(Player.Update))]
        private static class TimeoutPatch
        {
            [HarmonyPostfix]
            private static void Postfix(Player __instance)
            {
                if (__instance == Player.m_localPlayer)
                    CheckTimeouts();
            }
        }
    }
}
