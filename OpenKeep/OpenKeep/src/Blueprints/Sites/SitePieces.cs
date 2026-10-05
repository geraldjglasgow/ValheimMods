using System.Collections.Generic;

namespace OpenKeep.Blueprints.Sites
{
    /// <summary>
    /// Places construction site pieces on the machine that owns the site (its player as their creator, through
    /// <see cref="PiecePlacer"/>), each piece's materials taken from the site's store unless the build is free, and
    /// marks them built, the store and the built pieces written once per call. A piece whose prefab this game lacks is
    /// marked built without being placed, so the site cannot wait for it for ever.
    /// </summary>
    public static class SitePieces
    {
        /// <summary>
        /// Places the pieces in the given order until one's materials are short; true when all were placed. The ZDO is
        /// written once at the end (the owner's machine only).
        /// </summary>
        public static bool Place(SiteMarker site, IList<int> pieces, Player player, bool free, bool cheated)
        {
            SiteState s = site.State;
            Dictionary<string, int> store = free ? null : s.Store;
            List<int> done = new List<int>();
            foreach (int i in pieces)
            {
                if (i < 0 || i >= s.Blueprint.Pieces.Count || (!free && !SiteCosts.TryTake(store, s.Blueprint.Pieces[i].Prefab)))
                    break;
                PlaceOne(s, i, player, cheated);
                done.Add(i);
            }
            if (!free && done.Count > 0)
                s.SetStore(store);
            s.SetBuilt(done);
            return done.Count == pieces.Count;
        }

        private static void PlaceOne(SiteState s, int index, Player player, bool cheated)
        {
            BlueprintPiece p = s.Blueprint.Pieces[index];
            Piece prefab = MaterialBill.PieceOf(p.Prefab);
            if (prefab == null)
            {
                Plugin.Log.LogWarning($"OpenKeep: site {s.Name}: this game has no piece {p.Prefab}, skipped");
                return;
            }
            BuildFrame frame = s.Frame;
            BlueprintSafe.Call("OpenKeep site piece",
                () => PiecePlacer.Place(player, prefab, frame.World(p.X, p.Y, p.Z), frame.PieceRotation(p.Yaw), cheated), ZDOID.None);
        }
    }
}
