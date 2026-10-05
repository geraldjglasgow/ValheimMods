using System.Collections.Generic;
using OpenKeep.Blueprints.Sites;
using UnityEngine;

namespace OpenKeep.Blueprints
{
    /// <summary>
    /// Builds the rest of a construction site at once, on the machine that owns the site (<see cref="SiteBuilder"/>
    /// starts it once the site holds everything it still needs, or when building is free, after shaping the ground):
    /// <see cref="BlueprintRules.PiecesPerFrame"/> pieces a frame in <see cref="SiteOrder"/>, each piece's materials
    /// taken from the site's store unless free, the built pieces and the store written once a frame
    /// (<see cref="SitePieces"/>). Every machine sees the pieces as ordinary networked pieces. Stops quietly when the
    /// site goes away, another machine takes it over (that machine's builder carries on from the ZDO), the store runs
    /// short or the local player is gone; the builder then looks again. One site at a time; the HUD shows its progress.
    /// </summary>
    public static class BuildJob
    {
        private sealed class Run
        {
            public SiteMarker Site;
            public Player Player;
            public List<int> Order;
            public int Next;
            public bool Free;
            public bool Cheated;
        }

        private static Run run;

        public static bool Busy => run != null;

        public static bool IsBuilding(SiteMarker site) => run != null && run.Site == site;

        /// <summary>"Building x: 120 of 444 pieces" while a site builds at once on this machine, else null (the HUD shows it).</summary>
        public static string Progress
        {
            get
            {
                SiteState s = run != null && run.Site != null ? run.Site.State : null;
                Blueprint bp = s?.Blueprint;
                return bp == null ? null : BlueprintWords.Format(BlueprintWords.Progress, s.Name, s.BuiltCount, bp.Pieces.Count);
            }
        }

        public static void Start(SiteMarker site, Player player, bool free, bool cheated)
        {
            run = new Run { Site = site, Player = player, Order = SiteOrder.Remaining(site.State), Free = free, Cheated = cheated };
            Plugin.Log.LogInfo($"OpenKeep: building site {site.State.Name} at once: {run.Order.Count} pieces" + (free ? " (free)" : ""));
        }

        public static void Tick()
        {
            if (run == null)
                return;
            if (!StillOurs())
            {
                run = null;
                return;
            }
            int count = Mathf.Min(BlueprintRules.PiecesPerFrame, run.Order.Count - run.Next);
            List<int> batch = run.Order.GetRange(run.Next, count);
            run.Next += count;
            bool placed = SitePieces.Place(run.Site, batch, run.Player, run.Free, run.Cheated);
            if (!placed || run.Next >= run.Order.Count)
                run = null;
        }

        /// <summary>The site is still loaded and this machine's, and the player who started it is still here.</summary>
        private static bool StillOurs()
        {
            SiteMarker site = run.Site;
            return site != null && site.View != null && site.View.IsOwner() && site.State?.Blueprint != null
                && run.Player != null && run.Player == Player.m_localPlayer && !run.Player.IsDead();
        }
    }
}
