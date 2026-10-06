using System.Collections.Generic;
using OpenKeep.Core;
using UnityEngine;

namespace OpenKeep.Blueprints.Sites
{
    /// <summary>
    /// The deliverer's side of <see cref="SiteDelivery"/>'s requests: every delivery sent to another machine gets an id
    /// from a counter and waits for its answer with its materials held (<see cref="SiteParcel"/>). Yes keeps them with
    /// the site; "not owner" is sent once more to the post's current owner (or put in the store here when the post is
    /// now this machine's or nobody's); any other no gives them back. Without an answer after <see cref="Timeout"/>
    /// seconds the player is told, and the delivery waits <see cref="LateSeconds"/> more for a late answer, since the
    /// owner may have taken the materials and only its answer is slow; after that, or when the post is gone, the
    /// materials come back. Checked every frame by the sites' update hook.
    /// </summary>
    internal static class SiteDeliveries
    {
        private const float Timeout = 5f;
        private const float LateSeconds = 30f;

        private sealed class Pending
        {
            public long Id;
            public SiteMarker Site;
            public SiteParcel Parcel;
            public ZPackage Packet;
            public float SentAt;
            public bool Retried;
            public bool Late;
        }

        private static readonly Dictionary<long, Pending> pending = new Dictionary<long, Pending>();
        private static readonly List<Pending> expired = new List<Pending>();
        private static long nextId = 1;

        /// <summary>A delivery still waits for its answer (the runner keeps ticking for it).</summary>
        public static bool Waiting => pending.Count > 0;

        public static void Send(SiteMarker site, SiteParcel parcel)
        {
            long id = nextId++;
            ZPackage pkg = new ZPackage();
            pkg.Write(id);
            SiteDelivery.WriteGiven(pkg, parcel.Given);
            pending[id] = new Pending { Id = id, Site = site, Parcel = parcel, Packet = pkg, SentAt = Time.time };
            site.View.InvokeRPC(SiteDelivery.RpcName, pkg);
        }

        /// <summary>OpenKeep_SiteDelivered on the deliverer.</summary>
        public static void Receive(long sender, ZPackage pkg)
        {
            long id = pkg.ReadLong();
            bool ok = pkg.ReadBool();
            string reason = pkg.ReadString();
            if (!pending.TryGetValue(id, out Pending request))
                return;
            Plugin.Log.LogDebug($"OpenKeep: site delivery {id} answered{(request.Late ? " late" : "")} by peer {sender}: {(ok ? "yes" : "no, " + reason)}");
            if (!ok && reason == SiteDelivery.NotOwner && !request.Retried && !request.Late && request.Site != null)
            {
                Retry(request);
                return;
            }
            pending.Remove(id);
            Settle(request, ok);
        }

        private static void Retry(Pending request)
        {
            request.Retried = true;
            request.SentAt = Time.time;
            if (!SiteDelivery.TryHere(request.Site, request.Parcel))
            {
                request.Site.View.InvokeRPC(SiteDelivery.RpcName, request.Packet);
                return;
            }
            pending.Remove(request.Id);
        }

        /// <summary>The answer: told as usual, or, for a delivery already reported late, settled quietly.</summary>
        private static void Settle(Pending request, bool taken)
        {
            if (!request.Late)
                SiteDelivery.Told(request.Parcel, taken);
            else if (taken)
                request.Parcel.Keep();
            else
                request.Parcel.Refund();
        }

        /// <summary>Per frame: an unanswered delivery is reported late, a late one whose wait is over (or whose post is gone) comes back.</summary>
        public static void CheckTimeouts()
        {
            if (pending.Count == 0)
                return;
            float now = Time.time;
            foreach (Pending request in pending.Values)
            {
                if (request.Site == null || now - request.SentAt > (request.Late ? LateSeconds : Timeout))
                    expired.Add(request);
            }
            foreach (Pending request in expired)
                Expire(request, now);
            expired.Clear();
        }

        private static void Expire(Pending request, float now)
        {
            if (!request.Late && request.Site != null)
            {
                request.Late = true;
                request.SentAt = now;
                Messages.Center(SiteWords.NoAnswer);
                return;
            }
            pending.Remove(request.Id);
            Plugin.Log.LogInfo($"OpenKeep: site delivery {request.Id} was never answered; the materials are given back");
            request.Parcel.Refund();
        }
    }
}
