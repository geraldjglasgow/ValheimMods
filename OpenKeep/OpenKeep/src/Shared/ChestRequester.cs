using System;
using System.Collections.Generic;
using HarmonyLib;
using OpenKeep.Core;
using UnityEngine;

namespace OpenKeep.Shared
{
    /// <summary>
    /// The requester's bookkeeping: every request sent gets an id and waits for its reply. A reply "yes" runs the
    /// granted callback with the owner's payload; "not owner" is retried once (the owner is resolved again at
    /// send time); every other "no" and a missing reply after <c>Request Timeout</c> seconds count as denied with
    /// the centre message of SPEC 9.4. A slot with a request under way is known (<see cref="IsPending"/>) so a
    /// second click on it is ignored instead of being denied. Timeouts are checked after the local player's Update.
    /// </summary>
    public static class ChestRequester
    {
        private sealed class Pending
        {
            public long Id;
            public string Rpc;
            public Container Container;
            public Vector2i Slot;
            public ZPackage Packet;
            public float SentAt;
            public bool Retried;
            public Action<ZPackage> Granted;
            public Action<string> Refused;
        }

        private static readonly Dictionary<long, Pending> pending = new Dictionary<long, Pending>();
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

        public static void Send(Container container, string rpc, Vector2i slot, Action<ZPackage> writeBody, Action<ZPackage> granted, Action<string> refused)
        {
            long id = nextId++;
            ZPackage pkg = ChestRequests.NewRequest(id);
            writeBody(pkg);
            Pending request = new Pending
            {
                Id = id, Rpc = rpc, Container = container, Slot = slot, Packet = pkg, SentAt = Time.time,
                Granted = granted, Refused = refused,
            };
            if (!ChestRequests.Send(container, rpc, id, pkg))
            {
                Fail(request, ChestRequests.Timeout);
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
            Plugin.Log.LogDebug($"OpenKeep: request {id} {request.Rpc} answered by peer {sender}: {(ok ? "yes" : "no, " + reason)}");
            if (ok)
            {
                pending.Remove(id);
                request.Granted(payload);
            }
            else if (reason == ChestRequests.NotOwner && !request.Retried)
                Retry(request);
            else
            {
                pending.Remove(id);
                Fail(request, reason);
            }
        }

        private static void Retry(Pending request)
        {
            request.Retried = true;
            request.SentAt = Time.time;
            Plugin.Log.LogDebug($"OpenKeep: request {request.Id} {request.Rpc} retried after resolving the owner again");
            if (!ChestRequests.Send(request.Container, request.Rpc, request.Id, request.Packet))
            {
                pending.Remove(request.Id);
                Fail(request, ChestRequests.Timeout);
            }
        }

        private static void Fail(Pending request, string reason)
        {
            Messages.Center(MessageFor(reason));
            request.Refused(reason);
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

        /// <summary>Requests without a reply after the timeout, or whose container is gone, count as denied.</summary>
        public static void CheckTimeouts()
        {
            if (pending.Count == 0)
                return;
            float timeout = SharedSettings.RequestTimeout.Value;
            List<Pending> expired = new List<Pending>();
            foreach (Pending request in pending.Values)
            {
                if (request.Container == null || Time.time - request.SentAt > timeout)
                    expired.Add(request);
            }
            foreach (Pending request in expired)
            {
                pending.Remove(request.Id);
                Plugin.Log.LogDebug($"OpenKeep: request {request.Id} {request.Rpc} got no answer within {timeout:0.#} s");
                Fail(request, ChestRequests.Timeout);
            }
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
