using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// An anchor windlass placed on the dock (<see cref="KwAnchor"/>): its anchor shown weighed as it will be on the launched
    /// ship, hanging over the side at its fairlead with the cable led to it from the drum (<see cref="KwAnchorPlan"/>), or,
    /// when no clear place is near, stowed upright on the deck, and its hover says why. The plan is worked out again when
    /// the ship round it changes (pieces added or taken away, a shield hung on a railing or taken off; a part never changes
    /// once built): every <see cref="Every"/> seconds, while the local player is within <see cref="Near"/>, the ship's
    /// pieces are counted. From the dock's pieces (<see cref="KwBpDock.Ship"/>, or those round it off a dock) read as
    /// blueprint pieces, so each screen works out what a launched ship would from its blueprint. Only drawn: off on a
    /// dedicated server; nothing is sent or saved.
    /// </summary>
    public sealed class KwAnchorDock : MonoBehaviour, Hoverable
    {
        private const float Every = 2f, Near = 40f, Around = 30f;
        private static readonly int SpanHash = KwGenerated.Key.GetStableHashCode();
        private static readonly List<Piece> Found = new List<Piece>();

        private ZNetView? view;
        private Transform? anchor;
        private GameObject? fairlead;
        private LineRenderer? cable;
        private KwAnchorLead? lead;
        private long sign = long.MinValue;
        private float next;

        private void Awake()
        {
            view = GetComponent<ZNetView>();
            anchor = transform.Find(KwAnchor.AnchorName);
            next = Random.Range(0.2f, 1f);
            if (!KwShipBody.Drawn)
                enabled = false;
        }

        private void Update()
        {
            if (Time.time < next || view == null || !view.IsValid())
                return;
            next = Time.time + Every;
            Player? me = Player.m_localPlayer;
            if (me != null && (me.transform.position - transform.position).sqrMagnitude < Near * Near)
                KwShipBody.Step("Broadside anchor lead", Refresh);
        }

        // The ship round it counted; planned and drawn again when it changed.
        private void Refresh()
        {
            Found.Clear();
            Piece.GetAllPiecesInRadius(transform.position, Around, Found);
            long now = Sign(Found);
            Found.Clear();
            if (now == sign)
                return;
            sign = now;
            lead = Plan();
            Draw();
        }

        // Which ship pieces stand round it, in any order, and the shields hung on their railings (a fairlead keeps from them).
        private static long Sign(List<Piece> pieces)
        {
            long sum = 0;
            foreach (Piece piece in pieces)
            {
                ZDO? zdo = KwBpDock.NameOf(piece) != null ? piece.m_nview.GetZDO() : null;
                if (zdo != null)
                    sum += unchecked((long)zdo.m_uid.GetHashCode() * 2654435761L + 1 + zdo.GetString(KwShieldRecord.Hash).GetStableHashCode());
            }
            return sum;
        }

        // The lead from the dock's ship pieces (or those round it), read as blueprint pieces in the world's frame.
        private KwAnchorLead Plan()
        {
            KwBpDockInfo? dock = KwBpDock.Find(transform.position);
            List<Piece> pieces = dock != null ? KwBpDock.Ship(dock) : Round();
            List<KwBpPiece> read = new List<KwBpPiece>(pieces.Count + 1);
            KwBpPiece? self = null;
            foreach (Piece piece in pieces)
            {
                KwBpPiece? one = Read(piece);
                if (one != null)
                    read.Add(one);
                if (piece.gameObject == gameObject)
                    self = one;
            }
            if (self == null)
                read.Add(self = new KwBpPiece(KwAnchor.Prefab, transform.position, transform.rotation, new List<KwBpValue>()));
            return KwAnchorPlan.For(read, self);
        }

        private List<Piece> Round()
        {
            List<Piece> round = new List<Piece>();
            Piece.GetAllPiecesInRadius(transform.position, Around, round);
            return round;
        }

        // A ship piece as a blueprint piece: its prefab, its place and turn, a generated part's saved values.
        private static KwBpPiece? Read(Piece piece)
        {
            string? prefab = KwBpDock.NameOf(piece);
            if (prefab == null)
                return null;
            List<KwBpValue> data = prefab.StartsWith(KwToolPieces.PartPrefix, System.StringComparison.Ordinal) || KwHatch.Opening(prefab) != null
                ? KwBpData.Capture(piece.m_nview.GetZDO()) : new List<KwBpValue>();
            return new KwBpPiece(prefab, piece.transform.position, piece.transform.rotation, data);
        }

        // The anchor over the side at its fairlead with its cable, or stowed on the deck.
        private void Draw()
        {
            if (fairlead != null)
                Destroy(fairlead);
            fairlead = null;
            if (lead == null || !lead.Found)
            {
                if (anchor != null)
                    KwAnchorLook.Stow(anchor);
                if (cable != null)
                    cable.enabled = false;
                return;
            }
            fairlead = KwAnchorLook.Fairlead(transform, lead);
            cable ??= KwAnchorLook.Cable(transform);
            cable.enabled = true;
            KwAnchorLook.Set(cable, transform, lead.Cable(true));
            if (anchor != null)
                KwAnchorLook.Hang(anchor, lead);
        }

        public string GetHoverText()
        {
            string line = lead == null ? "" : lead.Found
                ? $"\nIts anchor hangs over the side at the fairlead ({KwAnchorRoute.Length(lead.Deck):0.0} m of cable on deck)"
                : $"\nIts anchor stays on the deck: {lead.Why}";
            return "Anchor windlass" + line + "\nOn a launched ship, E drops or weighs the anchor";
        }

        public string GetHoverName() => "Anchor windlass";

        public float GetHoverOffset() => 0f;
    }
}
