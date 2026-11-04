using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// A layout placed (DECORATE.md, "Placing"): two decorations a frame, each through the game's own placement
    /// (<c>Player.PlacePiece</c>: a networked piece with the player as its creator, its placing effect, the same as one placed
    /// by hand for every peer and a dedicated server) and paid the game's way as it is placed (<see cref="KwGamePay"/>).
    /// Before each, the game's rules and the solid test are asked again (something may have moved in) and its cost again;
    /// one that no longer fits or cannot be paid is left out and counted. The pieces placed are the batch the undo takes
    /// back (<see cref="KwDecorUndo"/>). The player's swing plays once, for the first.
    /// </summary>
    public static class KwDecorPlacer
    {
        private const int AFrame = 2;
        private static readonly Queue<KwDecorPlan> queue = new Queue<KwDecorPlan>();
        private static KwDecorGrid? grid;
        private static Player? who;
        private static int placed, left;

        public static bool Busy => queue.Count > 0;

        public static void Start(Player player, List<KwDecorPlan> plans, KwDecorGrid area)
        {
            queue.Clear();
            foreach (KwDecorPlan plan in plans)
                queue.Enqueue(plan);
            (who, grid, placed, left) = (player, area, 0, 0);
            KwDecorUndo.Begin();
        }

        /// <summary>
        /// A few more placed; the message once the last is. Not while the player's ghost stands on a launched ship this frame:
        /// a piece placed then would be taken as built aboard (<see cref="KwShipRiderPlaced"/>).
        /// </summary>
        public static void Step()
        {
            if (queue.Count > 0 && KwShipRiderAim.Ship != null)
                return;
            for (int n = 0; n < AFrame && queue.Count > 0; n++)
                One(queue.Dequeue());
            if (queue.Count == 0 && who != null)
                Finish();
        }

        private static void One(KwDecorPlan plan)
        {
            Player? player = who;
            if (player == null || player.IsDead() || grid == null)
            {
                queue.Clear();
                return;
            }
            Piece piece = plan.Kind.Piece;
            if (!KwDecorRules.Allowed(grid, plan) || !Clear(plan) || !KwDecorCost.CanPay(player, new List<Piece.Requirement>(plan.Cost)))
            {
                left++;
                return;
            }
            player.PlacePiece(piece, plan.Position, plan.Rotation, placed == 0, player.NoCostCheat());
            Piece? made = Newest(piece.gameObject.name, player.GetPlayerID());
            KwDecorUndo.Placed(made);
            if (!KwDecorCost.Free(player))
                KwGamePay.Take(player, plan.Cost);
            placed++;
        }

        // Nothing in its way: a mooring post or lantern pole over a dock's piling is let stand in the curb it cuts (KwDecorMoorings).
        private static bool Clear(KwDecorPlan plan) =>
            KwDockMooring.OnPiling(plan.Kind.Name) && plan.Slot.Pushed && KwPierDeck.Near(plan.Position, 0.05f).Count > 0 ? KwDecorMoorings.Clear(plan) : KwDecorSolids.Clear(plan);

        // The piece the game just placed: the newest in its list of every piece, of that prefab and this player's.
        private static Piece? Newest(string prefab, long creator)
        {
            List<Piece> all = Piece.s_allPieces;
            Piece? last = all.Count > 0 ? all[all.Count - 1] : null;
            return last != null && Utils.GetPrefabName(last.gameObject) == prefab && last.GetCreator() == creator ? last : null;
        }

        private static void Finish()
        {
            Player player = who!;
            who = null;
            KwDecorUndo.End();
            string more = left > 0 ? $"; {left} left out (something moved in, or not enough to pay)" : "";
            string undo = placed > 0 ? $"\n{KwDecorWords.UndoKey} takes them back" : "";
            player.Message(MessageHud.MessageType.Center, $"{placed} decorations placed{more}{undo}");
        }
    }
}
