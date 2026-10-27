using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// The line shaft's way (<see cref="KwRudderGear"/>) from the pinion over the rudder's quadrant, forward through the
    /// stern post, under the wheel's deck to the bevel box under the wheel: one straight run when the wheel stands nearly in
    /// line with the shaft's bore (no more than <see cref="Skew"/> off it), else the fewest bends round what stands in the
    /// way (<see cref="KwGearGrid"/>). Every stretch passes the checks of <see cref="KwGearLine"/>. Its hangers go a lead
    /// from each bevel box and evenly between, never more than <see cref="Spacing"/> apart, none short of where the deck
    /// reaches over the shaft. Kit axes of the rudder.
    /// </summary>
    public static class KwRudderGearRoute
    {
        /// <summary>How far off the bore's line the wheel may stand for one straight run (a slope of 5°); the hangers' spacing.</summary>
        public const float Skew = 0.087f, Spacing = 1.6f;

        /// <summary>How far from a bevel box its own hanger stands, behind its wheel.</summary>
        public const float Lead = 0.32f;

        /// <summary>The gear from a pinion to the wheel standing at a point on a deck at a level; null with no clear way.</summary>
        public static KwRudderGear? Find(KwGearShip ship, Vector3 pinion, float post, Vector3 wheel, float level)
        {
            KwGearLine line = new KwGearLine(ship, post, level);
            float? z = line.At(wheel.x, wheel.y);
            if (z == null)
                return null;
            Vector3 under = new Vector3(wheel.x, wheel.y, z.Value);
            if (!line.Corner(under))
                return null;
            List<Vector3>? corners = Lay(line, pinion, under, post);
            return corners == null ? null : KwRudderGear.Shafted(wheel, corners.ToArray(), Hangers(line, corners, post));
        }

        /// <summary>The corners from the pinion to a place under the deck: one straight run when it lies nearly in line with the bore, else the fewest bends; null with no clear way.</summary>
        public static List<Vector3>? Lay(KwGearLine line, Vector3 pinion, Vector3 under, float post) =>
            Direct(line, pinion, under) ?? KwGearGrid.Search(line, pinion, under, post + KwRudderGearFind.StemGap);

        // One straight run from the pinion to under the wheel, when the wheel stands nearly in line with the bore.
        private static List<Vector3>? Direct(KwGearLine line, Vector3 pinion, Vector3 under)
        {
            float along = under.x - pinion.x;
            if (along <= 0f || Mathf.Abs(under.y - pinion.y) > Skew * along || !line.Run(pinion, under))
                return null;
            return new List<Vector3> { pinion, under };
        }

        /// <summary>The hangers along every stretch under the deck, none aft of where the deck reaches over the shaft.</summary>
        public static Vector4[] Hangers(KwGearLine line, List<Vector3> corners, float post)
        {
            List<Vector4> hangers = new List<Vector4>();
            for (int k = 0; k + 1 < corners.Count; k++)
                Along(line, corners[k], corners[k + 1], k == 0 ? post + KwRudderGearFind.StemGap : float.MinValue, hangers);
            return hangers.ToArray();
        }

        /// <summary>Hangers along one stretch: a lead from each end, evenly between, none aft of a place along the ship.</summary>
        public static void Along(IKwGearWay line, Vector3 a, Vector3 b, float aftmost, List<Vector4> hangers)
        {
            float length = (b - a).magnitude;
            Vector3 way = (b - a) / Mathf.Max(length, 1e-4f);
            float from = Mathf.Max(Lead, way.x > 0.01f ? (aftmost - a.x) / way.x : Lead), to = length - Lead;
            if (to < from)
                return;
            int gaps = Mathf.Max(1, Mathf.CeilToInt((to - from) / Spacing));
            for (int i = 0; i <= gaps; i++)
            {
                Vector3 p = a + way * Mathf.Lerp(from, to, i / (float)gaps);
                float? top = line.Over(p);
                if (top != null)
                    hangers.Add(new Vector4(p.x, p.y, p.z, top.Value));
            }
        }
    }

    /// <summary>
    /// What a stretch of the line shaft must pass (<see cref="KwRudderGearRoute"/>): forward of the stern post (aft of it and
    /// in its bore it is clear), inside the hull clear of its ribs (where it comes in through the narrow stern, within
    /// <see cref="Narrow"/> of the post, inside the planking's outer face: it goes through the stern's timber there), out
    /// of every keel, post and cabin on a lower deck, by
    /// every mast and ladder, and with the wheel's deck over it to hang from (never under a hatch's opening) from
    /// <see cref="KwRudderGearFind.StemGap"/> forward of the post on. Its height anywhere is that deck's top less
    /// <see cref="KwGearTrain.Below"/>.
    /// </summary>
    public sealed class KwGearLine : IKwGearWay
    {
        // How far apart a stretch is checked; how far inside the hull's skin it stays; how far round a hatch's opening; how
        // near the wheel's deck's level a deck counts as that one; how far under a deck a hanger may reach further; how much
        // room a bevel box needs round its corner.
        private const float Step = 0.2f, Margin = 0.12f, Hole = 0.15f, Level = 0.35f, Gap = 0.3f, Box = 0.2f, Narrow = 0.5f;

        private readonly KwGearShip ship;
        private readonly float post, level;

        public KwGearLine(KwGearShip ship, float post, float level) => (this.ship, this.post, this.level) = (ship, post, level);

        /// <summary>The shaft's height at a place under the wheel's deck; null off that deck or over an opening.</summary>
        public float? At(float x, float y)
        {
            float? top = ship.Top(x, y, level, level - Level, level + Level, Hole);
            return top.HasValue ? top.Value - KwGearTrain.Below : (float?)null;
        }

        /// <summary>The deck's top over a point of the shaft, where a hanger is fastened; null with none near over it.</summary>
        public float? Over(Vector3 p) =>
            ship.Top(p.x, p.y, p.z + KwGearTrain.Below, p.z + KwGearTrain.Below - 0.04f, p.z + KwGearTrain.Below + Gap, Hole);

        /// <summary>Whether a straight stretch is clear all along.</summary>
        public bool Run(Vector3 a, Vector3 b)
        {
            int steps = Mathf.Max(1, Mathf.CeilToInt((b - a).magnitude / Step));
            for (int i = 0; i <= steps; i++)
            {
                if (!Clear(Vector3.Lerp(a, b, i / (float)steps)))
                    return false;
            }
            return !ship.Blocked(a, b);
        }

        /// <summary>Whether a bevel box fits round a corner: clear a box's room each way, the deck over it.</summary>
        public bool Corner(Vector3 p) =>
            Over(p) != null && Clear(p + new Vector3(Box, 0f, 0f)) && Clear(p - new Vector3(Box, 0f, 0f))
            && Clear(p + new Vector3(0f, Box, 0f)) && Clear(p - new Vector3(0f, Box, 0f));

        // One point of the shaft.
        private bool Clear(Vector3 p)
        {
            if (p.x < post + 0.05f)
                return true;
            float margin = p.x < post + Narrow ? KwGearTrain.Shaft - KwPlanks.Thick : Margin;
            if (Mathf.Abs(p.y) > ship.Inside(p) - margin || ship.Solid(p, KwGearTrain.Shaft + 0.02f))
                return false;
            return p.x < post + KwRudderGearFind.StemGap || Over(p) != null;
        }
    }
}
