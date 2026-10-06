using System.Collections.Generic;
using EarthWright.Actions;
using EarthWright.Core;
using EarthWright.Terrain;

namespace EarthWright.Costs
{
    /// <summary>
    /// The live cost line of the selected entry at the current brush size: "Cost: Stamina 12  Stone 3/5", each item
    /// as have/need in red where the player is short, a missing station in red, "Free build" while that is on; and the
    /// reason the player cannot pay, for the preview. Special entries show their materials and stamina; their volume
    /// is only known once the ramp or road exists, so their handlers report it themselves.
    /// </summary>
    internal sealed class CostLine
    {
        private const string Red = "#ff6a5a";
        private const string Green = "#9be39b";

        /// <summary>The cost line refreshes every 0.25 s: an estimate that young is taken wherever the brush stood.</summary>
        private const float RecentAge = 0.25f;

        /// <summary>The HUD text, or null when the use costs nothing worth showing.</summary>
        public string Text;

        /// <summary>Why the player cannot pay now, or null.</summary>
        public string ShortReason;

        public static CostLine For(CostContext ctx)
        {
            if (FreeBuild.On)
                return new CostLine { Text = Colour(Language.Localize(CostWords.Free), Green) };
            WorkCost cost = WorkCost.Of(ctx, EstimateOf(ctx));
            List<string> parts = new List<string>();
            string station = AddStation(ctx, parts);
            if (!StaminaCost.IsVanilla(ctx) && cost.Stamina > 0f)
                parts.Add(Language.Localize(CostWords.Stamina) + " " + cost.Stamina.ToString("0.#"));
            Bill total = cost.Items.Total;
            AddItems(ctx.Player.GetInventory(), total, parts);
            string items = Affordability.ShortOf(ctx.Player, total);
            string text = parts.Count > 0 ? Language.Localize(CostWords.Cost) + ": " + string.Join("   ", parts) : null;
            return new CostLine { Text = text, ShortReason = station ?? items };
        }

        /// <summary>
        /// The estimate of a brush click now, only while a volume cost is set (special entries: none); shared with the
        /// changed-points preview (<see cref="LiveEstimate"/>), which estimates the same click, and taken from it while
        /// it is younger than the line's own refresh even when the brush has moved since.
        /// </summary>
        private static EditEstimate EstimateOf(CostContext ctx)
        {
            if (ctx.Action.IsSpecial || !VolumeCharge.Enabled || BillBuilder.MaterialsFree(ctx.Player))
                return null;
            TerrainEdit edit = EditFactory.Build(ctx.Action);
            // The building hooks only add flags (admin routing, terraform limits); the click will carry them too.
            EditEvents.RaiseBuilding(edit);
            return VolumeCharge.Exempt(edit) ? null : LiveEstimate.Recent(edit, RecentAge);
        }

        /// <summary>Adds a missing station in red and returns the matching reason, or null when every station is near.</summary>
        private static string AddStation(CostContext ctx, List<string> parts)
        {
            string missing = StationRule.Missing(ctx);
            if (missing == null)
                return null;
            string reason = CostWords.Format(CostWords.NeedStation, Language.Localize(missing));
            parts.Add(Colour(reason, Red));
            return reason;
        }

        private static void AddItems(Inventory inventory, Bill bill, List<string> parts)
        {
            foreach (BillLine line in bill.Lines)
            {
                int have = Bill.Have(inventory, line);
                string part = Language.Localize(line.Name) + " " + have + "/" + line.Amount;
                parts.Add(have < line.Amount ? Colour(part, Red) : part);
            }
        }

        private static string Colour(string text, string colour) => "<color=" + colour + ">" + text + "</color>";
    }
}
