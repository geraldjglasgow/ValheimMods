using System.Collections.Generic;
using Splatform;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// The work of an undo and a redo (<see cref="KwUndo"/>). Removing goes the hammer's way: a piece's
    /// <see cref="WearNTear.Remove"/> asks its owner to destroy it, with no drops (design is free); a piece without one is
    /// claimed by the player and destroyed. Only pieces this player made are touched, and pieces under a ward the player
    /// has no access to, or that will not be removed now, are kept. Before a piece goes, what it is (prefab, place, turn and
    /// its saved ZDO values) is kept; a redo instantiates it again there with those values and this player as creator, as a
    /// blueprint build does (<see cref="KwBpJobs"/>), and only for Broadside ship pieces. An edit's step is swapped instead
    /// (<see cref="Swap"/>): each new part taken away and the one it replaced built again in its place, or back; a removal
    /// step (<see cref="KwUndoStep.Removal"/>) swaps the same way, nothing in place of what it took. A cabin taken away takes
    /// what stands on it (its roof railing, the cabins on its roof and what is on them), kept in the same step to come back
    /// with it (<see cref="KwCabinRiders"/>).
    /// </summary>
    public static class KwUndoWork
    {
        /// <summary>The action's pieces that still stand in the loaded world and were made by this player.</summary>
        public static List<Piece> Standing(KwUndoStep step, long creator)
        {
            List<Piece> standing = new List<Piece>();
            foreach (KwUndoPiece made in step.Pieces)
            {
                Piece? piece = StandingOne(made, creator);
                if (piece != null)
                    standing.Add(piece);
            }
            return standing;
        }

        // One piece a step made, while it stands in the loaded world and was made by this player; else null.
        private static Piece? StandingOne(KwUndoPiece made, long creator)
        {
            GameObject? found = ZNetScene.instance != null ? ZNetScene.instance.FindInstance(made.Id) : null;
            Piece? piece = found != null ? found.GetComponent<Piece>() : null;
            return piece != null && piece.m_nview != null && piece.m_nview.IsValid() && piece.GetCreator() == creator ? piece : null;
        }

        /// <summary>
        /// An edit undone or redone: each of the step's pieces still standing is taken away and the one it replaced built
        /// again in its place. The step they now make, the other way round, ready to swap back; and how many a ward kept. A
        /// piece already gone is passed over, and the one it replaced stays away. A step may be ragged: after its pairs,
        /// pieces it made with nothing replaced (a ladder built with a railing's gap) are taken away, and pieces it took
        /// with nothing in their place are built again; the step made keeps its pairs first, then those.
        /// </summary>
        public static KwUndoStep Swap(Player player, KwUndoStep step, out int kept)
        {
            KwUndoStep swapped = new KwUndoStep(step.Kind, step.Tool, -1) { Removal = step.Removal };
            KwCabinRiders riders = new KwCabinRiders(step, swapped);
            kept = 0;
            int pairs = Mathf.Min(step.Pieces.Count, step.Replaced.Count);
            for (int i = 0; i < pairs; i++)
                kept += SwapPair(player, step.Pieces[i], step.Replaced[i], swapped, riders) ? 0 : 1;
            for (int i = pairs; i < step.Pieces.Count; i++)
            {
                Piece? standing = StandingOne(step.Pieces[i], player.GetPlayerID());
                KwUndoPiece? what = standing != null ? Kept(standing) : null;
                if (standing != null && riders.Remove(standing))
                    swapped.Replaced.Add(what!);
                else if (standing != null)
                    kept++;
            }
            for (int i = pairs; i < step.Replaced.Count; i++)
                kept += BuildInto(player, step.Replaced[i], swapped) ? 0 : 1;
            swapped.Replaced.AddRange(riders.Taken);
            return swapped;
        }

        // One pair: the piece standing taken away and the one it replaced built in its place, both kept in the step made;
        // true when done or the piece was gone already, false when a ward kept it.
        private static bool SwapPair(Player player, KwUndoPiece now, KwUndoPiece was, KwUndoStep swapped, KwCabinRiders riders)
        {
            Piece? standing = StandingOne(now, player.GetPlayerID());
            if (standing == null)
                return true;
            KwUndoPiece what = Kept(standing);
            Transform? made = null;
            ZDO? zdo = riders.Remove(standing, was) ? Build(player, was, out made) : null;
            if (zdo == null || made == null)
                return false;
            swapped.Pieces.Add(new KwUndoPiece(zdo.m_uid, made.position));
            swapped.Replaced.Add(what);
            return true;
        }

        // A piece taken with nothing in its place, built again and kept among the step's pieces; false when it may not be.
        private static bool BuildInto(Player player, KwUndoPiece was, KwUndoStep swapped)
        {
            ZDO? zdo = Build(player, was, out Transform? made);
            if (zdo == null || made == null)
                return false;
            swapped.Pieces.Add(new KwUndoPiece(zdo.m_uid, made.position));
            return true;
        }

        /// <summary>The standing pieces removed: a step of what was removed, ready to build again, and how many were kept.</summary>
        public static KwUndoStep Remove(KwUndoStep step, List<Piece> standing, out int kept)
        {
            KwUndoStep removed = new KwUndoStep(step.Kind, step.Tool, -1);
            KwCabinRiders riders = new KwCabinRiders(step, removed);
            kept = 0;
            foreach (Piece piece in standing)
            {
                KwUndoPiece what = Kept(piece);
                if (riders.Remove(piece))
                    removed.Pieces.Add(what);
                else
                    kept++;
            }
            removed.Pieces.AddRange(riders.Taken);
            return removed;
        }

        /// <summary>What a standing piece is, to build it again: its id, place, turn, prefab and saved values.</summary>
        internal static KwUndoPiece Kept(Piece piece)
        {
            Transform at = piece.transform;
            ZDO zdo = piece.m_nview.GetZDO();
            return new KwUndoPiece(zdo.m_uid, at.position)
            {
                Rotation = at.rotation,
                Prefab = Utils.GetPrefabName(piece.gameObject),
                Data = KwBpData.Capture(zdo),
            };
        }

        /// <summary>Whether the player may remove a piece as the hammer does: removable, and under no ward the player has no access to.</summary>
        internal static bool Removable(Piece piece) =>
            piece.m_canBeRemoved && piece.CanBeRemoved() && PrivateArea.CheckAccess(piece.transform.position, 0f, false, false);

        /// <summary>A piece removed as the hammer removes it (the game's RemovePiece), dropping nothing; false when it is kept.</summary>
        internal static bool RemoveOne(Piece piece)
        {
            if (!Removable(piece))
                return false;
            piece.GetComponent<IRemoved>()?.OnRemoved();
            WearNTear? wear = piece.GetComponent<WearNTear>();
            if (wear != null)
            {
                wear.Remove(true);
                return true;
            }
            piece.m_nview.ClaimOwnership();
            ZNetScene.instance.Destroy(piece.gameObject);
            return true;
        }

        /// <summary>The undone pieces built again: the step they now make, and how many could not be built.</summary>
        public static KwUndoStep Rebuild(Player player, KwUndoStep step, out int left)
        {
            KwUndoStep built = new KwUndoStep(step.Kind, step.Tool, -1);
            left = 0;
            foreach (KwUndoPiece piece in step.Pieces)
            {
                ZDO? zdo = Build(player, piece, out Transform? made);
                if (zdo != null && made != null)
                    built.Pieces.Add(new KwUndoPiece(zdo.m_uid, made.position));
                else
                    left++;
            }
            return built;
        }

        /// <summary>A hammer piece built again from what it kept at a place (an edit carrying it with a cabin's roof), recorded with this frame's undo step.</summary>
        internal static void Place(Player player, KwUndoPiece piece)
        {
            if (Build(player, piece, out Transform? made) != null && made != null)
                KwUndo.Made(player, made.gameObject, piece.Prefab, false);
        }

        // One piece instantiated where it stood, its saved values given back, the player its creator; null when it may not be.
        private static ZDO? Build(Player player, KwUndoPiece piece, out Transform? made)
        {
            made = null;
            ZNetScene? scene = ZNetScene.instance;
            GameObject? prefab = scene != null && KwBlueprintStation.IsShipPiece(piece.Prefab) ? scene.GetPrefab(piece.Prefab) : null;
            if (prefab == null || !PrivateArea.CheckAccess(piece.Position, 0f, false, false))
                return null;
            GameObject built = Object.Instantiate(prefab, piece.Position, piece.Rotation);
            made = built.transform;
            ZNetView? view = built.GetComponent<ZNetView>();
            ZDO? zdo = view != null ? view.GetZDO() : null;
            if (zdo != null)
                KwBpData.Apply(zdo, piece.Data);
            built.GetComponent<Piece>()?.SetCreator(player.GetPlayerID(), PlatformManager.DistributionPlatform.LocalUser.PlatformUserID);
            return zdo;
        }
    }
}
