using System;
using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// The pieces round the area gathered for what the area keeps clear (DECORATE.md, "Kept clear"), from the game's own
    /// list of pieces (<c>Piece.GetAllPiecesInRadius</c>, once a reading), a batch at a time so a large base never stalls a
    /// frame: 2 m either side of a door; a ladder or stairs and 1.5 m round; 2 m round a crafting station and 3.5 m in
    /// front of it; a chest's front; a bed and 1 m round; a portal's front and back; a sign's front; 1.6 m round a fire,
    /// oven, smelter, fermenter, beehive or cart; a ship and 1.5 m round; the dry dock (its keel block guides and keel
    /// blocks and 4.5 m round, the ship designed on it and 2 m round) as one box, also kept as the area's dry dock. The
    /// decorations already standing keep their spacing (<see cref="KwDecorClearRules"/>). The ways between path ends and
    /// the marking are plain arithmetic after (<see cref="KwDecorClear"/>).
    /// </summary>
    public sealed class KwDecorKeepClear
    {
        private const float Reach = 6f;
        private static readonly string[] Dock = { "KW_dockguide", "KW_keelcradle" };
        private static readonly string[] Designed = { "KW_gen_", "KW_hatch_", "KW_rudder_", "KW_cap_", "KW_bpstation", "KW_helm", "KW_anchor", "KW_nameboard" };
        private readonly List<Piece> near = new List<Piece>();
        private Bounds? dock;
        private int next;

        public KwDecorKeepClear(KwDecorArea area)
        {
            Piece.GetAllPiecesInRadius(area.Grid.Centre, area.Grid.Half * 1.5f + Reach, near);
        }

        /// <summary>A dry dock piece or a part of the ship designed on it (never a floor, kept clear).</summary>
        public static bool DryDock(string name) => Starts(name, Dock) || Starts(name, Designed);

        private static bool Starts(string name, string[] prefixes) => Array.Exists(prefixes, p => name.StartsWith(p, StringComparison.Ordinal));

        /// <summary>A few more pieces read; true once every one is (the dry dock then kept as one box).</summary>
        public bool Step(KwDecorArea area, int count)
        {
            for (int end = Mathf.Min(near.Count, next + count); next < end; next++)
            {
                Piece piece = near[next];
                if (piece == null)
                    continue;
                if (DryDock(piece.name))
                    dock = Grow(dock, piece, Starts(piece.name, Dock) ? 4.5f : 2f);
                else
                    KwDecorClearRules.Add(area, piece);
            }
            if (next < near.Count)
                return false;
            Finish(area);
            return true;
        }

        private void Finish(KwDecorArea area)
        {
            near.Clear();
            if (dock == null)
                return;
            KwDecorBox box = KwDecorBox.Around(dock.Value, 0f);
            area.Kept.Add(box);
            area.DryDock = box;
            area.Yards.Add(dock.Value.center);
        }

        // The dock and its ship as one box, each piece grown by its margin.
        private static Bounds Grow(Bounds? dock, Piece piece, float margin)
        {
            Bounds bounds = KwDecorClearRules.BoundsOf(piece);
            bounds.Expand(margin * 2f);
            if (dock == null)
                return bounds;
            Bounds all = dock.Value;
            all.Encapsulate(bounds);
            return all;
        }
    }
}
