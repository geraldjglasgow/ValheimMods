using System.Collections.Generic;
using OpenKeep.Core;
using UnityEngine;

namespace OpenKeep.Blueprints
{
    /// <summary>
    /// Fix ground for one building, checked: the pieces, those touching the ground, the ground work (pad under them,
    /// slopes rounded off around them), its stone, and the first reason it cannot be done now.
    /// </summary>
    public sealed class GroundFixPlan
    {
        public Piece Start;
        public List<FixPiece> Building;
        internal GroundFixTarget Target;
        public GroundWork Work;
        public MaterialBill Bill;
        public Rect Area;

        /// <summary>Why it cannot be done now (localized), or null.</summary>
        public string Problem;

        public int TouchingCount => Target.Touching.Count;

        public static GroundFixPlan Make(Piece start, Player player)
        {
            GroundFixPlan plan = new GroundFixPlan { Start = start, Building = GroundFixBuilding.From(start) };
            plan.Target = GroundFixTarget.From(plan.Building);
            plan.Bill = MaterialBill.ForGround(player);
            if (plan.Target.Touching.Count == 0)
            {
                plan.Work = new GroundWork();
                plan.Problem = Language.Localize(BlueprintWords.FixNothing);
                return plan;
            }
            Rect touched = plan.Target.Area;
            plan.Area = Rect.MinMaxRect(touched.xMin - BlueprintRules.SkirtReach, touched.yMin - BlueprintRules.SkirtReach,
                touched.xMax + BlueprintRules.SkirtReach, touched.yMax + BlueprintRules.SkirtReach);
            plan.Work = GroundFit.Plan(GroundGrid.Sample(plan.Area, plan.Target), BlueprintRules.FixSmoothPasses);
            plan.Bill.AddGround(plan.Work);
            plan.Problem = BlueprintSafe.Call("OpenKeep fix ground check", () => plan.FirstProblem(player), null);
            return plan;
        }

        private string FirstProblem(Player player)
        {
            if (!BlueprintSettings.Enabled)
                return Language.Localize(BlueprintWords.Disabled);
            if (Work.Unloaded > 0)
                return Language.Localize(BlueprintWords.Unloaded);
            if (Work.Points.Count == 0)
                return Language.Localize(BlueprintWords.FixFine);
            string protection = SiteProtection.Reason(Start.transform.position, Area, Work, Target.Under);
            if (protection != null)
                return Language.Localize(protection);
            string lacking = Bill.Shortfall(player);
            return lacking != null ? BlueprintWords.Format(BlueprintWords.Lacking, lacking) : null;
        }

        /// <summary>Clears what stands on moving ground, pays the stone, sends the ground and keeps it for Alt+Z.</summary>
        public void Apply(Player player)
        {
            int cleared = SiteClearing.Clear(Area, Work, point => false);
            Bill.Charge(player);
            int compilers = GroundWriter.Send(Work);
            GroundUndo.Record(Work, fix: true);
            Messages.Center(BlueprintWords.Format(BlueprintWords.FixDone, TouchingCount, Work.Cut.ToString("0"), Work.Fill.ToString("0")));
            Plugin.Log.LogInfo($"OpenKeep: fixed the ground of {Building.Count} pieces ({TouchingCount} touching): {cleared} objects cleared, " +
                $"{Work.Points.Count} ground points sent to {compilers} terrain compilers");
        }
    }
}
