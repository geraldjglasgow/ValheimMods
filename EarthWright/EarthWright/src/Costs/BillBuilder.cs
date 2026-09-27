using System.Collections.Generic;
using UnityEngine;

namespace EarthWright.Costs
{
    /// <summary>
    /// Puts together what a terrain use costs in items: the entry's materials (its own or its YAML list, grown with
    /// the brush radius), the extra item per swing, and the volume stone. Nothing at all while materials are free:
    /// free build, the game's no-cost cheat (devcommands "nocost") or the world's "no build cost" setting.
    /// </summary>
    internal static class BillBuilder
    {
        public static bool MaterialsFree(Player player)
        {
            if (FreeBuild.On || player == null || player.NoCostCheat())
                return true;
            return ZoneSystem.instance != null && ZoneSystem.instance.GetGlobalKey(GlobalKeys.NoBuildCost);
        }

        /// <summary>The cost of a use that raises this volume (m³) and newly paves this area (m²).</summary>
        public static UseCost For(CostContext ctx, float raised, float paved)
        {
            UseCost cost = new UseCost();
            if (MaterialsFree(ctx.Player))
                return cost;
            if (MaterialSettings.ChargeMaterials.Value)
                AddMaterials(cost.Items, ctx);
            cost.Items.Add(ItemLookup.Item(MaterialSettings.ExtraItem.Value), MaterialSettings.ExtraItemAmount.Value);
            AddVolume(cost, raised, paved);
            return cost;
        }

        /// <summary>The entry's materials and the extra item only, as the game's requirement check needs them.</summary>
        public static Bill Items(CostContext ctx) => For(ctx, 0f, 0f).Items;

        private static void AddVolume(UseCost cost, float raised, float paved)
        {
            cost.VolumeOwed = VolumeCharge.Cost(raised, paved);
            cost.VolumeDue = VolumeCharge.Due(cost.VolumeOwed);
            if (cost.VolumeDue > 0)
                cost.Volume.Add(ItemLookup.Item(MaterialSettings.VolumeItem.Value), cost.VolumeDue);
        }

        private static void AddMaterials(Bill bill, CostContext ctx)
        {
            float scale = ctx.Scale(MaterialSettings.MaterialRadiusExponent.Value);
            List<ItemAmount> listed = ctx.Override?.Resources;
            if (listed != null)
            {
                foreach (ItemAmount item in listed)
                    bill.Add(ItemLookup.Item(item.Prefab), Scaled(item.Amount, scale));
                return;
            }
            if (ctx.Piece == null || ctx.Piece.m_resources == null)
                return;
            foreach (Piece.Requirement requirement in ctx.Piece.m_resources)
                bill.Add(requirement.m_resItem, Scaled(requirement.m_amount, scale));
        }

        /// <summary>An amount grown with the radius, rounded up, never below 1 when the entry costs any.</summary>
        private static int Scaled(int amount, float scale)
        {
            return amount <= 0 ? 0 : Mathf.Max(1, Mathf.CeilToInt(amount * scale - 0.001f));
        }
    }
}
