using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// A cabin's shape from what it saves (<see cref="KwCabinSpec"/>), in its kit axes (x from its back end along its frame,
    /// y across, z up from the keel line): on each side at the hull the hull's own planking is its wall, raised where the
    /// hull is lower than the roof by clinker strakes straight up from the sheer (or from the deck, where that is over the
    /// sheer) to the roof's level and no higher, level at the hull's own strakes' heights over the keel line
    /// (<see cref="KwStrakes"/>), a board across a flat stern the hull closes in the same rows; its own walls, solid
    /// (<see cref="KwCabinWalls"/>); its roof (<see cref="KwCabinRoof"/>); and its corner posts (<see cref="KwCabinPosts"/>).
    /// Pure numbers into builders, so every peer, a blueprint and a launched ship draw the same cabin.
    /// </summary>
    public static class KwCabinShape
    {
        /// <summary>The cabin in its looks and what is walked on; one: every look in one builder (a preview). One that cannot be built is a plain frame.</summary>
        public static KwHouseLayers Layers(KwCabinSpec spec, KwBiomeTier tier, bool one = false)
        {
            KwHouseLayers layers = new KwHouseLayers(one);
            if (spec.Unbuilt != null)
            {
                layers.Draw(layers.Wood, b => Frame(b, spec));
                return layers;
            }
            List<Station> raised = Raised(spec);
            if (raised.Exists(s => s.Half[1].y - s.Half[0].y > 0.01f))
                layers.Draw(layers.Hull, b => Sides(b, spec, raised));
            foreach (KwCabinWall wall in KwCabinWalls.Of(spec))
                layers.Draw(layers.Hull, b => KwCabinWallMesh.Draw(b, wall));
            KwCabinRoof.Draw(layers, spec, tier);
            layers.Draw(layers.Wood, b => KwCabinPosts.Draw(b, spec), false);
            return layers;
        }

        /// <summary>
        /// Along the cabin, each section's sheer (or the deck, where that is higher) and the roof's top straight over it: the
        /// hull's sides raised there (the sheer again where the hull already reaches the roof).
        /// </summary>
        public static List<Station> Raised(KwCabinSpec spec) => spec.Unbuilt != null ? new List<Station>() : KwCabinOutline.Inside(spec).ConvertAll(s =>
        {
            Vector2 sheer = s.Half[s.Half.Length - 1];
            float foot = Mathf.Max(sheer.y, spec.Floor);
            return new Station(s.X, new[] { new Vector2(sheer.x, foot), new Vector2(sheer.x, Mathf.Max(spec.Top, foot)) });
        });

        // The raised strakes on each side at the hull, level from the keel line (the cabin's z = 0), closed at both ends,
        // and a board across a flat stern the hull closes, from its sheer to the roof.
        private static void Sides(MeshBuilder b, KwCabinSpec spec, List<Station> raised)
        {
            KwGirth girth = new KwGirth(raised) { Holes = KwOpeningSkin.Holes(KwOpeningSurfaces.RaisedOpenings(spec)) };
            foreach ((KwCabinSide side, float sign) in new[] { (spec.Left, 1f), (spec.Right, -1f) })
            {
                if (!side.AtHull)
                    continue;
                for (int s = 0; s + 1 < raised.Count; s++)
                    KwStrakes.Gap(b, girth, s, sign);
                KwStrakes.EndCap(b, girth, 0, sign);
                KwStrakes.EndCap(b, girth, raised.Count - 1, sign);
            }
            Transom(b, spec, raised[0], spec.Back, -1f);
            Transom(b, spec, raised[raised.Count - 1], spec.Front, 1f);
        }

        // A board across the raised sides at an end the hull closes there: a flat stern's; at a pointed end, as wide as the
        // post the planking ends on, from the deck to the roof, its face a hair inside the hull's end, so the post hides it
        // below the post's top and it closes the gap over a post lower than the roof.
        private static void Transom(MeshBuilder b, KwCabinSpec spec, Station at, KwCabinEnd end, float outward)
        {
            if (end.Hull == KwCabinEnd.Open || !KwCabinOutline.Closed(spec, end) || at.Half[1].y - at.Half[0].y <= 0.05f)
                return;
            KwPlanks.TransomBoard(b, end.Hull == KwCabinEnd.Flat ? at : Pointed(spec, at, outward), outward);
        }

        // A pointed end's board: the raised section from the deck up, a hair inside the hull's end.
        private static Station Pointed(KwCabinSpec spec, Station at, float outward) => new Station(at.X - outward * 0.025f,
            new[] { new Vector2(at.Half[0].x, Mathf.Min(at.Half[0].y, spec.Floor)), at.Half[1] });

        // A cabin no place of which fits (KwCabinFit), so it can still be seen, clicked and taken away: a plain frame over its
        // deck sections as wide as the hull's inside at their middle (0.3 m at least), a post at each corner and a rail round its top.
        private static void Frame(MeshBuilder b, KwCabinSpec spec)
        {
            float half = Mathf.Max(KwCabinOutline.MinWide / 2f, KwShapes.InsideWidth(KwCabinOutline.Up(spec, spec.Length / 2f).Half, spec.Floor + 0.05f));
            float t = KwCabinWall.Thick, x1 = Mathf.Max(spec.Length, KwCabinOutline.MinLength), z0 = spec.Floor - KwCabinOutline.Sink, z1 = spec.Top;
            foreach (float x in new[] { 0f, x1 - t })
            {
                foreach (float y in new[] { -half, half - t })
                    Solid.Box(b, new Vector3(x, y, z0), new Vector3(x + t, y + t, z1));
                Solid.Box(b, new Vector3(x, -half, z1 - t), new Vector3(x + t, half, z1));
            }
            foreach (float y in new[] { -half, half - t })
                Solid.Box(b, new Vector3(0f, y, z1 - t), new Vector3(x1, y + t, z1));
        }

        /// <summary>The raised sides' area in m² (each side at the hull), for the cabin's cost.</summary>
        public static float SidesArea(KwCabinSpec spec)
        {
            List<Station> raised = Raised(spec);
            int sides = (spec.Left.AtHull ? 1 : 0) + (spec.Right.AtHull ? 1 : 0);
            float area = 0f;
            for (int i = 0; i + 1 < raised.Count; i++)
            {
                float a = raised[i].Half[1].y - raised[i].Half[0].y, c = raised[i + 1].Half[1].y - raised[i + 1].Half[0].y;
                area += (a + c) / 2f * (raised[i + 1].X - raised[i].X);
            }
            return area * sides;
        }

        /// <summary>The walls' and raised sides' area in m², for the cabin's cost.</summary>
        public static float WallArea(KwCabinSpec spec)
        {
            float area = SidesArea(spec);
            foreach (KwCabinWall wall in KwCabinWalls.Of(spec))
                area += KwCabinWalls.Area(wall.Outline);
            return area;
        }
    }
}
