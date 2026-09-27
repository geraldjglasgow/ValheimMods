using EarthWright.Core;

namespace EarthWright.Clearing
{
    /// <summary>
    /// Carries out a <see cref="ClearPlan"/>. Each object is first claimed (this machine becomes its owner, as the game
    /// does when it removes an object itself), so every change below is made by the owner and replicates to everyone:
    /// either the object is destroyed outright (nothing drops) or it is hit through its own damage RPC, which on the
    /// owner runs the game's normal chopping and mining and drops wood and stone (<see cref="SurvivalHits"/>).
    /// </summary>
    public static class ClearExecutor
    {
        /// <summary>Clears every target; returns how many were taken away or struck.</summary>
        public static int Run(ClearPlan plan, Player player)
        {
            int cleared = 0;
            foreach (ClearTarget target in plan.Targets)
            {
                if (Safe.Call("EarthWright clearing", () => ClearOne(plan, target, player), false))
                    cleared++;
            }
            return cleared;
        }

        private static bool ClearOne(ClearPlan plan, ClearTarget target, Player player)
        {
            ZNetView view = target.View;
            if (view == null || !view.IsValid())
                return false;
            view.ClaimOwnership();
            if (plan.Drops)
                SurvivalHits.Strike(target, player, plan.Tools);
            else
                Remove(view);
            return true;
        }

        /// <summary>Destroys an object this machine owns; its ZDO is destroyed for everyone.</summary>
        public static void Remove(ZNetView view)
        {
            if (view != null && view.IsValid() && ZNetScene.instance != null)
                ZNetScene.instance.Destroy(view.gameObject);
        }
    }
}
