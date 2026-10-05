using System.Linq;
using OpenKeep.Core;
using UnityEngine;

namespace OpenKeep.Blueprints
{
    /// <summary>A blueprint at one spot, checked: its ground work, its bill, what stands in the way, and the first reason it cannot be built now.</summary>
    public sealed class SitePlan
    {
        public Blueprint Blueprint;
        public BuildFrame Frame;
        public GroundWork Work;
        public MaterialBill Bill;

        /// <summary>Player-built pieces standing where the blueprint's pieces go.</summary>
        public int InTheWay;

        /// <summary>The water rule moved the floor from where the player aimed.</summary>
        public bool WaterMoved;

        /// <summary>Why it cannot be built here now (localized), or null.</summary>
        public string Problem;
    }

    /// <summary>
    /// Checks a blueprint at a spot for the local player, in the order the reasons are worth reading: the switch,
    /// pieces this game does not have, ground not loaded, pieces in the way, protection over the site (dungeons,
    /// no-build places, wards), pieces not learned, and materials.
    /// </summary>
    public static class SitePlanner
    {
        /// <summary>The checked plan. <paramref name="needMaterials"/> false: missing materials are not a problem (a site collects them later).</summary>
        public static SitePlan Plan(Blueprint bp, BuildFrame frame, bool waterMoved, Player player, bool needMaterials = true)
        {
            SitePlan plan = new SitePlan { Blueprint = bp, Frame = frame, WaterMoved = waterMoved };
            plan.Work = GroundFit.Plan(bp, frame);
            plan.Bill = MaterialBill.For(bp, player);
            plan.Bill.AddGround(plan.Work);
            plan.InTheWay = BuiltPiecesIn(bp, frame);
            plan.Problem = BlueprintSafe.Call("OpenKeep blueprint check", () => FirstProblem(plan, player, needMaterials), null);
            return plan;
        }

        private static string FirstProblem(SitePlan plan, Player player, bool needMaterials)
        {
            if (!BlueprintSettings.Enabled)
                return Language.Localize(BlueprintWords.Disabled);
            if (plan.Bill.Missing.Count > 0)
                return BlueprintWords.Format(BlueprintWords.Missing, string.Join(", ", plan.Bill.Missing.Take(5)));
            if (plan.Work.Unloaded > 0)
                return Language.Localize(BlueprintWords.Unloaded);
            if (plan.InTheWay > 0)
                return BlueprintWords.Format(BlueprintWords.InTheWay, plan.InTheWay);
            string protection = SiteProtection.Reason(plan);
            if (protection != null)
                return Language.Localize(protection);
            if (plan.Bill.Unlearned.Count > 0)
                return BlueprintWords.Format(BlueprintWords.Unlearned, string.Join(", ", plan.Bill.Unlearned.Take(5).Select(n => Localization.instance.Localize(n))));
            string lacking = needMaterials ? plan.Bill.Shortfall(player) : null;
            return lacking != null ? BlueprintWords.Format(BlueprintWords.Lacking, lacking) : null;
        }

        /// <summary>Player-built pieces whose pivot lies within the blueprint's pieces' frame rectangle (plus a metre).</summary>
        public static int BuiltPiecesIn(Blueprint bp, BuildFrame frame)
        {
            Rect area = bp.PieceBounds;
            area = Rect.MinMaxRect(area.xMin - 1f, area.yMin - 1f, area.xMax + 1f, area.yMax + 1f);
            int count = 0;
            foreach (Piece piece in Piece.s_allPieces)
            {
                if (piece == null || !piece.IsPlacedByPlayer())
                    continue;
                Vector3 p = piece.transform.position;
                if (area.Contains(frame.Local(p.x, p.z)) && Mathf.Abs(p.y - frame.Ground) < 60f)
                    count++;
            }
            return count;
        }
    }
}
