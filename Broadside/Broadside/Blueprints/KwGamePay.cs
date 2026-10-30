using UnityEngine;
using Object = UnityEngine.Object;

namespace Broadside
{
    /// <summary>
    /// A cost checked and taken the way the game checks and takes a building piece's (<c>Player.HaveRequirements</c> in
    /// its build mode, <c>Player.ConsumeResources</c> at quality 0), so whatever lets the player build from nearby chests
    /// counts and pays here too: a saved ship's cost past the build menu's slots, a fallen mast put back, an edit at sea.
    /// The check asks of a piece of its own (never shown, placed or networked; no station), one requirement at a time so
    /// the player is told each item lacking. Free in a world without build costs, as the game's pieces are.
    /// </summary>
    public static class KwGamePay
    {
        private static Piece? asker;

        /// <summary>Whether the player has the requirement as the game counts it for building.</summary>
        public static bool Has(Player player, Piece.Requirement need)
        {
            Piece piece = Asker();
            piece.m_resources = new[] { need };
            try
            {
                return player.HaveRequirements(piece, Player.RequirementMode.CanBuild);
            }
            finally
            {
                piece.m_resources = new Piece.Requirement[0];
            }
        }

        /// <summary>Takes the requirements as the game takes a placed piece's.</summary>
        public static void Take(Player player, Piece.Requirement[] cost)
        {
            if (cost.Length > 0 && !Free())
                player.ConsumeResources(cost, 0);
        }

        private static bool Free() => ZoneSystem.instance != null && ZoneSystem.instance.GetGlobalKey(GlobalKeys.NoBuildCost);

        private static Piece Asker()
        {
            if (asker != null)
                return asker;
            GameObject holder = new GameObject("KwGamePay");
            holder.SetActive(false);
            Object.DontDestroyOnLoad(holder);
            asker = holder.AddComponent<Piece>();
            asker.m_name = "Broadside cost";
            return asker;
        }
    }
}
