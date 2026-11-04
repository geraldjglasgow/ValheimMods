using System.Collections.Generic;
using HarmonyLib;
using PatchGuard;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// Placing the hammock (<c>BS_hammock</c>) by its two ends, on the placing player's game: the first click sets one end
    /// (<see cref="KwHammockAim.Click"/>: a post on a floor, a deck or the ground, an iron hook in anything else) and costs
    /// nothing; the second, where the other end goes, plans the hammock between them (<see cref="KwHammockPlan"/>) and hangs
    /// it through the game's own placement (<c>Player.PlacePiece</c> where the plan stands as the ship is drawn: the networked
    /// piece, its creator, its placing and wear set up, the game's place effect, building statistics and the hammer's swing),
    /// its shape written to its new ZDO and built in the same frame (<see cref="KwHammock.Take"/>), and aboard a launched ship
    /// made to ride it (<see cref="KwShipRider.Place"/>); true, so the game then takes its recipe as it takes any piece's
    /// (none with the world's free build key), its stamina and the hammer's wear. Before each click the game has checked the
    /// workbench in range and the recipe (<c>HaveRequirements</c>). Esc or a right click cancels a first end
    /// (<see cref="KwHammockCancel"/>), and so does putting the hammock away (<see cref="KwHammockPreview"/>).
    /// </summary>
    [HarmonyPatch(typeof(Player), nameof(Player.TryPlacePiece))]
    public static class KwHammockTool
    {
        private static KwHammockClick? pending;

        /// <summary>The first end waiting for its second; null while none does.</summary>
        public static KwHammockClick? Pending => pending;

        [HarmonyPrefix]
        private static bool Prefix(Player __instance, Piece piece, ref bool __result)
        {
            if (piece == null || piece.name != KwHammocks.Prefab)
                return true;
            __result = Guard.Run<Player, Piece, bool>("Broadside hammock", Click, __instance, piece);
            return false;
        }

        /// <summary>The hammock in this player's hammer, or null.</summary>
        public static Piece? Held(Player player)
        {
            Piece? piece = player.InPlaceMode() ? player.GetSelectedPiece() : null;
            return piece != null && piece.name == KwHammocks.Prefab ? piece : null;
        }

        private static bool Click(Player player, Piece piece) => pending == null ? First(player) : Second(player, piece, pending);

        private static bool First(Player player)
        {
            KwHammockClick? aim = KwHammockAim.Click(player);
            if (aim == null || aim.Why != null)
                return Say(player, aim?.Why ?? KwHammockWords.AimFirst);
            pending = aim;
            return Say(player, KwHammockWords.Started(aim));
        }

        private static bool Second(Player player, Piece piece, KwHammockClick first)
        {
            if (first.Lost)
            {
                Cancel(KwHammockWords.ShipGone);
                return false;
            }
            KwHammockClick? aim = KwHammockAim.Click(player);
            if (aim == null || aim.Why != null)
                return Say(player, aim?.Why ?? KwHammockWords.AimSecond);
            KwHammockPlan plan = KwHammockPlan.Make(first, aim, true);
            return plan.Why != null || plan.Spec == null ? Say(player, plan.Why ?? KwHammockWords.AimSecond) : Hang(player, piece, plan);
        }

        // Placed the game's way, its shape built in the same frame, riding its ship aboard; true: the game takes its cost.
        private static bool Hang(Player player, Piece piece, KwHammockPlan plan)
        {
            (Vector3 at, Quaternion turn) = plan.Drawn;
            bool cheated = (player.m_inventory.ItemCheated(piece.m_resources) || player.NoCostCheat()) && !PlayerProfile.s_bypassCheatChecks;
            Game.instance.IncrementPlayerStat(PlayerStatType.Builds, 1f, cheated);
            Game.instance.GetPlayerProfile().IncrementStatBuildPiecePlaced(piece.m_name, 1f, cheated);
            player.PlacePiece(piece, at, turn, true, cheated);
            pending = null;
            Piece? made = Newest(piece.name, player.GetPlayerID());
            if (made == null)
            {
                Log.Warn("Broadside: the hammock just placed was not found; it stands as two posts.");
                return true;
            }
            KwHammock.Take(made.gameObject, plan.Spec!);
            if (plan.Ship != null)
                KwShipRider.Place(made.gameObject, plan.Ship);
            Say(player, KwHammockWords.Hung(plan));
            return true;
        }

        // The piece the game just placed: the newest in its list of every piece, of that prefab and this player's.
        private static Piece? Newest(string prefab, long creator)
        {
            List<Piece> all = Piece.s_allPieces;
            Piece? last = all.Count > 0 ? all[all.Count - 1] : null;
            return last != null && Utils.GetPrefabName(last.gameObject) == prefab && last.GetCreator() == creator ? last : null;
        }

        /// <summary>The first end forgotten, the player told (words) when there are any.</summary>
        public static void Cancel(string? words)
        {
            pending = null;
            if (words != null && Player.m_localPlayer != null)
                Say(Player.m_localPlayer, words);
        }

        private static bool Say(Player player, string text)
        {
            player.Message(MessageHud.MessageType.Center, text);
            return false;
        }
    }
}
