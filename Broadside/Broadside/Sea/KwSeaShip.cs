using System.Collections.Generic;
using System.Diagnostics;
using PatchGuard;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// A launched ship's edits where it floats (the user, 2026-10-26: "I might want to edit the railing to add another
    /// cannon ... Or I might want to add in a porthole window"), on every peer, a dedicated server too. The editing player's
    /// game asks the ship's owner (<see cref="EditRpc"/>, <see cref="KwSeaAsk"/>); the owner checks, changes the blueprint
    /// the ship was built from in place (<see cref="KwSeaApply"/>), drops the marks of what it replaced
    /// (<see cref="KwSeaMarks"/>), writes it into the ship's ZDO (<see cref="KwShipBody.Key"/>) with the next revision
    /// (<see cref="RevKey"/>) and answers with the cost (<see cref="KwSeaOwner"/>). Every peer reads the revision five times
    /// a second; when it moved, the ZDO's blueprint is copied into the live one entry by entry (<see cref="KwSeaDiff"/>) and
    /// each kind redraws only its own changed parts (<see cref="KwSeaKind.Redraw"/>), then the ship's stats and its ladder
    /// over the side (<see cref="KwSeaRedraw"/>). A peer loading the ship later builds the edited ship from the ZDO.
    /// </summary>
    public sealed class KwSeaShip : MonoBehaviour
    {
        public const string RevKey = "kw_ship_rev", EditRpc = "KW_SeaEdit", EditedRpc = "KW_SeaEdited";
        public static readonly int RevHash = RevKey.GetStableHashCode();
        private const float Poll = 0.2f;

        private readonly System.Collections.Generic.Queue<System.Action> later = new System.Collections.Generic.Queue<System.Action>();
        private float polled;
        private KwKeelLines? keels;

        public ZNetView View { get; private set; } = null!;
        public KwShipBody Body { get; private set; } = null!;
        public Rigidbody? Rigid { get; private set; }

        /// <summary>The blueprint the ship was built from, kept as the ship is now (the same object every component holds).</summary>
        public KwBlueprint Blueprint { get; private set; } = null!;

        public KwBpHull Hull { get; private set; } = null!;

        /// <summary>The revision of the blueprint this peer has drawn.</summary>
        public int Revision { get; private set; }

        /// <summary>Counts up each time this peer redraws an edit.</summary>
        public int Version { get; private set; }

        /// <summary>The blueprint's keels (never edited at sea), which its parts are seated on.</summary>
        public KwKeelLines Keels => keels ??= KwKeelLines.Of(Blueprint);

        public Transform Visual => transform.Find(KwShipPrefab.Visual) ?? transform;
        public Transform Fittings => transform.Find(KwShipPrefab.Fittings) ?? transform;
        public Transform Colliders => transform.Find(KwShipPrefab.Colliders) ?? transform;

        public bool IsOwner => View != null && View.IsValid() && View.IsOwner();

        /// <summary>
        /// Made as the ship's last build step: the calls registered, then the kinds told (<see cref="KwSeaKind.Building"/>),
        /// as steps the build may spread over frames (<see cref="KwShipBody"/>).
        /// </summary>
        public static IEnumerable<bool> Steps(KwShipBody body, KwBlueprint blueprint, KwBpHull hull)
        {
            KwSeaShip ship = Add(body, blueprint, hull);
            foreach (KwSeaKind kind in KwSeaKinds.All)
            {
                foreach (bool step in kind.Building(ship))
                    yield return step;
            }
        }

        // The component and its calls.
        private static KwSeaShip Add(KwShipBody body, KwBlueprint blueprint, KwBpHull hull)
        {
            KwSeaShip ship = body.gameObject.AddComponent<KwSeaShip>();
            (ship.Body, ship.Blueprint, ship.Hull) = (body, blueprint, hull);
            (ship.View, ship.Rigid) = (body.GetComponent<ZNetView>(), body.GetComponent<Rigidbody>());
            ship.Revision = ship.View != null && ship.View.IsValid() ? ship.View.GetZDO().GetInt(RevHash) : 0;
            if (ship.View != null && ship.View.IsValid())
            {
                ship.View.Register<ZPackage, int>(EditRpc, (sender, pkg, token) => Guard.Run("Broadside sea edit", () => KwSeaOwner.Edit(ship, sender, pkg, token)));
                ship.View.Register<int, bool, string, ZPackage>(EditedRpc, (sender, token, ok, words, cost) => Guard.Run("Broadside sea edit answer", () => KwSeaAsk.Answered(ship, token, ok, words, cost)));
            }
            return ship;
        }

        private void OnDestroy()
        {
            if (View != null && View.IsValid())
            {
                View.Unregister(EditRpc);
                View.Unregister(EditedRpc);
            }
        }

        private void Update()
        {
            KwSeaHide.Tick(this);
            if (later.Count > 0)
                KwShipBody.Step("Broadside sea later", later.Dequeue());
            if (Time.time - polled < Poll || View == null || !View.IsValid())
                return;
            polled = Time.time;
            int now = View.GetZDO().GetInt(RevHash);
            if (now != Revision)
                Guard.Run("Broadside sea redraw", () => CatchUp(now));
        }

        // The ZDO's blueprint copied into the live one, and what changed redrawn.
        private void CatchUp(int now)
        {
            Stopwatch watch = Stopwatch.StartNew();
            KwBlueprint? fresh = KwShipData.Unpack(View.GetZDO().GetString(KwShipBody.Key));
            Revision = now;
            if (fresh == null)
                return;
            double unpacked = watch.Elapsed.TotalMilliseconds;
            KwSeaChanges changes = KwSeaDiff.Into(Blueprint, fresh);
            Redraw(changes, unpacked);
        }

        /// <summary>On the owner, its own edit applied: the revision it wrote taken as drawn, and the changes redrawn.</summary>
        internal void Applied(int revision, KwSeaChanges changes)
        {
            Revision = revision;
            Redraw(changes, 0.0);
        }

        // Each kind's own changes redrawn, then what every kind hangs on others, then the ship's stats and ladder.
        private void Redraw(KwSeaChanges changes, double unpacked)
        {
            if (changes.Empty)
                return;
            Stopwatch watch = Stopwatch.StartNew();
            foreach (KwSeaKind kind in KwSeaKinds.All)
            {
                KwSeaChanges mine = changes.Mine(kind);
                if (!mine.Empty)
                    KwShipBody.Step("Broadside sea redraw " + kind.Name, () => kind.Redraw(this, mine));
            }
            foreach (KwSeaKind kind in KwSeaKinds.All)
                KwShipBody.Step("Broadside sea after " + kind.Name, () => kind.After(this, changes));
            KwShipBody.Step("Broadside sea redraw ship", () => KwSeaRedraw.Common(this, changes));
            Version++;
            double ms = watch.Elapsed.TotalMilliseconds;
            if (ms + unpacked > 4.0)
                Log.Info($"Broadside: a ship's edit ({changes}) redrawn in {ms:0.0} ms (unpacked in {unpacked:0.0} ms)");
        }

        private System.Action? ladderLater, shadowLater;

        /// <summary>The ladder over the side placed again (<see cref="KwSeaRedraw.Ladder"/>), as one queued piece of work.</summary>
        internal System.Action LadderLater => ladderLater ??= () => KwSeaRedraw.Ladder(this);

        /// <summary>The shadow's boxes made again (<see cref="KwSeaRedraw.Shadow"/>), as one queued piece of work.</summary>
        internal System.Action ShadowLater => shadowLater ??= () => KwSeaRedraw.Shadow(this);

        /// <summary>Work for a later frame, one a frame in order (a redraw's slower steps, so no frame takes them all).</summary>
        public void Later(System.Action work)
        {
            if (!later.Contains(work))
                later.Enqueue(work);
        }

        /// <summary>A point of the world in the ship's frame as its body stands (where its colliders are).</summary>
        public Vector3 Local(Vector3 world) =>
            Rigid != null ? Quaternion.Inverse(Rigid.rotation) * (world - Rigid.position) : transform.InverseTransformPoint(world);

        /// <summary>A way of the world in the ship's frame as its body stands.</summary>
        public Vector3 LocalWay(Vector3 world) => Rigid != null ? Quaternion.Inverse(Rigid.rotation) * world : transform.InverseTransformDirection(world);

        /// <summary>A point of the ship's frame in the world as its body stands.</summary>
        public Vector3 World(Vector3 local) => Rigid != null ? Rigid.position + Rigid.rotation * local : transform.TransformPoint(local);

        /// <summary>A point of the ship's frame in the world as it is drawn (where ghosts ride).</summary>
        public Vector3 Drawn(Vector3 local) => transform.TransformPoint(local);
    }
}
