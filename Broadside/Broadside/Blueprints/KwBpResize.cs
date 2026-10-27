using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// A saved ship made wider or scaled as a whole, for <see cref="KwBpReshape"/>. Wider: every rib's and hull part's
    /// beam times the factor (<see cref="KwBpPartText.Widened"/>), and every piece moved out from the centre line by it, so
    /// a ladder over the side or a hatch off the middle keeps its place on the hull; the heights and lengths stay. Scaled:
    /// every size times the factor (<see cref="KwBpPartText.Scaled"/>: keels, ribs, hull parts, masts and sails, ladders,
    /// bowsprits, cabins; the timbers' thickness kept), every piece moved out from the centre line, up from the
    /// keel line and forward from the stern by it, so the ship stays on its keel blocks with its stern where it was. A
    /// keel hangs its own timber's depth under the keel line as before, and a post cap or a bowsprit (on a post's top) or
    /// a rudder (at its rudder point) goes where that point of its post now is (<see cref="KwBpPosts"/>). A Post cap tool cap
    /// scales with the ship (<see cref="KwCapSpec.Scaled"/>); other fittings (old hammer caps, hatches, rudders) keep their size. A rudder's measured link to the deck is dropped either way: its owner
    /// measures the new stern again once it is built.
    /// </summary>
    internal static class KwBpResize
    {
        public static KwBlueprint? Widen(KwBlueprint blueprint, float w, out string? error)
        {
            error = null;
            KwBlueprint wider = new KwBlueprint(blueprint.Name);
            foreach (KwBpPiece piece in blueprint.Pieces)
            {
                string? text = KwBpPartText.Widened(KwBpPartText.Tool(piece), KwShipParts.Saved(piece), w, out error);
                if (text == null)
                    return null;
                Vector3 at = piece.Position;
                wider.Pieces.Add(KwBpPartText.Moved(piece, new Vector3(at.x * w, at.y, at.z), text, KwRudder.Is(piece.Prefab)));
            }
            return wider;
        }

        public static KwBlueprint? Scale(KwBlueprint blueprint, float s, out string? error)
        {
            KwBpHull hull = KwBpHull.Measure(blueprint);
            KwBpPosts posts = new KwBpPosts();
            string?[]? texts = Texts(blueprint, hull, s, posts, out error);
            if (texts == null)
                return null;
            KwBlueprint scaled = new KwBlueprint(blueprint.Name);
            for (int i = 0; i < texts.Length; i++)
            {
                KwBpPiece piece = blueprint.Pieces[i];
                bool fitting = KwRudder.Is(piece.Prefab) || piece.Prefab == KwBowsprit.Made
                    || KwCaps.Is(piece.Prefab);
                Vector3 at = (fitting ? posts.Moved(piece) : null) ?? Place(piece, hull, s);
                scaled.Pieces.Add(KwBpPartText.Moved(piece, at, texts[i], KwRudder.Is(piece.Prefab)));
            }
            return scaled;
        }

        // Every piece's saved text scaled (null and why when one cannot be), and every keel's post as it was and is now.
        private static string?[]? Texts(KwBlueprint blueprint, KwBpHull hull, float s, KwBpPosts posts, out string? error)
        {
            string?[] texts = new string?[blueprint.Pieces.Count];
            error = null;
            for (int i = 0; i < texts.Length; i++)
            {
                KwBpPiece piece = blueprint.Pieces[i];
                texts[i] = KwBpPartText.Scaled(KwBpPartText.Tool(piece), KwShipParts.Saved(piece), s, out error);
                if (texts[i] == null)
                    return null;
                if (KwBpPartText.Tool(piece) == "keel")
                    posts.Add(piece, Place(piece, hull, s), texts[i]!);
            }
            return texts;
        }

        // A piece's place scaled out from the centre line, forward from the stern and up from the keel line; a keel's top
        // (its timber's depth over its place) scaled so, its timber still hanging under it.
        private static Vector3 Place(KwBpPiece piece, KwBpHull hull, float s)
        {
            Vector3 at = piece.Position;
            float drop = KwShipParts.Keel(piece, out _, out KwKeelBody body) ? body.Depth : 0f;
            float y = hull.Keel + (at.y + drop - hull.Keel) * s - drop;
            return new Vector3(at.x * s, y, hull.Aft + (at.z - hull.Aft) * s);
        }
    }
}
