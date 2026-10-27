using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// What a hull part costs in its tier's two materials, by its size: planking by the area of its skin (both sides),
    /// a deck by the area of its top, one of the bulk material for every 2 m² and one of the fittings for every 4 m²; a
    /// keel by its length and its post's height in metres of the standard keel (0.3 by 0.35 m), one of the bulk a metre
    /// and one of the fittings every 2 m; a rib by the length of its frame, one of the bulk every 2 m and one of the
    /// fittings every 4 m; a railing by its length along the ship (half that for one side only, <see cref="KwRailText"/>; a
    /// roof railing by its railed edges, <see cref="KwRoofRail"/>),
    /// in the planking's two materials of the tier it is given (its own; for one saving none <see cref="KwBpCost"/> gives
    /// it the tier of the planking it stands on), one of the bulk every
    /// 2 m and one of the fittings every 4 m. At least one of each. Worked out from what the part saves, so every peer, a
    /// blueprint and a refund agree.
    /// </summary>
    public static class KwBiomeCost
    {
        public const string Rail = "rail";
        private const float StandardArea = 2f * KwKeel.HalfWidth * KwKeel.Depth;

        /// <summary>
        /// A generated part's cost in a tier, from its saved span or sizes: the part's own tier for one the tiers dress, the
        /// one its planking has for a railing; a cabin in both its tier's hull and deck materials
        /// (<see cref="KwHouseCost"/>); empty for any other part, or one whose saved text does not read.
        /// </summary>
        public static List<(string Item, int Amount)> Of(string tool, string saved, KwBiomeTier tier)
        {
            if (KwHouses.Owns(tool))
                return KwHouseCost.Of(tool, saved, tier);
            KwPart? part = tool == Rail ? KwPart.Hull : KwBiomeTiers.PartOf(tool);
            float size = part == null ? 0f : Size(tool, saved);
            return size > 0f ? For(part!.Value, tier, size) : new List<(string Item, int Amount)>();
        }

        /// <summary>The cost of a part of this size (m² for planking and decks, metres for keels, ribs and railings).</summary>
        public static List<(string Item, int Amount)> For(KwPart part, KwBiomeTier tier, float size)
        {
            (float main, float trim) = part == KwPart.Keel ? (1f, 0.5f) : (0.5f, 0.25f);
            (string bulk, string fittings) = tier.Materials(part);
            return new List<(string Item, int Amount)> { (bulk, Amount(size * main)), (fittings, Amount(size * trim)) };
        }

        /// <summary>A keel's size in metres of the standard keel: its length and its post's height, by its thickness.</summary>
        public static float KeelSize(float length, float post, KwKeelBody body) => (length + post) * 2f * body.Half * body.Depth / StandardArea;

        private static int Amount(float share) => Mathf.Max(1, Mathf.CeilToInt(share - 0.001f));

        private static float Size(string tool, string saved)
        {
            if (tool == KwShapeTool.Keel)
                return KwKeelSpec.TryParse(saved, out KwKeelSpec keel) ? KeelSize(keel.Length, keel.Post ? keel.PostAbove : 0f, keel.Body) : 0f;
            // a rib at half a keel's rate: its frame's length on both sides and across the keel
            if (tool == KwShapeTool.Rib)
                return KwRibs.TryRead(saved, out string family, out KwFrame frame) ? RibLength(KwJoints.Section(family, frame)) / 2f : 0f;
            if (tool == Rail && KwRoofRail.TryParse(saved, out KwRoofRail roof))
                return roof.Length;
            if (!KwSpan.TryParse(saved, out KwSpan span))
                return 0f;
            if (tool == Rail)
                return Along(span.Stations()) * KwRailText.Share(span.Rail);
            KwDef def = span.Def(tool);
            return tool == "deck" ? DeckArea(def) : SkinArea(def.Stations);
        }

        // A railing's length along the ship: from its first station to its last, as its rails and posts run.
        private static float Along(List<Station> stations) => stations.Count > 1 ? stations[stations.Count - 1].X - stations[0].X : 0f;

        // The skin's area on both sides: each station's girth, averaged between stations, times the distance between them.
        private static float SkinArea(List<Station> stations)
        {
            float area = 0f;
            for (int s = 0; s + 1 < stations.Count; s++)
                area += (Girth(stations[s].Half) + Girth(stations[s + 1].Half)) * (stations[s + 1].X - stations[s].X);
            return area;
        }

        private static float DeckArea(KwDef def)
        {
            List<Vector2> edge = KwShapes.DeckEdge(def.Stations, KwMesh.DeckHeight(def), KwShapes.InsideWidth);
            float area = 0f;
            for (int i = 0; i + 1 < edge.Count; i++)
                area += (edge[i].y + edge[i + 1].y) * (edge[i + 1].x - edge[i].x);
            return area;
        }

        // A rib frame's length: both its sides along the section and the floor timber across the keel.
        private static float RibLength(Vector2[] half) => 2f * Girth(half) + 2f * (half[0].x + 0.06f);

        private static float Girth(Vector2[] half)
        {
            float girth = 0f;
            for (int i = 0; i + 1 < half.Length; i++)
                girth += Vector2.Distance(half[i], half[i + 1]);
            return girth;
        }
    }
}
