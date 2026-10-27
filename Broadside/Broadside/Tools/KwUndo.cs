using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// Undo and redo for building on the dock: Ctrl + Z and Ctrl + Y with a Broadside piece in the hammer
    /// (<see cref="KwUndoKeys"/>). The local player's last <see cref="Most"/> building actions this session are kept, each
    /// the ZDO ids of the pieces it made (<see cref="KwUndoStep"/>): every part a tool spawns (<see cref="KwTools.Spawn"/>;
    /// the parts spawned in one frame are one action, as every tool builds all it makes at once) and every Broadside ship
    /// piece placed from the hammer (<see cref="KwUndoPlaced"/>). Undo removes the newest action's pieces that still stand
    /// and that this player made, the way the hammer removes a piece (<see cref="KwUndoWork"/>); an action whose pieces are
    /// all gone already (removed by hand, the dock cleared) is passed over. Nothing is refunded: design is free. Redo builds
    /// the undone pieces again where they stood, from what they kept in their ZDO. An edit (<see cref="KwEditWork"/>) is one
    /// action too: its new parts, each kept beside the part it replaced (<see cref="Replaced"/>); undo takes the new parts
    /// away and builds the old ones again in their places, redo the other way round (<see cref="KwUndoWork.Swap"/>). A new
    /// action clears what could be redone. Kept on the player's own game only; removing and building go through the game's
    /// networked pieces.
    /// </summary>
    public static class KwUndo
    {
        /// <summary>How many actions are kept.</summary>
        public const int Most = 30;

        /// <summary>How near the player every piece of an action must be to undo or redo it: the game has the area loaded.</summary>
        public const float Reach = 60f;

        private static readonly List<KwUndoStep> Done = new List<KwUndoStep>(), Undone = new List<KwUndoStep>();
        private static ZNet? session;
        private static long owner;

        /// <summary>A piece the local player just made: it joins the action of this frame, or starts a new one.</summary>
        public static void Made(Player player, GameObject made, string kind, bool tool)
        {
            ZNetView? view = made.GetComponent<ZNetView>();
            ZDO? zdo = view != null ? view.GetZDO() : null;
            if (zdo == null || player != Player.m_localPlayer)
                return;
            Fresh(player);
            KwUndoStep? top = Done.Count > 0 ? Done[Done.Count - 1] : null;
            if (top != null && top.Frame == Time.frameCount)
                top.Join(kind, tool);
            else
                Push(Done, top = new KwUndoStep(kind, tool, Time.frameCount));
            top.Pieces.Add(new KwUndoPiece(zdo.m_uid, made.transform.position));
            Undone.Clear();
        }

        /// <summary>
        /// A piece an edit just took away to build its replacement in its place (<see cref="KwEditWork"/>): kept with this
        /// frame's action beside that replacement, the piece the action recorded last.
        /// </summary>
        public static void Replaced(Player player, KwUndoPiece old)
        {
            KwUndoStep? top = player == Player.m_localPlayer && Done.Count > 0 ? Done[Done.Count - 1] : null;
            if (top != null && top.Frame == Time.frameCount && top.Pieces.Count == top.Replaced.Count + 1)
                top.Replaced.Add(old);
        }

        /// <summary>
        /// A piece this frame's action took away with nothing built in its place (a railing a cabin stood on): kept after the
        /// action's pairs, so undo builds it again and redo takes it again (<see cref="KwUndoWork.Swap"/>). Call it after the
        /// frame's pieces are spawned; with nothing spawned this frame it is not kept.
        /// </summary>
        public static void Taken(Player player, KwUndoPiece old)
        {
            KwUndoStep? top = player == Player.m_localPlayer && Done.Count > 0 ? Done[Done.Count - 1] : null;
            if (top != null && top.Frame == Time.frameCount)
                top.Replaced.Add(old);
        }

        /// <summary>
        /// Pieces the hammer removed together (a cabin and what stood on it, <see cref="KwCabinRemoval"/>), kept as one action:
        /// undo builds them again, redo takes them again (<see cref="KwUndoStep.Removal"/>).
        /// </summary>
        public static void Removed(Player player, string kind, List<KwUndoPiece> taken)
        {
            if (player != Player.m_localPlayer || taken.Count == 0)
                return;
            Fresh(player);
            KwUndoStep step = new KwUndoStep(kind, true, -1) { Removal = true };
            step.Replaced.AddRange(taken);
            Push(Done, step);
            Undone.Clear();
        }

        /// <summary>The newest action that still has pieces standing (a removal: pieces to build again), undone.</summary>
        public static void Undo(Player player)
        {
            Fresh(player);
            while (Done.Count > 0)
            {
                KwUndoStep step = Done[Done.Count - 1];
                if (!Near(player, step))
                {
                    Say(player, "Too far from those parts to undo");
                    return;
                }
                Done.RemoveAt(Done.Count - 1);
                List<Piece> standing = KwUndoWork.Standing(step, player.GetPlayerID());
                if (standing.Count > 0 || (step.Removal && step.Replaced.Count > 0))
                {
                    Take(player, step, standing);
                    return;
                }
            }
            Say(player, "Nothing to undo");
        }

        // The action's standing pieces removed, what they were kept for a redo, and the player told.
        private static void Take(Player player, KwUndoStep step, List<Piece> standing)
        {
            KwUndoStep removed = step.Swaps ? KwUndoWork.Swap(player, step, out int kept) : KwUndoWork.Remove(step, standing, out kept);
            int count = removed.Count;
            if (count > 0)
                Push(Undone, removed);
            if (count == 0)
                Say(player, $"Could not undo: {step.Words(kept)} (a ward you have no access to)");
            else
                Say(player, "Undid: " + step.Words(count) + (kept > 0 ? $"\n{kept} kept (a ward you have no access to)" : ""));
        }

        /// <summary>The newest undone action built again.</summary>
        public static void Redo(Player player)
        {
            Fresh(player);
            KwUndoStep? step = Undone.Count > 0 ? Undone[Undone.Count - 1] : null;
            if (step == null)
            {
                Say(player, "Nothing to redo");
                return;
            }
            if (!Near(player, step))
            {
                Say(player, "Too far from those parts to redo");
                return;
            }
            Undone.RemoveAt(Undone.Count - 1);
            KwUndoStep built = step.Swaps ? KwUndoWork.Swap(player, step, out int left) : KwUndoWork.Rebuild(player, step, out left);
            int count = built.Count;
            if (count > 0)
                Push(Done, built);
            if (count == 0)
                Say(player, $"Could not redo: {step.Words(left)} (a ward you have no access to)");
            else
                Say(player, "Redid: " + step.Words(count) + (left > 0 ? $"\n{left} left out (a ward you have no access to)" : ""));
        }

        // Every piece of the action within Reach of the player (a removal's taken pieces too).
        private static bool Near(Player player, KwUndoStep step)
        {
            Vector3 at = player.transform.position;
            List<KwUndoPiece> pieces = new List<KwUndoPiece>(step.Pieces);
            if (step.Removal)
                pieces.AddRange(step.Replaced);
            return pieces.TrueForAll(piece => (piece.Position - at).sqrMagnitude <= Reach * Reach);
        }

        // The record belongs to one player in one world session: another character or a new session starts it empty.
        private static void Fresh(Player player)
        {
            long id = player.GetPlayerID();
            if (session != null && session == ZNet.instance && owner == id)
                return;
            Done.Clear();
            Undone.Clear();
            session = ZNet.instance;
            owner = id;
        }

        private static void Push(List<KwUndoStep> steps, KwUndoStep step)
        {
            steps.Add(step);
            if (steps.Count > Most)
                steps.RemoveAt(0);
        }

        private static void Say(Player player, string text) => player.Message(MessageHud.MessageType.Center, text);
    }
}
