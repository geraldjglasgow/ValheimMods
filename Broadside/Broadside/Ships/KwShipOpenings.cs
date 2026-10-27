using System.Collections.Generic;
using PatchGuard;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// A launched ship's openings (doorways, windows, gunports and portholes in its planking and cabins), made from its
    /// blueprint on every peer, a dedicated server included (<see cref="KwShipOpeningPlan"/>, the same numbering
    /// everywhere; a double door is one opening with two leaves). Every covered one's leaves hang on their hinges under the
    /// ship's fittings, a holder per part placed as the part is (<see cref="KwShipOpeningLeaf"/>: a box to bump into
    /// everywhere, the look only where there is a screen).
    /// Whether a covered opening is open lives in the ship's ZDO under its number (<see cref="Key"/>: 0 shut, 1 or -1 open
    /// one way or the other), written only by the ship's owner, asked with one call on the ship (<see cref="Rpc"/>: the
    /// opening's number, the way and the ship's revision it was numbered in), so a ship's openings have no ZDO of their own,
    /// as its hatches have none. Every peer reads the states a few times a second (only while the ship's revision is the one
    /// its openings were numbered in: an edit at sea numbers them again, <see cref="Replan"/>) and swings the leaves toward
    /// them. A ship has no ward of its own and its hatches open for anyone aboard, so its openings do too. The sea comes in
    /// through the open ones low in the hull's sides (<see cref="Leaks"/>, read by <see cref="KwShipFlood"/> on the owner):
    /// one with no cover always, a covered one while it is open; a shut cover is watertight. Made on every ship, one with no
    /// openings too, so a door or window cut at sea is hung and floods like the others.
    /// </summary>
    public sealed class KwShipOpenings : MonoBehaviour
    {
        public const string Rpc = "KW_ShipOpeningUse";
        private const float Poll = 0.1f;

        private ZNetView? view;
        private KwShipOpeningPlan? plan;
        private Transform? holder;
        private readonly Dictionary<int, Transform> parts = new Dictionary<int, Transform>();
        private int[] keys = new int[0], states = new int[0];
        private float polled = -99f;
        private int revision;
        private bool registered;

        /// <summary>Each part's openings fitted once (by its piece), so planning the ship again after an edit fits only the parts changed.</summary>
        public KwShipOpeningCache Cache { get; } = new KwShipOpeningCache();

        /// <summary>The holes the sea comes in through now, in the ship's frame, lowest first (at most <see cref="KwShipOpeningPlan.Most"/>).</summary>
        public List<KwHullHole> Leaks { get; } = new List<KwHullHole>(KwShipOpeningPlan.Most);

        /// <summary>The openings as numbered now.</summary>
        public KwShipOpeningPlan? Plan => plan;

        /// <summary>The ZDO key a launched ship keeps its covered opening n's state under: "kw_ship_open3".</summary>
        public static int Key(int n) => ("kw_ship_open" + n).GetStableHashCode();

        /// <summary>A ship's openings from its blueprint, under its fittings' holder.</summary>
        public static void Add(Transform ship, Transform holder, KwBlueprint blueprint, KwBpHull hull)
        {
            KwShipOpenings openings = ship.gameObject.AddComponent<KwShipOpenings>();
            openings.holder = holder;
            ZNetView? view = ship.GetComponent<ZNetView>();
            openings.Set(KwShipOpeningPlan.Of(blueprint, KwShipTuning.Draught(hull), openings.Cache), view != null && view.IsValid() ? view.GetZDO().GetInt(KwSeaShip.RevHash) : 0);
            openings.Cache.Keep();
            openings.Hang(blueprint, null);
        }

        /// <summary>
        /// The openings numbered again after an edit at sea, at the ship's new revision: the changed parts' leaves hung again,
        /// every other leaf given its new number, the states and the leaks read again.
        /// </summary>
        public void Replan(KwBlueprint blueprint, KwBpHull hull, ICollection<int> changed, int rev)
        {
            KwShipOpeningPlan now = KwShipOpeningPlan.Of(blueprint, KwShipTuning.Draught(hull), Cache);
            Cache.Keep();
            Dictionary<int, int> renumber = plan != null ? KwShipOpeningPlan.Renumber(plan, now) : new Dictionary<int, int>();
            foreach (int index in changed)
            {
                if (parts.TryGetValue(index, out Transform part))
                {
                    parts.Remove(index);
                    part.gameObject.SetActive(false);
                    Destroy(part.gameObject);
                }
            }
            foreach (Transform part in parts.Values)
            {
                foreach (KwShipOpeningLeaf leaf in part.GetComponentsInChildren<KwShipOpeningLeaf>(true))
                    leaf.Renumber(renumber);
            }
            Set(now, rev);
            Hang(blueprint, changed);
        }

        private void Set(KwShipOpeningPlan made, int rev)
        {
            (view, plan, revision) = (GetComponent<ZNetView>(), made, rev);
            (keys, states) = (new int[made.Covered], new int[made.Covered]);
            for (int n = 0; n < keys.Length; n++)
                keys[n] = Key(n);
            if (!registered && view != null && view.IsValid())
            {
                view.Register<int, int, int>(Rpc, (sender, n, way, at) => Guard.Run("Broadside ship opening", () => Use(n, way, at)));
                registered = true;
            }
            Read();
            made.Leaks(states, Leaks);
        }

        // Every leaf on its hinge (only the parts given, when given), under a holder for its part placed as the part is.
        private void Hang(KwBlueprint blueprint, ICollection<int>? only)
        {
            foreach ((int piece, KwLeafPlan leaf, int number, string note) in plan!.Leaves)
            {
                if (only != null && !only.Contains(piece))
                    continue;
                KwBpPiece at = blueprint.Pieces[piece];
                if (!parts.TryGetValue(piece, out Transform part))
                    parts[piece] = part = Part(holder!, at, piece);
                KwShipOpeningLeaf.Hang(part, leaf, this, number, note, KwBiomeTiers.Of(at.Data));
            }
        }

        // A part's holder for its leaves, on the layer the ship's colliders are on, so a shut leaf stops what its wall does.
        private static Transform Part(Transform holder, KwBpPiece piece, int index)
        {
            Transform part = new GameObject("openings" + index) { layer = LayerMask.NameToLayer("vehicle") }.transform;
            part.SetParent(holder, false);
            part.SetLocalPositionAndRotation(piece.Position, piece.Rotation);
            return part;
        }

        /// <summary>A covered opening's state as last read: 0 shut, 1 or -1 open.</summary>
        public int State(int n) => n >= 0 && n < states.Length ? states[n] : 0;

        /// <summary>Asks the ship's owner to open a covered opening (the way given) or shut it, as numbered at this peer's revision.</summary>
        public void Ask(int n, int way)
        {
            if (view != null && view.IsValid())
                view.InvokeRPC(Rpc, n, way, revision);
        }

        // A few times a second: the states read again, and the holes the sea comes in through made again when one changed.
        private void Update()
        {
            if (plan == null || states.Length == 0 || Time.time - polled < Poll)
                return;
            polled = Time.time;
            if (Read())
                plan.Leaks(states, Leaks);
        }

        // The ZDO's states into the array while the ship is at the revision they are numbered for; true when any changed.
        private bool Read()
        {
            ZDO? zdo = view != null && view.IsValid() ? view.GetZDO() : null;
            if (zdo == null || zdo.GetInt(KwSeaShip.RevHash) != revision)
                return false;
            bool changed = false;
            for (int n = 0; n < states.Length; n++)
            {
                int now = zdo.GetInt(keys[n]);
                if (now != states[n])
                    (states[n], changed) = (now, true);
            }
            return changed;
        }

        // On the ship's owner: a shut opening opens the way asked, an open one shuts (its leaks at once); a call numbered in
        // another revision of the ship is left alone.
        private void Use(int n, int way, int at)
        {
            if (view == null || !view.IsValid() || !view.IsOwner() || n < 0 || n >= keys.Length || at != revision)
                return;
            ZDO zdo = view.GetZDO();
            zdo.Set(keys[n], zdo.GetInt(keys[n]) != 0 ? 0 : (way < 0 ? -1 : 1));
            if (Read())
                plan!.Leaks(states, Leaks);
        }

        private void OnDestroy()
        {
            if (registered && view != null)
                view.Unregister(Rpc);
        }
    }
}
