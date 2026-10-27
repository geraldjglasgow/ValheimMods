using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// A deck of a launched ship as the Doorway and Window tools ask it at sea: its piece, its top's height along it and its
    /// half width there (its outline inside the planking, as its boxes lie, <see cref="KwShipColliders"/>), from the
    /// blueprint's seated span. Pure.
    /// </summary>
    public sealed class KwSeaOpeningDeck
    {
        private readonly List<Vector2> edge;
        private readonly System.Func<float, float> z;

        private KwSeaOpeningDeck(KwBpPiece piece, KwDef def)
        {
            Piece = piece;
            z = KwMesh.DeckHeight(def);
            edge = KwShapes.DeckEdge(def.Stations, z, KwShapes.InsideWidth);
        }

        public KwBpPiece Piece { get; }

        /// <summary>The blueprint's decks.</summary>
        public static List<KwSeaOpeningDeck> Of(KwBlueprint blueprint)
        {
            List<KwSeaOpeningDeck> decks = new List<KwSeaOpeningDeck>();
            foreach ((KwBpPiece piece, string tool, KwSpan span) in KwBpHull.Parts(blueprint))
            {
                if (tool == "deck" && !KwSeaApply.IsGone(piece))
                    decks.Add(new KwSeaOpeningDeck(piece, span.Def(tool)));
            }
            return decks;
        }

        /// <summary>The deck's top under or over a point of the ship's frame (its height there); null off its outline.</summary>
        public float? Top(Vector3 at)
        {
            Vector3 kit = KwMesh.ToKit(KwSeaApply.Back(Piece.Rotation) * (at - Piece.Position));
            float? half = Half(kit.x);
            if (half == null || Mathf.Abs(kit.y) > half.Value)
                return null;
            return (Piece.Position + Piece.Rotation * KwMesh.ToUnity(new Vector3(kit.x, kit.y, z(kit.x)))).y;
        }

        // The outline's half width at a place along (between its points); null past its ends.
        private float? Half(float x)
        {
            for (int i = 0; i + 1 < edge.Count; i++)
            {
                Vector2 a = edge[i], c = edge[i + 1];
                if (x >= Mathf.Min(a.x, c.x) && x <= Mathf.Max(a.x, c.x))
                    return Mathf.Abs(c.x - a.x) < 1e-5f ? Mathf.Max(a.y, c.y) : Mathf.Lerp(a.y, c.y, (x - a.x) / (c.x - a.x));
            }
            return null;
        }
    }

    /// <summary>
    /// What the Doorway and Window tools ask the world at sea, answered from a launched ship's blueprint in its own frame
    /// (on the dock, rays in the world: <see cref="KwOpeningRays"/>, <see cref="KwOpeningWater"/>): the deck inside under a
    /// place on planking (the highest deck top or cabin roof under a point just inboard of it, within 8 m, as the dock's
    /// ray down finds it); whether an opening on planking would cut through a deck or a cabin's roof inside the hull (the
    /// same three places along, from its frame's top to its foot); and whether its sill would stand under the ship's
    /// design waterline (level in the ship's frame, as its openings' flooding takes it, <see cref="KwShipOpeningPlan"/>).
    /// Pure.
    /// </summary>
    public static class KwSeaOpeningFloor
    {
        // As the dock's rays: how far inboard of planking they look, how far down, a deck's thickness.
        private const float Inboard = 0.25f, Beside = 0.12f, Down = 8f, Thick = 0.1f;

        /// <summary>The deck inside under a place on a site's wall (the part's kit z): a cabin's own floor; on planking the deck under it; null for none.</summary>
        public static float? Floor(KwSeaOpeningShip ship, KwSeaOpeningPart part, KwOpeningSite site, float u, float z)
        {
            if (site.Spec != null)
                return site.Spec.Floor;
            Vector3 at = part.Ship(site.Surface.Point(u, z + 0.05f, -site.Surface.Thick - Inboard));
            float? top = Under(ship, at);
            return top == null ? (float?)null : part.Kit(new Vector3(at.x, top.Value, at.z)).z;
        }

        /// <summary>The highest deck top or cabin roof under a point of the ship's frame, within 8 m; null for none.</summary>
        public static float? Under(KwSeaOpeningShip ship, Vector3 at)
        {
            float? best = null;
            foreach (KwSeaOpeningDeck deck in ship.Decks)
                best = Higher(best, deck.Top(at), at.y);
            foreach (KwSeaOpeningPart part in ship.Parts)
                best = Higher(best, Roof(part, at), at.y);
            return best;
        }

        // The higher of two tops, a top counted only at or under the point and within reach of it.
        private static float? Higher(float? best, float? top, float y)
        {
            if (top == null || top.Value > y + 0.001f || y - top.Value > Down)
                return best;
            return best == null || top.Value > best.Value ? top : best;
        }

        // A cabin's roof's top over or under a point (its height in the ship's frame); null off its outline or for planking.
        private static float? Roof(KwSeaOpeningPart part, Vector3 at)
        {
            if (part.Planking || !KwCabinSpec.TryParse(part.Saved, out KwCabinSpec spec))
                return null;
            Vector3 kit = part.Kit(at);
            if (kit.x < spec.X0 - 0.05f || kit.x > spec.X1 + 0.05f)
                return null;
            float left = KwCabinOutline.Reach(spec, spec.Left, kit.x, spec.Top), right = KwCabinOutline.Reach(spec, spec.Right, kit.x, spec.Top);
            if (kit.y > left + 0.05f || kit.y < -right - 0.05f)
                return null;
            return part.Ship(new Vector3(kit.x, kit.y, spec.Top)).y;
        }

        /// <summary>Whether an opening on planking would cut through a deck or a cabin's roof inside the hull (a doorway's own floor left out).</summary>
        public static bool Crosses(KwSeaOpeningShip ship, KwOpeningSite site, KwOpening o)
        {
            KwSeaOpeningPart? part = KwSeaOpeningPart.Of(site);
            if (part == null || !site.Planking)
                return false;
            float wide = KwOpeningFrames.Width(o.Frame).Wide, low = o.IsDoorway ? o.Sill + 0.06f : o.Sill - wide, high = o.Top + wide;
            foreach (float u in new[] { o.U0 + 0.02f, o.U, o.U1 - 0.02f })
            {
                Vector3 a = part.Ship(site.Surface.Point(u, high, -site.Surface.Thick - Beside));
                Vector3 c = part.Ship(site.Surface.Point(u, low, -site.Surface.Thick - Beside));
                if (Meets(ship, (a + c) / 2f, Mathf.Min(a.y, c.y), Mathf.Max(a.y, c.y)))
                    return true;
            }
            return false;
        }

        // Whether a deck's top or underside, or a cabin roof's top or underside, lies between two heights at a place.
        private static bool Meets(KwSeaOpeningShip ship, Vector3 at, float lo, float hi)
        {
            foreach (KwSeaOpeningDeck deck in ship.Decks)
            {
                if (deck.Top(at) is float top && Within(top, lo, hi, Thick))
                    return true;
            }
            foreach (KwSeaOpeningPart part in ship.Parts)
            {
                if (Roof(part, at) is float roof && Within(roof, lo, hi, KwPlanks.Thick))
                    return true;
            }
            return false;
        }

        private static bool Within(float top, float lo, float hi, float thick) => (top >= lo && top <= hi) || (top - thick >= lo && top - thick <= hi);

        /// <summary>Whether an opening's sill, at its middle along, would stand under the ship's design waterline.</summary>
        public static bool Below(KwSeaOpeningShip ship, KwOpeningSite site, KwOpening o)
        {
            KwSeaOpeningPart? part = KwSeaOpeningPart.Of(site);
            return part != null && part.Ship(site.Surface.Point(o.U, o.Sill, 0f)).y < ship.Waterline;
        }
    }
}
