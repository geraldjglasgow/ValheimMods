using EarthWright.Core;

namespace EarthWright.Clearing
{
    /// <summary>
    /// Carries out a <see cref="ClearPlan"/>: its targets go into the <see cref="ClearQueue"/>, which strikes a few per
    /// frame. Either the object is destroyed outright (nothing drops; this machine takes it first, since destroying needs
    /// the owner) or it is hit through its own damage RPC, which runs on the object's owner the game's normal chopping and
    /// mining and drops wood and stone there (<see cref="SurvivalHits"/>); an object nobody owns is claimed first, so
    /// the hit has somewhere to go. Objects another player owns are never taken over to be hit: that would take over
    /// the physics of a tree or a rolling log the other player is simulating.
    /// </summary>
    public static class ClearExecutor
    {
        /// <summary>Queues every target; returns how many will be taken away or struck.</summary>
        public static int Run(ClearPlan plan, Player player)
        {
            int cleared = 0;
            foreach (ClearTarget target in plan.Targets)
            {
                if (ClearQueue.Add(plan, target, player))
                    cleared++;
            }
            return cleared;
        }

        /// <summary>Clears one queued target; returns how many hits it took (0 when it was gone).</summary>
        internal static int ClearOne(ClearQueue.Job job)
        {
            ZNetView view = job.Target.View;
            if (view == null || !view.IsValid())
                return 0;
            if (job.Plan.Drops)
                return SurvivalHits.Strike(job.Target, job.Player, job.Plan.Tools);
            Remove(view);
            return 1;
        }

        /// <summary>Destroys an object for everyone; its ZDO is destroyed with it.</summary>
        public static void Remove(ZNetView view) => OwnedPick.Remove(view);
    }
}
