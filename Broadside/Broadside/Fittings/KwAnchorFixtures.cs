using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// What stands on and round a ship besides its hull, railings, decks and cabins, as an anchor's cable must miss it
    /// (<see cref="KwAnchorShip"/>), from blueprint pieces alone: masts and ladders as columns, a keel's post as its box, a
    /// bowsprit as its spar, a hatch as its coaming with its lid open over it, a rudder round its post, a post cap, another
    /// windlass, and any other kit fitting as the box round its prefab's meshes (a captain's wheel, a name board). The
    /// windlass's own frame (sleepers, cheeks, cranks, rail and the drum's flanges) is in <see cref="Own"/>, kept out of its
    /// cable's way.
    /// </summary>
    public static class KwAnchorFixtures
    {
        // A ladder's half width with its rails; the box round a post cap, any carved head included; a rudder's reach aft of
        // its post, forward over the deck, to its sides, and over its post's top (its head and tiller).
        private const float Ladder = 0.3f, CapLong = 0.8f, CapWide = 0.45f, CapHigh = 1.8f;
        private const float RudderAft = 2.2f, RudderFore = 1.2f, RudderWide = 0.6f, RudderOver = 1.5f;

        private static readonly Dictionary<string, (Vector3 Min, Vector3 Max)?> Boxes = new Dictionary<string, (Vector3 Min, Vector3 Max)?>();

        /// <summary>A piece's solids, if it is one of these; none for the hull's own parts and what is not in the way.</summary>
        public static void Add(List<KwAnchorSolid> into, KwBpPiece piece, KwAnchorPose pose, KwBlueprint ship)
        {
            if (KwHatch.Opening(piece.Prefab) is Vector2 opening)
                Hatch(into, pose, opening, piece.Prefab == KwHatch.Prefix + "large");
            else if (Of(piece, pose, ship) is KwAnchorSolid solid)
                into.Add(solid);
        }

        private static KwAnchorSolid? Of(KwBpPiece piece, KwAnchorPose pose, KwBlueprint ship)
        {
            string prefab = piece.Prefab, saved = KwShipParts.Saved(piece);
            if (prefab == KwSailTool.Made)
                return Mast(pose, saved);
            if (prefab == KwToolPieces.PartPrefix + KwLadderTool.Tool)
                return KwLadder.TryLength(saved, out float length) ? new KwAnchorColumn("a ladder", pose.Out(Vector3.zero) + new Vector3(0f, 0f, 0.12f), -length - 0.12f, Ladder) : null;
            if (prefab == KwKeel.MadeName)
                return Post(pose, saved);
            if (prefab == KwBowsprit.Made)
                return KwBowsprit.TryParse(saved, out KwBowsprit sprit) ? new KwAnchorRod("the bowsprit", pose, sprit.Along(-KwBowsprit.Heel), sprit.Tip, sprit.Radius + 0.03f) : null;
            if (KwRudder.Is(prefab))
                return Rudder(pose, KwShipParts.Fit(ship, piece));
            if (KwCaps.Is(prefab))
                return new KwAnchorBox("a post cap", pose, new Vector3(-CapLong, -CapWide, -0.3f), new Vector3(CapLong, CapWide, CapHigh));
            if (KwAnchor.Is(prefab))
                return new KwAnchorBox("another windlass", pose, new Vector3(-0.36f, -0.84f, 0f), new Vector3(0.36f, 0.84f, 0.9f));
            return Kit(prefab) is (Vector3 min, Vector3 max) ? new KwAnchorBox(Name(prefab), pose, min, max) : null;
        }

        /// <summary>The windlass's own frame round its drum, which its cable leaves past (windlass kit).</summary>
        public static List<KwAnchorSolid> Own()
        {
            KwAnchorPose at = new KwAnchorPose(Vector3.zero, Quaternion.identity);
            List<KwAnchorSolid> own = new List<KwAnchorSolid>();
            foreach (float s in new[] { -1f, 1f })
            {
                own.Add(Box(at, -0.34f, 0.34f, s * 0.38f, s * 0.54f, 0f, 0.08f));
                own.Add(Box(at, -0.17f, 0.17f, s * 0.40f, s * 0.52f, 0.08f, 0.80f));
                own.Add(Box(at, -0.03f, 0.03f, s * 0.60f, s * 0.64f, KwAnchorShapes.Axle - 0.045f, KwAnchorShapes.Axle + 0.30f));
                own.Add(Box(at, -0.03f, 0.03f, s * 0.62f, s * 0.82f, KwAnchorShapes.Axle + 0.245f, KwAnchorShapes.Axle + 0.295f));
                own.Add(new KwAnchorDisc("the windlass's drum", s * KwAnchorShapes.FlangeIn, s * KwAnchorShapes.FlangeOut, KwAnchorShapes.Flange));
            }
            own.Add(Box(at, -0.06f, 0.06f, -0.52f, 0.52f, 0.80f, 0.88f));
            return own;
        }

        private static KwAnchorBox Box(KwAnchorPose at, float x0, float x1, float y0, float y1, float z0, float z1) =>
            new KwAnchorBox("the windlass", at, new Vector3(Mathf.Min(x0, x1), Mathf.Min(y0, y1), z0), new Vector3(Mathf.Max(x0, x1), Mathf.Max(y0, y1), z1));

        // A mast: its height the first number of its text ("mast|height|..."), its radius from it.
        private static KwAnchorSolid? Mast(KwAnchorPose pose, string saved)
        {
            string[] p = saved.Split('|');
            if (p.Length < 2 || p[0] != "mast" || !float.TryParse(p[1], NumberStyles.Float, CultureInfo.InvariantCulture, out float height) || height <= 0f)
                return null;
            return new KwAnchorColumn("the mast", pose.Out(Vector3.zero), height, KwSailLayout.RadiusOf(height) + 0.04f);
        }

        // A keel's post, from its foot under the keel's tip up to its flat top (as KwKeel draws it, its mesh about its pivot).
        private static KwAnchorSolid? Post(KwAnchorPose pose, string saved)
        {
            if (!KwKeelSpec.TryParse(saved, out KwKeelSpec spec) || !spec.Post)
                return null;
            KwKeelBody body = spec.Body;
            float x = spec.Length + (spec.Flat ? body.PostDepth / 2f : -body.PostDepth / 2f), top = spec.RiseAt(x) + spec.PostAbove + body.Depth;
            Vector3 min = new Vector3(x - body.PostDepth / 2f, -body.PostHalf, top - spec.PostAbove - body.Depth), max = new Vector3(x + body.PostDepth / 2f, body.PostHalf, top);
            return new KwAnchorBox("the stem post", pose, min, max);
        }

        // A hatch's coaming round its opening, and its lid standing open on its aft hinge or its doors on their side hinges
        // (opened past upright, KwHatchCover's 110 degrees, so leaning out over the hinge).
        private static void Hatch(List<KwAnchorSolid> into, KwAnchorPose pose, Vector2 opening, bool doors)
        {
            float hx = opening.x / 2f + KwHatch.Rim, hy = opening.y / 2f + KwHatch.Rim, top = KwHatch.Above, leaf = KwHatch.Lid + 0.05f;
            into.Add(new KwAnchorBox("a hatch", pose, new Vector3(-hx, -hy, -KwHatch.Below), new Vector3(hx, hy, top)));
            float lean = Mathf.Sin(20f * Mathf.Deg2Rad);
            if (!doors)
            {
                into.Add(new KwAnchorBox("a hatch's lid", pose, new Vector3(-hx - 2f * hx * lean - leaf, -hy, top), new Vector3(-hx + leaf, hy, top + 2f * hx)));
                return;
            }
            foreach (float s in new[] { -1f, 1f })
            {
                float a = s * (hy - leaf), b = s * (hy + hy * lean + leaf);
                into.Add(new KwAnchorBox("a hatch's door", pose, new Vector3(-hx, Mathf.Min(a, b), top), new Vector3(hx, Mathf.Max(a, b), top + hy)));
            }
        }

        // A rudder round its post: its blade aft, its head and tiller over the post's top and forward over the deck.
        private static KwAnchorSolid Rudder(KwAnchorPose pose, KwRudderFit fit) =>
            new KwAnchorBox("the rudder", pose, new Vector3(-RudderAft, -RudderWide, -0.5f), new Vector3(RudderFore, RudderWide, fit.Height + RudderOver));

        // Another kit fitting: the box round its prefab's meshes (kit axes), made once; none for the hull's generated parts and the dock.
        private static (Vector3 Min, Vector3 Max)? Kit(string prefab)
        {
            if (prefab.StartsWith(KwToolPieces.PartPrefix, System.StringComparison.Ordinal) || !KwBlueprintStation.IsShipPiece(prefab))
                return null;
            if (!Boxes.TryGetValue(prefab, out (Vector3 Min, Vector3 Max)? box))
                Boxes[prefab] = box = Measure(prefab);
            return box;
        }

        private static (Vector3 Min, Vector3 Max)? Measure(string prefab)
        {
            GameObject? made = ZNetScene.instance != null ? ZNetScene.instance.GetPrefab(prefab) : null;
            if (made == null)
                return null;
            Vector3 min = Vector3.positiveInfinity, max = Vector3.negativeInfinity;
            foreach (MeshFilter filter in made.GetComponentsInChildren<MeshFilter>(true))
            {
                Bounds b = filter.sharedMesh != null ? filter.sharedMesh.bounds : default;
                for (int i = 0; i < 8 && filter.sharedMesh != null; i++)
                {
                    Vector3 corner = b.center + Vector3.Scale(b.extents, new Vector3((i & 1) == 0 ? -1f : 1f, (i & 2) == 0 ? -1f : 1f, (i & 4) == 0 ? -1f : 1f));
                    Vector3 kit = KwMesh.ToKit(made.transform.InverseTransformPoint(filter.transform.TransformPoint(corner)));
                    (min, max) = (Vector3.Min(min, kit), Vector3.Max(max, kit));
                }
            }
            return min.x <= max.x ? (min, max) : ((Vector3, Vector3)?)null;
        }

        private static string Name(string prefab) => prefab == KwWheel.Prefab ? "the captain's wheel" : prefab == KwNameBoard.Prefab ? "a name board" : "a fitting";
    }
}
