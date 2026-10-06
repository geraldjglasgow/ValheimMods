using System.Collections.Generic;

namespace EarthWright.Clearing
{
    /// <summary>One object a clearing will take away.</summary>
    public sealed class ClearTarget
    {
        public ZNetView View;
        public ClearCategory Kind;
    }

    /// <summary>
    /// What one clearing will do, decided before anything is charged or changed: the objects to take away and how,
    /// plus the objects left alone and why (a ward or a protection rule, a missing or too weak tool), or the reason the
    /// whole clearing is refused, for the message to the player.
    /// </summary>
    public sealed class ClearPlan
    {
        public readonly List<ClearTarget> Targets = new List<ClearTarget>();

        public Player Player;

        /// <summary>The item the clearing is done with (the terrain tool in hand), for the protection rules; null for commands.</summary>
        public string Tool;

        public ClearArea Area;
        public ClearCategory Mask;
        public ClearTools Tools;

        /// <summary>Survival mode with its tool check (admin commands skip the check).</summary>
        public bool CheckTools;

        /// <summary>Objects are chopped, mined and picked through their own game code, so they drop what they normally drop.</summary>
        public bool Drops;

        /// <summary>Set when nothing may be cleared here at all (a dungeon, a protection rule at the centre); then nothing is planned.</summary>
        public string Refusal;

        public int Warded;
        public int NeedAxe;
        public int NeedPickaxe;

        /// <summary>
        /// Survival with drops: every object within the log range around the area when it was planned, so the follow-up
        /// passes know which logs were lying there before the first blow without walking the objects again.
        /// </summary>
        public List<ZNetView> Nearby;

        public bool Empty => Targets.Count == 0;

        /// <summary>A new, empty plan with the same player, tool, area, kinds and mode (a survival follow-up pass).</summary>
        public ClearPlan Again()
        {
            return new ClearPlan { Player = Player, Tool = Tool, Area = Area, Mask = Mask, Tools = Tools, CheckTools = CheckTools, Drops = Drops };
        }
    }

    /// <summary>
    /// Builds a <see cref="ClearPlan"/> from the objects found in an area: keeps those of the requested kinds, leaves
    /// the ones <see cref="ClearRules"/> protects, and in survival mode the ones the player's tools cannot handle.
    /// </summary>
    public static class ClearPlanner
    {
        /// <summary>
        /// Plans the clearing of an area under the server's mode. <paramref name="admin"/> skips the tool check;
        /// <paramref name="tool"/> is the item in hand for an entry, null for a console command.
        /// </summary>
        public static ClearPlan Prepare(Player player, ClearArea area, ClearCategory mask, bool admin, string tool)
        {
            bool survival = ClearingSettings.Survival;
            ClearPlan plan = new ClearPlan
            {
                Player = player, Tool = tool, Area = area, Mask = mask,
                Tools = admin || !survival ? ClearTools.Unlimited() : ClearTools.Of(player),
                CheckTools = survival && !admin,
                Drops = survival && ClearingSettings.SurvivalDrops.Value,
            };
            plan.Refusal = ClearRules.Refusal(player, area.Center, tool);
            if (plan.Refusal == null)
                AddAll(plan, Candidates(plan));
            return plan;
        }

        /// <summary>The objects inside the area; with drops, found within the log range once and kept for the follow-up.</summary>
        private static List<ZNetView> Candidates(ClearPlan plan)
        {
            if (!plan.Drops)
                return ObjectScan.Inside(plan.Area);
            plan.Nearby = ObjectScan.Within(plan.Area.Center, plan.Area.Reach + SurvivalFollowUp.LogRange, null);
            return ObjectScan.Inside(plan.Area, plan.Nearby);
        }

        /// <summary>Adds every object of the plan's kinds that may be cleared.</summary>
        public static void AddAll(ClearPlan plan, List<ZNetView> views)
        {
            foreach (ZNetView view in views)
            {
                if (view != null && view.IsValid())
                    Consider(plan, view);
            }
        }

        private static void Consider(ClearPlan plan, ZNetView view)
        {
            ClearCategory kind = ObjectKinds.Classify(view.gameObject);
            if (kind == ClearCategory.None || (kind & plan.Mask) == 0)
                return;
            if (ClearRules.Protected(plan.Player, view.transform.position, plan.Tool))
            {
                plan.Warded++;
                return;
            }
            bool byHand = view.GetComponent<Pickable>() != null;
            if (plan.CheckTools && !byHand && !plan.Tools.CanClear(kind, ObjectKinds.MinToolTier(view.gameObject)))
            {
                CountMissingTool(plan, kind);
                return;
            }
            plan.Targets.Add(new ClearTarget { View = view, Kind = kind });
        }

        private static void CountMissingTool(ClearPlan plan, ClearCategory kind)
        {
            if (ClearTools.NeedsPickaxe(kind))
                plan.NeedPickaxe++;
            else
                plan.NeedAxe++;
        }
    }
}
