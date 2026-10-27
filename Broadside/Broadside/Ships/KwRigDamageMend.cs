using HarmonyLib;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// The hammer's repair on a launched ship's rig (<see cref="KwRigDamage"/>), on the repairing player's game, as the hull's
    /// holes are mended (<see cref="KwShipRepair"/>, whose click asks these). With the hammer out, aiming at a mast, yard, boom, gaff, sail,
    /// bowsprit, post cap or rudder blade within <see cref="Reach"/> m highlights the ship where the game's own ray found
    /// nothing to repair (<c>Player.UpdateWearNTearHover</c>, whose ray passes the rig's hit boxes), and the repair then mends what is aimed at first: a
    /// sail's holes and the notches in its own yard, boom and gaff, a spar's notches (and with them the lashings, hoops and
    /// lacing they broke), a cap's chips, a rudder's chunks; free, as the game's repair is. A fallen mast, bowsprit or cap is put back by the repair aimed at its stump, or
    /// at the deck within <see cref="Step"/> m of where it stood: for what it cost to build (<see cref="KwRigPiece.Cost"/>),
    /// taken only when the ship's owner has put it back (<see cref="KwRigDamage.RaisedRpc"/>). The game's own checks hold
    /// (a workbench in range for a ship's repair, wards).
    /// </summary>
    [HarmonyPatch]
    public static class KwRigDamageMend
    {
        public const float Reach = 12f, Step = 1.5f;
        private static int mask;

        private static int Mask(Player me) => mask != 0 ? mask : mask = me.m_removeRayMask | (1 << KwRigDamageBoxes.Layer);

        [HarmonyPatch(typeof(Player), "UpdateWearNTearHover"), HarmonyPostfix]
        private static void Hovered(Player __instance)
        {
            if (__instance != Player.m_localPlayer || !__instance.InPlaceMode() || GameCamera.instance == null)
                return;
            Collider? aimed = Aimed(__instance, out _, out bool rig);
            KwRigDamage? damage = rig ? aimed!.GetComponentInParent<KwRigDamage>() : null;
            Piece? ship = damage != null ? damage.GetComponent<Piece>() : null;
            // only where the game's own ray found nothing to repair: a piece built aboard behind a sail stays the one repaired
            if (ship == null || __instance.m_hoveringPiece != null)
                return;
            __instance.m_hoveringPiece = ship;
            ship.GetComponent<WearNTear>()?.Highlight();
        }

        /// <summary>
        /// What the hammer aims at nearest, a rig's hit box (or the rudder's blade, then rig is true) or anything the game's
        /// repair ray meets, within reach of the player's eyes, and where; null for nothing.
        /// </summary>
        internal static Collider? Aimed(Player me, out Vector3 point, out bool rig)
        {
            (point, rig) = (Vector3.zero, false);
            Transform eye = GameCamera.instance.transform;
            if (!Physics.Raycast(eye.position, eye.forward, out RaycastHit hit, 50f, Mask(me), QueryTriggerInteraction.Ignore)
                || Vector3.Distance(me.m_eye.position, hit.point) > Reach)
                return null;
            point = hit.point;
            rig = hit.collider.GetComponentInParent<KwShipHitPart>() != null || KwRigHitFrame.Of(hit.collider) != null;
            return hit.collider;
        }

        /// <summary>On the player's game that asked for a piece to be put back, once its owner has: its cost taken.</summary>
        public static void Raised(KwRigDamage damage, int piece)
        {
            Player? me = Player.m_localPlayer;
            KwRigPiece? put = damage.Parts.Get(piece);
            if (me == null || put == null)
                return;
            KwBpCost.Take(me, put.Cost);
            me.Message(MessageHud.MessageType.Center, $"{put.Called} is put back ({KwBpCost.Words(put.Cost)})");
        }
    }

    /// <summary>
    /// What the hammer's repair asks the ship's owner for (<see cref="KwRigDamageMend"/>): a part aimed at mended, or a
    /// fallen piece put back when the player has its cost.
    /// </summary>
    internal static class KwRigMendAsk
    {
        /// <summary>The rig part a hit box or the rudder's blade stands for, mended or put back; false when it is whole.</summary>
        public static bool Part(KwRigDamage damage, Collider aimed, Player me)
        {
            KwShipHitPart? tag = aimed.GetComponentInParent<KwShipHitPart>();
            KwRigPiece? part = damage.Parts.Get(tag != null ? tag.Piece : Rudder(damage, aimed));
            if (part == null)
                return false;
            if (damage.Condition.Fallen(part.Index))
                return Raise(damage, part, me);
            (char type, int which) = What(part, tag);
            return Mend(damage, part.Index, which, type);
        }

        // What a mend of an aimed part takes: a mast's or bowsprit's column's notches, a yard's, boom's or gaff's, a sail's
        // holes, a cap's chips, a rudder's chunks.
        private static (char Type, int Part) What(KwRigPiece piece, KwShipHitPart? tag)
        {
            if (piece.Kind == KwRigPiece.CapKind)
                return (KwRigMark.Chip, -1);
            if (piece.Kind == KwRigPiece.RudderKind)
                return (KwRigMark.Chunk, -1);
            if (tag != null && tag.Kind == KwShipHitRouter.Sail)
                return (KwRigMark.Hole, tag.Part);
            return (KwRigMark.Notch, tag != null && tag.Kind == KwShipHitRouter.Yard ? tag.Part : KwRigSpar.Column);
        }

        // The rudder whose blade a collider is on (its index in the blueprint), or -1.
        private static int Rudder(KwRigDamage damage, Collider aimed)
        {
            Transform? blade = KwRigHitFrame.Of(aimed);
            foreach (KwRigPiece piece in damage.Parts.Pieces)
            {
                if (blade != null && piece.Kind == KwRigPiece.RudderKind && (piece.Piece.Position - blade.parent.localPosition).sqrMagnitude < 0.01f)
                    return piece.Index;
            }
            return -1;
        }

        /// <summary>A fallen piece put back that stood within reach of a point on the deck; false when none did.</summary>
        public static bool Deck(KwRigDamage damage, Vector3 point, Player me)
        {
            Vector3 local = damage.Frame.InverseTransformPoint(point);
            foreach (KwRigPiece piece in damage.Parts.Pieces)
            {
                Vector3 foot = piece.ToShip(piece.Main.A);
                if (damage.Condition.Fallen(piece.Index) && new Vector2(local.x - foot.x, local.z - foot.z).magnitude < KwRigDamageMend.Step)
                    return Raise(damage, piece, me);
            }
            return false;
        }

        /// <summary>Marks of a type on a part asked gone (<see cref="KwRigDamage.Mends"/>); false when it has none.</summary>
        public static bool Mend(KwRigDamage damage, int piece, int part, char type)
        {
            if (!damage.Marks.Exists(m => KwRigDamage.Mends(m, piece, type, part)))
                return false;
            ZPackage package = new ZPackage();
            package.Write(piece);
            package.Write(part);
            package.Write((int)type);
            damage.GetComponent<ZNetView>()?.InvokeRPC(KwRigDamage.MendRpc, package);
            return true;
        }

        /// <summary>A fallen piece put back when the player has its cost (taken once the owner has put it back); else told what is missing.</summary>
        public static bool Raise(KwRigDamage damage, KwRigPiece piece, Player me)
        {
            string? missing = KwBpCost.Missing(me, piece.Cost);
            if (missing != null)
            {
                me.Message(MessageHud.MessageType.Center, $"{piece.Called} needs {missing} to put back");
                return false;
            }
            damage.GetComponent<ZNetView>()?.InvokeRPC(KwRigDamage.RaiseRpc, piece.Index);
            return true;
        }
    }
}
