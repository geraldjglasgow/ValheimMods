using System;
using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// What stands on a cabin (the user's report, 2026-10-25: "If you build a cabin, then build another cabin on top of it,
    /// and then change the height of the first cabin, the cabin on top needs to be pushed up further too"): a cabin built on
    /// its roof (<see cref="Stands"/>: the Cabin tool builds one on the same keel line, the same way round, its deck the
    /// roof's top, over the roof), the roof railing on its roof, ladders whose top or foot is on its roof, and the kit's other
    /// pieces standing on its roof's top (masts, old hammer caps). The hull's own parts and what hangs on the keels' posts
    /// (rudders, the Post cap tool's caps, bowsprits) never ride on a roof. Pure numbers on parts read once
    /// (<see cref="KwStackPart"/>).
    /// </summary>
    public static class KwCabinStack
    {
        /// <summary>
        /// Metres: how near a piece's foot is to a roof's top to stand on it; how near a ladder's top is to a roof's top and
        /// how far out of its outline it may be (it hangs from the edge); how near a ladder's foot is to a roof under it.
        /// </summary>
        public const float OnTop = 0.12f, LadderTop = 0.15f, LadderEdge = 0.35f, LadderFoot = 0.15f;

        // Off the lower cabin's keel line, its roof's top and the upper cabin's deck apart, the least overlap along; a roof
        // railing's origin from the roof's (KwRoofSite.Railed); a standing piece out of the roof's outline.
        private const float Line = 0.05f, Level = 0.03f, Overlap = 0.05f, Railed = 0.3f, Out = 0.05f;

        private static readonly HashSet<string> Fixed = new HashSet<string>(StringComparer.Ordinal)
        {
            Part("keel"), Part("rib"), Part("planking"), Part("deck"), Part(KwBiomeCost.Rail), Part(KwCapTool.Tool), Part(KwBowsprit.Tool),
            KwShipPrefab.Name,
        };

        private static string Part(string tool) => KwToolPieces.PartPrefix + tool;

        /// <summary>Whether a cabin stands on another's roof: on its keel line the same way round, its deck at the roof's top, over the roof.</summary>
        public static bool Stands(KwStackPart upper, KwStackPart lower)
        {
            if (upper == lower || upper.Cabin == null || lower.Cabin == null || Vector3.Dot(upper.Forward, lower.Forward) < 0.999f)
                return false;
            Vector3 off = upper.Position - lower.Position;
            KwStackFrame frame = lower.At.Frame;
            if (Mathf.Abs(Vector3.Dot(off, frame.Left)) > Line || Mathf.Abs(off.y) > Line || Mathf.Abs(upper.Cabin.Floor + off.y - lower.Cabin.Top) > Level)
                return false;
            float from = Vector3.Dot(off, frame.Forward);
            return Mathf.Min(from + upper.Cabin.X1, lower.Cabin.X1) - Mathf.Max(from + upper.Cabin.X0, lower.Cabin.X0) > Overlap;
        }

        /// <summary>The cabin among the parts whose roof a cabin stands on; null for one on a deck.</summary>
        public static KwStackPart? Under(KwStackPart cabin, List<KwStackPart> parts) => parts.Find(lower => Stands(cabin, lower));

        /// <summary>The roof railing on a cabin's roof (at its origin on the roof's top), or null.</summary>
        public static KwStackPart? Railing(KwStackPart cabin, List<KwStackPart> parts)
        {
            Vector3 origin = new Vector3(cabin.Position.x, cabin.At.Top, cabin.Position.z);
            return parts.Find(part => part.RoofRail != null && (part.Position - origin).sqrMagnitude < Railed * Railed);
        }

        /// <summary>The kit's pieces standing on a cabin's roof's top (<see cref="Rides"/>).</summary>
        public static List<KwStackPart> Standing(KwStackPart cabin, List<KwStackPart> parts)
        {
            KwCabinAt at = cabin.At;
            return parts.FindAll(part => Rides(part) && at.OnRoof(part.Position, Out, OnTop));
        }

        /// <summary>Whether a part may ride on a roof: not a cabin, railing or ladder (each has its own rule), nor a hull part or a post's fitting.</summary>
        public static bool Rides(KwStackPart part) =>
            part.Cabin == null && part.RoofRail == null && !part.IsLadder && !Fixed.Contains(part.Prefab) && !KwHouses.IsPart(part.Prefab)
            && !part.Prefab.StartsWith(KwRudder.Prefix, StringComparison.Ordinal);

        /// <summary>A ladder's top (its piece's place, on the edge it hangs from) and the point it stands on, a little out from the edge.</summary>
        public static (Vector3 Top, Vector3 Foot) Ends(KwStackPart ladder) =>
            (ladder.Position, ladder.Position + ladder.Forward * KwLadderDrop.Inset + Vector3.down * ladder.Length);

        /// <summary>The cabin among them whose roof a ladder comes up on (its top on the roof's edge, or within its outline).</summary>
        public static KwStackPart? TopOn(KwStackPart ladder, List<KwStackPart> cabins) =>
            cabins.Find(cabin => cabin.At.OnRoof(ladder.Position, LadderEdge, LadderTop));

        /// <summary>The cabin among them whose roof a ladder stands on.</summary>
        public static KwStackPart? FootOn(KwStackPart ladder, List<KwStackPart> cabins)
        {
            Vector3 foot = Ends(ladder).Foot;
            return cabins.Find(cabin => cabin.At.OnRoof(foot, LadderFoot, LadderFoot));
        }
    }
}
