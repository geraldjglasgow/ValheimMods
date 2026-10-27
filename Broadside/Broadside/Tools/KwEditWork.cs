using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// An edit's parts built again (<see cref="KwEditSession"/>): each part is removed the hammer's way and the new one
    /// spawned through the tools' own spawn path in the same place and turn (<see cref="KwTools.Spawn"/>), or as far from it
    /// as its plan moves it (<see cref="KwEditPlan.Shift"/>: a cabin carried on an edited cabin's roof, what stands on it),
    /// its saved text and tier new, every other value it kept in its ZDO given to the new one (a mast's hoist, the ship id of
    /// a ship built from a blueprint); a part that follows the edit is built again with its own tool, or from its prefab for
    /// a hammer piece. Replacing rather than rewriting the old part's text: a generated part builds its mesh from its text
    /// once, when it loads (<see cref="KwGenerated"/>), so other peers would never see a rewritten text, while a new networked
    /// piece is built from its text on every peer. The parts spawned in one frame are one undo step, and each part taken away
    /// is kept with it beside its replacement (<see cref="KwUndo.Replaced"/>), so Ctrl + Z puts the old parts back and Ctrl + Y
    /// the new ones (<see cref="KwUndoWork.Swap"/>); a part a plan only takes away is kept after them
    /// (<see cref="KwUndo.Taken"/>). A part the player may not remove (a ward) is left as it is; a part gone meanwhile is
    /// passed over. The parts taken away are told to the caller (gone), so an edit keeps them hidden until their removal
    /// reaches this screen (<see cref="KwEditHide"/>). Free, as every design piece is.
    /// </summary>
    public static class KwEditWork
    {
        /// <summary>
        /// Every plan's part built again (or taken away); how many of the edit's own were, how many of them a ward kept, and
        /// into gone (when given) every part taken away.
        /// </summary>
        public static int Replace(Player player, string tool, List<KwEditPlan> plans, out int kept, List<Piece>? gone = null)
        {
            int done = 0;
            kept = 0;
            foreach (KwEditPlan plan in Ordered(plans))
            {
                Piece piece = plan.Target.Piece;
                if (piece == null || piece.m_nview == null || !piece.m_nview.IsValid())
                    continue;
                bool made = plan.Take ? Take(player, piece) : One(player, tool, plan);
                if (made)
                    gone?.Add(piece);
                if (!plan.Follows && made)
                    done++;
                else if (!plan.Follows)
                    kept++;
            }
            return done;
        }

        // The plans that build first, then those that only take a part away (kept after the step's pairs).
        private static List<KwEditPlan> Ordered(List<KwEditPlan> plans)
        {
            List<KwEditPlan> ordered = plans.FindAll(plan => !plan.Take);
            ordered.AddRange(plans.FindAll(plan => plan.Take));
            return ordered;
        }

        // The part taken away and its new self spawned in its place, its other values kept; false when it may not be removed.
        private static bool One(Player player, string tool, KwEditPlan plan)
        {
            Piece piece = plan.Target.Piece;
            Transform at = piece.transform;
            (Vector3 position, Quaternion turn) = (at.position + plan.Shift, at.rotation);
            KwUndoPiece old = KwUndoWork.Kept(piece);
            string ship = piece.m_nview.GetZDO().GetString(KwBlueprintStation.ShipHash);
            if (!KwUndoWork.RemoveOne(piece))
                return false;
            List<KwBpValue> keep = Keep(old.Data, ship);
            string made = plan.Tool ?? tool;
            if (made.Length > 0)
                KwTools.Spawn(player, made, position, turn, plan.Text, plan.Tier, keep);
            else
                KwUndoWork.Place(player, new KwUndoPiece(old.Id, position) { Rotation = turn, Prefab = old.Prefab, Data = keep });
            KwUndo.Replaced(player, old);
            return true;
        }

        // A part only taken away, kept with this frame's undo step after its pairs; false when it may not be removed.
        private static bool Take(Player player, Piece piece)
        {
            KwUndoPiece old = KwUndoWork.Kept(piece);
            if (!KwUndoWork.RemoveOne(piece))
                return false;
            KwUndo.Taken(player, old);
            return true;
        }

        // What the old part kept in its ZDO besides the game's own values, with the ship id of a ship built from a blueprint.
        private static List<KwBpValue> Keep(List<KwBpValue> data, string ship)
        {
            List<KwBpValue> keep = new List<KwBpValue>(data);
            if (ship.Length > 0)
                keep.Add(new KwBpValue(KwBlueprintStation.ShipHash, ship));
            return keep;
        }
    }
}
