using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// What a layout costs (DECORATE.md, "Cost"): each decoration's recipe (its piece's requirements, given back when it is
    /// removed) summed by item, checked the game's way for building (<see cref="KwGamePay.Has"/>, so a mod that builds from
    /// nearby chests counts too); free with the game's no-cost cheat or the world's no build cost. The workbench the
    /// decorations need is looked for where the player stands, as the game does. The part a player can pay for is the
    /// layout's decorations in order, each kept while the running total is still paid for.
    /// </summary>
    public static class KwDecorCost
    {
        private const string Lacking = "#ff7a6a", Enough = "#e8d8b0";

        public static bool Free(Player player) => player.NoCostCheat() || (ZoneSystem.instance != null && ZoneSystem.instance.GetGlobalKey(GlobalKeys.NoBuildCost));

        /// <summary>The plans' recipes summed by item, in the order first met.</summary>
        public static List<Piece.Requirement> Sum(IEnumerable<KwDecorPlan> plans)
        {
            List<Piece.Requirement> sum = new List<Piece.Requirement>();
            foreach (KwDecorPlan plan in plans)
                Add(sum, plan.Cost);
            return sum;
        }

        private static void Add(List<Piece.Requirement> sum, Piece.Requirement[] more)
        {
            foreach (Piece.Requirement need in more)
            {
                if (need.m_resItem == null || need.m_amount <= 0)
                    continue;
                Piece.Requirement? had = sum.Find(r => r.m_resItem == need.m_resItem);
                if (had != null)
                    had.m_amount += need.m_amount;
                else
                    sum.Add(new Piece.Requirement { m_resItem = need.m_resItem, m_amount = need.m_amount, m_recover = true });
            }
        }

        public static bool CanPay(Player player, List<Piece.Requirement> sum) => Free(player) || sum.TrueForAll(need => KwGamePay.Has(player, need));

        /// <summary>The plans in order, each kept while everything kept so far is still paid for.</summary>
        public static List<KwDecorPlan> Affordable(Player player, List<KwDecorPlan> plans)
        {
            if (Free(player))
                return new List<KwDecorPlan>(plans);
            List<KwDecorPlan> kept = new List<KwDecorPlan>();
            foreach (KwDecorPlan plan in plans)
            {
                kept.Add(plan);
                if (!CanPay(player, Sum(kept)))
                    kept.RemoveAt(kept.Count - 1);
            }
            return kept;
        }

        /// <summary>The station the decorations need that is not in range of the player, by name; null when every one is (or none is needed).</summary>
        public static string? MissingStation(Player player, IEnumerable<KwDecorPlan> plans)
        {
            if (player.NoCostCheat() || ZoneSystem.instance.GetGlobalKey(GlobalKeys.NoWorkbench))
                return null;
            foreach (KwDecorPlan plan in plans)
            {
                CraftingStation station = plan.Kind.Piece.m_craftingStation;
                if (station != null && !CraftingStation.HaveBuildStationInRange(station.m_name, player.transform.position))
                    return station.m_name;
            }
            return null;
        }

        /// <summary>"6 Wood (have 20), 4 Leather scraps (have 1)": what lacks in red.</summary>
        public static string Text(Player player, List<Piece.Requirement> sum)
        {
            if (sum.Count == 0 || Free(player))
                return "free";
            StringBuilder text = new StringBuilder();
            foreach (Piece.Requirement need in sum)
            {
                string name = Localization.instance.Localize(need.m_resItem.m_itemData.m_shared.m_name);
                int have = player.GetInventory().CountItems(need.m_resItem.m_itemData.m_shared.m_name);
                string colour = KwGamePay.Has(player, need) ? Enough : Lacking;
                text.Append(text.Length > 0 ? ", " : "").Append($"<color={colour}>{need.m_amount} {name}</color> (have {have})");
            }
            return text.ToString();
        }
    }
}
