using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace OpenKeep.Blueprints
{
    /// <summary>
    /// Makes a blueprint of a build in the world: of the player-built pieces around the point the crosshair is on
    /// ('openkeep blueprint save': the player's own, or everyone's with "all"), or of exactly the pieces the Copy
    /// building tool selected (<see cref="Of"/>). The blueprint is turned so the side facing the player is its front,
    /// centred on the pieces, its levelled ground the most common ground height under them. The ground under and around
    /// the pieces is saved as relief (a 1 m grid of heights above that ground), so dug pools and canals come back; when
    /// any of it lies below sea level the blueprint keeps its water. Ships, carts and construction site posts are never
    /// part of a blueprint.
    /// </summary>
    public static class BlueprintCapture
    {
        private const float ReliefMargin = 2f;

        /// <summary>The blueprint of the pieces around the point, or null with the reason.</summary>
        public static Blueprint Capture(Player player, Vector3 at, float yaw, float radius, bool everyone, out string error)
        {
            List<Piece> pieces = Collect(player, at, radius, everyone);
            error = pieces.Count == 0 ? $"no built pieces of {(everyone ? "anyone" : "yours")} within {radius:0} m of the crosshair" : null;
            return error == null ? Of(pieces, yaw) : null;
        }

        /// <summary>
        /// The blueprint of exactly these pieces, its front facing <paramref name="yaw"/> (pieces that cannot be copied,
        /// or are gone, are left out); null when none is left.
        /// </summary>
        public static Blueprint Of(IEnumerable<Piece> pieces, float yaw)
        {
            List<Piece> kept = pieces.Where(Copyable).ToList();
            if (kept.Count == 0)
                return null;
            BuildFrame frame = FrameFor(kept, yaw);
            Blueprint bp = new Blueprint();
            foreach (Piece piece in InBuildOrder(kept))
                bp.Pieces.Add(Row(piece, frame));
            bp.Finish();
            bp.Levels.Clear();
            bp.Relief = ReliefOf(bp.PieceBounds, frame);
            SetWater(bp, frame.Ground);
            return bp;
        }

        /// <summary>A piece a blueprint can hold: built by a player and loaded, not a ship, a cart, a construction site's post or a menu entry.</summary>
        public static bool Copyable(Piece p)
        {
            return p != null && p.IsPlacedByPlayer() && p.m_nview != null && p.m_nview.IsValid() && p.GetComponent<Ship>() == null
                && p.GetComponent<Vagon>() == null && p.GetComponent<Sites.SiteMarker>() == null && !BlueprintMenu.IsOurs(p);
        }

        /// <summary>The camera's yaw to a quarter turn: the side of a build facing the player becomes its front.</summary>
        public static float FacingYaw()
        {
            GameCamera camera = GameCamera.instance;
            return camera != null ? Mathf.Repeat(Mathf.Round(camera.transform.eulerAngles.y / 90f) * 90f, 360f) : 0f;
        }

        private static List<Piece> Collect(Player player, Vector3 at, float radius, bool everyone)
        {
            long me = player.GetPlayerID();
            return Piece.s_allPieces.Where(p => Copyable(p) && (everyone || p.GetCreator() == me)
                && Flat(p.transform.position - at).sqrMagnitude <= radius * radius).ToList();
        }

        /// <summary>The frame: centred on the pieces, the most common ground height under them as its ground.</summary>
        private static BuildFrame FrameFor(List<Piece> pieces, float yaw)
        {
            Vector3 min = pieces[0].transform.position, max = min;
            foreach (Piece p in pieces)
            {
                min = Vector3.Min(min, p.transform.position);
                max = Vector3.Max(max, p.transform.position);
            }
            Vector3 centre = new Vector3(Mathf.Round((min.x + max.x) * 0.5f), 0f, Mathf.Round((min.z + max.z) * 0.5f));
            centre.y = CommonGround(min, max);
            return new BuildFrame(centre, yaw);
        }

        /// <summary>The ground height met most often on a 1 m grid over the area (to 10 cm): the levelled pad of a build.</summary>
        private static float CommonGround(Vector3 min, Vector3 max)
        {
            Dictionary<int, int> counts = new Dictionary<int, int>();
            for (float x = min.x; x <= max.x; x += 1f)
            {
                for (float z = min.z; z <= max.z; z += 1f)
                {
                    if (!Heightmap.GetHeight(new Vector3(x, 0f, z), out float h))
                        continue;
                    int key = Mathf.RoundToInt(h * 10f);
                    counts[key] = (counts.TryGetValue(key, out int n) ? n : 0) + 1;
                }
            }
            return counts.Count == 0 ? min.y : counts.OrderByDescending(c => c.Value).First().Key / 10f;
        }

        /// <summary>The order the pieces were built in when one player made them all (ZDO ids count up), else lowest first.</summary>
        private static IEnumerable<Piece> InBuildOrder(List<Piece> pieces)
        {
            ZDOID first = pieces[0].m_nview.GetZDO().m_uid;
            if (pieces.All(p => p.m_nview.GetZDO().m_uid.UserID == first.UserID))
                return pieces.OrderBy(p => p.m_nview.GetZDO().m_uid.ID);
            return pieces.OrderBy(p => p.transform.position.y);
        }

        private static BlueprintPiece Row(Piece piece, BuildFrame frame)
        {
            Vector3 p = piece.transform.position;
            Vector2 local = frame.Local(p.x, p.z);
            return new BlueprintPiece
            {
                Prefab = Utils.GetPrefabName(piece.gameObject), X = Round(local.x), Y = Round(p.y - frame.Ground), Z = Round(local.y),
                Yaw = Mathf.Round(((piece.transform.eulerAngles.y - frame.Yaw) % 360f + 360f) % 360f * 10f) / 10f,
            };
        }

        /// <summary>The ground heights above the frame's ground on a 1 m grid over the pieces plus a margin (it replaces the square pad <see cref="Blueprint.Finish"/> made).</summary>
        private static Relief ReliefOf(Rect pieces, BuildFrame frame)
        {
            float x0 = Mathf.Floor(pieces.xMin - ReliefMargin), z0 = Mathf.Floor(pieces.yMin - ReliefMargin);
            Relief r = new Relief
            {
                X0 = x0, Z0 = z0,
                Width = Mathf.CeilToInt(pieces.xMax + ReliefMargin) - (int)x0 + 1, Depth = Mathf.CeilToInt(pieces.yMax + ReliefMargin) - (int)z0 + 1,
            };
            r.Offsets = new float[r.Width * r.Depth];
            for (int i = 0; i < r.Offsets.Length; i++)
            {
                Vector3 w = frame.World(x0 + i % r.Width, 0f, z0 + i / r.Width);
                r.Offsets[i] = Heightmap.GetHeight(w, out float h) ? Round(h - frame.Ground) : 0f;
            }
            return r;
        }

        /// <summary>Ground saved below sea level means water: the floor and depth that keep it under water when placed again.</summary>
        private static void SetWater(Blueprint bp, float ground)
        {
            float floor = bp.Relief.Offsets.Min();
            float sea = WaterRule.Sea;
            if (ground + floor >= sea)
                return;
            bp.HasWater = true;
            bp.WaterFloor = floor;
            bp.WaterDepth = Round(sea - (ground + floor));
        }

        private static Vector3 Flat(Vector3 v) => new Vector3(v.x, 0f, v.z);

        private static float Round(float value) => Mathf.Round(value * 1000f) / 1000f;
    }
}
