using System.Collections.Generic;
using EarthWright.Actions;
using EarthWright.Terrain;

namespace EarthWright.Costs
{
    /// <summary>
    /// Which crafting stations a terrain use needs nearby: the entry's own (the game's: a workbench for Raise ground,
    /// a stonecutter for Paved road) or the one EarthWright.Costs.yml names; none when the tool's switch or the
    /// entry's <c>stationRequired</c> says so. While "Paved Road Needs Stonecutter" is on, paving with any entry needs
    /// the stonecutter; while it is off, a paving entry's own stonecutter is dropped. The Groundbreaker entry follows
    /// "Groundbreaker Needs Stonecutter" instead (off by default). The world's "no workbench" setting and free build
    /// lift every station.
    /// </summary>
    internal static class StationRule
    {
        private const string GroundbreakerSpecial = "groundbreaker";
        private const string StonecutterPrefab = "piece_stonecutter";
        private const string StonecutterFallback = "$piece_stonecutter";

        /// <summary>The first needed station that is not in build range (its name token), or null.</summary>
        public static string Missing(CostContext ctx)
        {
            if (FreeBuild.On || NoWorkbenchWorld())
                return null;
            foreach (string station in Required(ctx))
            {
                if (CraftingStation.HaveBuildStationInRange(station, ctx.Player.transform.position) == null)
                    return station;
            }
            return null;
        }

        /// <summary>The station names the use needs (none, one or two).</summary>
        public static List<string> Required(CostContext ctx)
        {
            List<string> stations = new List<string>();
            if (!NeedsStations(ctx))
                return stations;
            string stonecutter = Stonecutter;
            string own = OwnStation(ctx, stonecutter);
            if (!string.IsNullOrEmpty(own))
                stations.Add(own);
            if (ctx.Paint == PaintOp.Paved && PavingNeedsStonecutter(ctx) && own != stonecutter)
                stations.Add(stonecutter);
            return stations;
        }

        /// <summary>The Groundbreaker entry follows its own switch; every other entry the paved road's.</summary>
        private static bool PavingNeedsStonecutter(CostContext ctx)
        {
            return ctx.Action.Special == GroundbreakerSpecial
                ? MaterialSettings.GroundbreakerNeedsStonecutter.Value
                : MaterialSettings.PavedNeedsStonecutter.Value;
        }

        private static bool NeedsStations(CostContext ctx)
        {
            bool? entry = ctx.Override?.StationRequired;
            return entry ?? FamilyNeedsStations(ctx.Family);
        }

        private static bool FamilyNeedsStations(ToolFamily family)
        {
            switch (family)
            {
                case ToolFamily.Hoe:
                    return MaterialSettings.HoeNeedsStations.Value;
                case ToolFamily.Cultivator:
                    return MaterialSettings.CultivatorNeedsStations.Value;
                default:
                    return MaterialSettings.ModdedNeedsStations.Value;
            }
        }

        /// <summary>The YAML station, else the piece's own, without a paving entry's stonecutter while paving is set free.</summary>
        private static string OwnStation(CostContext ctx, string stonecutter)
        {
            string listed = ctx.Override?.Station;
            if (!string.IsNullOrEmpty(listed))
                return ItemLookup.StationName(listed);
            CraftingStation station = ctx.Piece != null ? ctx.Piece.m_craftingStation : null;
            string own = station != null ? station.m_name : null;
            bool paving = ctx.Paint == PaintOp.Paved || ctx.Action.Paint == PaintOp.Paved;
            return paving && own == stonecutter && !PavingNeedsStonecutter(ctx) ? null : own;
        }

        private static string Stonecutter => ItemLookup.StationName(StonecutterPrefab, warn: false) ?? StonecutterFallback;

        private static bool NoWorkbenchWorld()
        {
            return ZoneSystem.instance != null && ZoneSystem.instance.GetGlobalKey(GlobalKeys.NoWorkbench);
        }
    }
}
