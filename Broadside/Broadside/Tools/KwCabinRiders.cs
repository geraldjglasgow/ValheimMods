using System.Collections.Generic;
using HarmonyLib;
using PatchGuard;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// What stands on a cabin goes with the cabin (the user's requests, 2026-10-25: a removed or undone cabin left its roof
    /// railing in the air; a cabin on a cabin must follow the one under it): the roof railing on it, the cabins standing on its
    /// roof all the way up with their railings, the pieces standing on those roofs (masts) and the ladders standing on any of
    /// them (<see cref="KwCabinLift.Removal"/>). Ladders against its roof's edge from the deck stay: they reach the deck below.
    /// One undo or redo (<see cref="KwUndoWork"/>) keeps what it takes in its own step, so the redo or undo after it builds it
    /// all again with its cabin; a cabin swapped in the same step for another version of itself (an edit undone or redone)
    /// takes nothing: an edit carries in its own step all of its stack that moves (<see cref="KwCabinFollow"/>), so what it
    /// left stands on both versions. A part one step made or built again is never taken twice. Runs on the acting player's
    /// game; everything goes through the game's networked removal.
    /// </summary>
    public sealed class KwCabinRiders
    {
        private readonly KwUndoStep step, made;
        private readonly HashSet<ZDOID> done = new HashSet<ZDOID>();

        /// <summary>For one undo or redo of a step, whose result step is made.</summary>
        public KwCabinRiders(KwUndoStep step, KwUndoStep made)
        {
            this.step = step;
            this.made = made;
        }

        /// <summary>What was taken with the cabins removed, to build again with them.</summary>
        public List<KwUndoPiece> Taken { get; } = new List<KwUndoPiece>();

        /// <summary>
        /// A piece removed the hammer's way (<see cref="KwUndoWork.RemoveOne"/>), and when it is a cabin not swapped for another
        /// version of a cabin (replacing: the piece built in its place, if any), what stands on it too, kept in
        /// <see cref="Taken"/>; false when the piece is kept.
        /// </summary>
        public bool Remove(Piece piece, KwUndoPiece? replacing = null)
        {
            List<Piece> on = replacing != null && KwHouses.IsPart(replacing.Prefab) ? new List<Piece>() : On(piece);
            if (!KwUndoWork.RemoveOne(piece))
                return false;
            foreach (Piece rider in on)
            {
                ZDOID id = rider != null && rider.m_nview != null && rider.m_nview.IsValid() ? rider.m_nview.GetZDO().m_uid : ZDOID.None;
                if (id == ZDOID.None || !done.Add(id) || step.Pieces.Exists(p => p.Id == id) || made.Pieces.Exists(p => p.Id == id))
                    continue;
                KwUndoPiece kept = KwUndoWork.Kept(rider!);
                if (KwUndoWork.RemoveOne(rider!))
                    Taken.Add(kept);
            }
            return true;
        }

        /// <summary>What goes with a cabin taken away (<see cref="KwCabinLift.Removal"/>), still loaded; none for any other piece.</summary>
        public static List<Piece> On(Piece piece)
        {
            List<Piece> on = new List<Piece>();
            if (piece == null || !KwHouses.IsPart(Utils.GetPrefabName(piece.gameObject)))
                return on;
            List<KwStackPart> parts = KwStackPart.Near(piece.transform.position);
            KwStackPart? cabin = parts.Find(part => part.Piece == piece);
            if (cabin?.Cabin == null)
                return on;
            foreach (KwStackPart part in KwCabinLift.Removal(parts, cabin))
            {
                if (part.Piece != null && part.Piece.m_nview != null && part.Piece.m_nview.IsValid())
                    on.Add(part.Piece);
            }
            return on;
        }
    }

    /// <summary>
    /// A cabin removed with the hammer takes what stands on it with it (<see cref="KwCabinRiders.On"/>: its roof railing, the
    /// cabins on its roof all the way up and what is on them): the game's removal aims as it always does; only when it removed
    /// the piece aimed at, a cabin, do they go too, the hammer's way (its owner destroys each; a ward the player has no access
    /// to keeps it). The cabin and all they took are one undo step (<see cref="KwUndo.Removed"/>): Ctrl + Z builds them again,
    /// Ctrl + Y takes them again; a cabin removed as part of a whole blueprint ship (Alt, <see cref="KwBpShipRemoval"/>) is not
    /// kept for undo.
    /// </summary>
    [HarmonyPatch(typeof(Player), "RemovePiece")]
    public static class KwCabinRemoval
    {
        private static readonly List<Piece> Riders = new List<Piece>();
        private static KwUndoPiece? aimed;

        [HarmonyPrefix]
        private static void Prefix(Player __instance) => Guard.Run("Broadside cabin removal", Remember, __instance);

        [HarmonyPostfix]
        private static void Postfix(Player __instance, bool __result) => Guard.Run("Broadside cabin removal", Follow, __instance, __result);

        // What stands on the cabin the game's removal aims at (the same ray and mask; the game checks the reach), and the
        // cabin itself as it is, to build again on undo.
        private static void Remember(Player player)
        {
            Riders.Clear();
            aimed = null;
            if (player != Player.m_localPlayer || GameCamera.instance == null)
                return;
            Transform eye = GameCamera.instance.transform;
            if (!Physics.Raycast(eye.position, eye.forward, out RaycastHit hit, 50f, player.m_removeRayMask))
                return;
            Piece? cabin = hit.collider.GetComponentInParent<Piece>();
            Riders.AddRange(KwCabinRiders.On(cabin!));
            bool whole = (Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt)) && KwBpShipRemoval.ShipOf(cabin).Length > 0;
            if (cabin != null && !whole && KwHouses.IsPart(Utils.GetPrefabName(cabin.gameObject)) && cabin.m_nview != null && cabin.m_nview.IsValid())
                aimed = KwUndoWork.Kept(cabin);
        }

        // The cabin removed: what stood on it taken too, and all of it kept as one undo step.
        private static void Follow(Player player, bool removed)
        {
            List<Piece> riders = new List<Piece>(Riders);
            KwUndoPiece? cabin = aimed;
            Riders.Clear();
            aimed = null;
            if (!removed)
                return;
            List<KwUndoPiece> taken = cabin != null ? new List<KwUndoPiece> { cabin } : new List<KwUndoPiece>();
            foreach (Piece rider in riders)
            {
                if (rider == null || rider.m_nview == null || !rider.m_nview.IsValid())
                    continue;
                KwUndoPiece kept = KwUndoWork.Kept(rider);
                if (KwUndoWork.RemoveOne(rider))
                    taken.Add(kept);
            }
            if (cabin != null)
                KwUndo.Removed(player, taken.Count == 1 ? KwHouses.Cabin : "", taken);
        }
    }
}
