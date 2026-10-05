using System.Collections.Generic;
using UnityEngine;

namespace OpenKeep.Blueprints.Sites
{
    /// <summary>
    /// A construction site's ground, shaped on the machine that owns the site before any piece: planned there and then
    /// (the terrain may have changed since the site was placed; the plan is kept a few seconds), paid with the ground's
    /// net Stone from the store (a cut that gives more stone than the fill needs puts the rest into the store, to be taken
    /// back with the site), then the site cleared, the ground work sent to the terrain owners and the site marked shaped.
    /// It waits while the store lacks the stone or part of the ground is not loaded here; the stone the site asks for is
    /// corrected to the fresh plan.
    /// </summary>
    public static class SiteGround
    {
        private const float PlanLife = 3f;

        /// <summary>Shapes the ground when it can; true once it is shaped, false while it waits.</summary>
        public static bool TryShape(SiteMarker site, bool free)
        {
            SiteState s = site.State;
            if (s.GroundDone)
                return true;
            Dictionary<string, int> store = s.Store;
            if (!free && s.GroundStone > SiteCosts.Count(store, SiteCosts.StoneItem))
                return false;
            GroundWork work = PlanFor(site);
            if (work.Unloaded > 0)
                return false;
            int net = work.StoneNeeded - work.StoneRemoved;
            if (net != s.GroundStone)
                s.SetGroundStone(net);
            if (!free && SiteCosts.Count(store, SiteCosts.StoneItem) < net)
                return false;
            if (!free)
            {
                SiteCosts.Add(store, SiteCosts.StoneItem, -net);
                s.SetStore(store);
            }
            Shape(site, work);
            return true;
        }

        private static GroundWork PlanFor(SiteMarker site)
        {
            SiteRun run = site.Run;
            if (run.Ground == null || Time.time > run.GroundAt + PlanLife)
            {
                run.Ground = GroundFit.Plan(site.State.Blueprint, site.State.Frame);
                run.GroundAt = Time.time;
            }
            return run.Ground;
        }

        private static void Shape(SiteMarker site, GroundWork work)
        {
            SiteState s = site.State;
            int cleared = SiteClearing.Clear(new SitePlan { Blueprint = s.Blueprint, Frame = s.Frame, Work = work });
            int compilers = GroundWriter.Send(work);
            BuildUndo.GroundShaped(site, work);
            s.SetGroundDone();
            site.Run.Ground = null;
            Plugin.Log.LogInfo($"OpenKeep: site {s.Name} at {s.Frame.Origin}: {cleared} objects cleared, " +
                $"{work.Points.Count} ground points sent to {compilers} terrain compilers");
        }
    }
}
