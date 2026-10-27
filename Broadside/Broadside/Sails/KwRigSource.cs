using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// The ship round a rigged piece (<see cref="KwRigShip"/>) read as blueprint pieces in one frame: on a launched ship its
    /// blueprint (<see cref="KwShipBody.Blueprint"/>), the piece placed in its fittings' frame; on the dock the dock's ship
    /// pieces (<see cref="KwBpDock.Ship"/>, or those within <see cref="Around"/> of it off a dock) in the world's, as the
    /// blueprint table would save them, so each screen ties the ropes as the launched ship will. Only where something is
    /// drawn (<see cref="KwShipBody.Drawn"/>): a dedicated server reads nothing.
    /// </summary>
    public static class KwRigSource
    {
        /// <summary>How far round a piece off a dock its ship is read, and the change in it watched.</summary>
        public const float Around = 30f;

        private static readonly List<Piece> Found = new List<Piece>();

        /// <summary>
        /// The ship round a piece; null where nothing is drawn. On the dock, the sign of the pieces round it, and the ship
        /// read before (last) kept while that sign is what it was (known).
        /// </summary>
        public static KwRigShip? Of(Transform piece, long known, KwRigShip? last, out long sign, out bool dock)
        {
            (sign, dock) = (0L, false);
            if (!KwShipBody.Drawn)
                return null;
            KwShipBody? body = piece.GetComponentInParent<KwShipBody>();
            if (body != null)
                return last ?? Launched(piece, body);
            (sign, dock) = (Sign(piece.position), true);
            return last != null && sign == known ? last : Dock(piece.position, piece.rotation);
        }

        /// <summary>The dock's ship round a place a rigged piece stands at, or would (the sail tool's preview).</summary>
        public static KwRigShip Dock(Vector3 position, Quaternion rotation)
        {
            KwBpDockInfo? dock = KwBpDock.Find(position);
            List<KwBpPiece> read = Read(dock != null ? KwBpDock.Ship(dock) : Round(position));
            return new KwRigShip(read, Self(read, position, rotation));
        }

        // A launched ship's blueprint, the piece's place and turn in the frame its pieces are placed in.
        private static KwRigShip? Launched(Transform piece, KwShipBody body)
        {
            KwBlueprint? blueprint = body.Blueprint;
            if (blueprint == null)
                return null;
            Transform? frame = body.transform.Find(KwShipPrefab.Fittings);
            if (frame == null)
                frame = body.transform;
            Vector3 at = frame.InverseTransformPoint(piece.position);
            Quaternion turn = Quaternion.Inverse(frame.rotation) * piece.rotation;
            return new KwRigShip(blueprint.Pieces, Self(blueprint.Pieces, at, turn));
        }

        /// <summary>The rigged piece among the pieces (a mast or a bowsprit standing there), else one standing for it there.</summary>
        public static KwBpPiece Self(List<KwBpPiece> pieces, Vector3 at, Quaternion turn)
        {
            KwBpPiece? self = pieces.Find(p => (p.Position - at).sqrMagnitude < 0.0025f && (p.Prefab == KwSailTool.Made || p.Prefab == KwBowsprit.Made));
            return self ?? new KwBpPiece("", at, turn, new List<KwBpValue>());
        }

        /// <summary>Which ship pieces stand within <see cref="Around"/> of a point, in any order.</summary>
        public static long Sign(Vector3 at)
        {
            Found.Clear();
            Piece.GetAllPiecesInRadius(at, Around, Found);
            long sum = 0;
            foreach (Piece piece in Found)
            {
                ZDO? zdo = KwBpDock.NameOf(piece) != null ? piece.m_nview.GetZDO() : null;
                if (zdo != null)
                    sum += unchecked((long)zdo.m_uid.GetHashCode() * 2654435761L + 1);
            }
            Found.Clear();
            return sum;
        }

        private static List<Piece> Round(Vector3 at)
        {
            List<Piece> round = new List<Piece>();
            Piece.GetAllPiecesInRadius(at, Around, round);
            return round;
        }

        // Ship pieces as blueprint pieces: prefab, place and turn, a generated part's or a hatch's saved values.
        private static List<KwBpPiece> Read(List<Piece> pieces)
        {
            List<KwBpPiece> read = new List<KwBpPiece>(pieces.Count);
            foreach (Piece piece in pieces)
            {
                string? prefab = KwBpDock.NameOf(piece);
                if (prefab == null)
                    continue;
                List<KwBpValue> data = prefab.StartsWith(KwToolPieces.PartPrefix, System.StringComparison.Ordinal) || KwHatch.Opening(prefab) != null
                    ? KwBpData.Capture(piece.m_nview.GetZDO()) : new List<KwBpValue>();
                read.Add(new KwBpPiece(prefab, piece.transform.position, piece.transform.rotation, data));
            }
            return read;
        }
    }

    /// <summary>
    /// A rigged piece's ties kept on it (<see cref="KwRigPlan"/>), made again each time its fixed ropes are (<see cref="Fresh"/>);
    /// on the dock it watches the ship round it (every <see cref="Every"/> seconds while the local player is within
    /// <see cref="Near"/>) and, when a piece comes or goes there (a cabin, a railing, planking), has its mast tie its ropes on
    /// again (<see cref="KwSailRig.Rerig"/>). Only drawn: nothing is sent or saved.
    /// </summary>
    public sealed class KwRigState : MonoBehaviour
    {
        private const float Every = 2f, Near = 40f;

        private KwRigPlan? plan;
        private long sign;
        private float next;
        private static bool warned;

        /// <summary>The piece's ties worked out again from the ship round it as it is now (the ship read again only when changed).</summary>
        public static KwRigPlan Fresh(Transform piece, List<KwSailLayout>? sails)
        {
            KwRigState? state = piece.GetComponent<KwRigState>();
            if (state == null)
                state = piece.gameObject.AddComponent<KwRigState>();
            KwRigShip? ship = state.Read(piece, out bool dock);
            state.plan = new KwRigPlan(ship, sails);
            state.enabled = dock && piece.GetComponent<KwSailRig>() != null;
            state.next = Time.time + Every;
            return state.plan;
        }

        // The ship round the piece; a throw is reported once and leaves the ropes loose rather than the mast unbuilt.
        private KwRigShip? Read(Transform piece, out bool dock)
        {
            dock = false;
            try
            {
                return KwRigSource.Of(piece, sign, plan?.Ship, out sign, out dock);
            }
            catch (System.Exception e)
            {
                if (!warned)
                    Log.Warn($"Broadside: a mast's ropes could not be tied on to its ship: {e}");
                warned = true;
                return null;
            }
        }

        /// <summary>The piece's ties as last worked out (worked out now the first time).</summary>
        public static KwRigPlan Of(Transform piece, List<KwSailLayout>? sails)
        {
            KwRigState? state = piece.GetComponent<KwRigState>();
            return state != null && state.plan != null ? state.plan : Fresh(piece, sails);
        }

        private void Update()
        {
            if (Time.time < next)
                return;
            next = Time.time + Every;
            Player? me = Player.m_localPlayer;
            if (me != null && (me.transform.position - transform.position).sqrMagnitude < Near * Near)
                KwShipBody.Step("Broadside rigging", Watch);
        }

        // The ship round it counted; its mast ties its ropes on again when it changed.
        private void Watch()
        {
            long now = KwRigSource.Sign(transform.position);
            if (now == sign)
                return;
            sign = now;
            KwSailRig.Rerig(transform.position, 0.05f);
        }
    }
}
