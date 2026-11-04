using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// A dock section's solid parts, made on every peer (a dedicated server too: the game's support check runs on the
    /// piece's owner and needs the pilings): one box for the deck from its top down to the cap beams' underside (walked
    /// on, built on), a low box for each run of curb (none where another section meets the side, <see cref="KwPierGaps"/>),
    /// and a box for each piling down to its foot, so ships and boats bump them and the game's support check finds them
    /// standing in the ground (WearNTear: a collider within 0.15 m of the terrain or a rock holds the piece up at its
    /// material's full support). And snap points for the game's pieces: the deck's four corners, its centre line's two ends
    /// and the edges at every station, at the deck's top. All under the piece, on its layer, made once; the curbs made
    /// again when the sections meeting it change.
    /// </summary>
    public static class KwPierBody
    {
        private const string CurbName = "bs_pier_curb";

        public static void Make(Transform piece, KwPierSpec spec, KwPierKind kind, KwPierGaps gaps)
        {
            float cap = -kind.CapFoot;
            Box(piece, "deck", new Vector3(0f, -cap * 0.5f, spec.Length * 0.5f), new Vector3(spec.Width, cap, spec.Length));
            Curbs(piece, spec, kind, gaps);
            for (int i = 0; i < spec.Stations; i++)
                Pilings(piece, spec, kind, i);
            Snaps(piece, spec);
        }

        /// <summary>The curbs' solid parts made (again): a box for each run of a side no other section meets.</summary>
        public static void Curbs(Transform piece, KwPierSpec spec, KwPierKind kind, KwPierGaps gaps)
        {
            Drop(piece, CurbName);
            float x = spec.Width * 0.5f - kind.Curb.x * 0.5f, y = kind.Curb.y * 0.5f;
            foreach ((int edge, float side) in new[] { (KwPierGaps.Left, -x), (KwPierGaps.Right, x) })
            {
                foreach (Vector2 run in gaps.Runs(edge, spec.Length))
                    Box(piece, "curb", new Vector3(side, y, (run.x + run.y) * 0.5f), new Vector3(kind.Curb.x, kind.Curb.y, run.y - run.x));
            }
        }

        // The children of that name taken out of the physics and off the piece at once (the game's support check, asked
        // again this frame, never finds them), then destroyed.
        private static void Drop(Transform piece, string name)
        {
            for (int i = piece.childCount - 1; i >= 0; i--)
            {
                GameObject child = piece.GetChild(i).gameObject;
                if (child.name != name)
                    continue;
                child.SetActive(false);
                child.transform.SetParent(null, false);
                Object.Destroy(child);
            }
        }

        private static void Pilings(Transform piece, KwPierSpec spec, KwPierKind kind, int station)
        {
            float x = KwPierShape.PostX(spec, kind), z = KwPierShape.PostZ(spec, kind, station);
            Vector2 depth = spec.Depths[station];
            foreach ((float side, float foot) in new[] { (-x, depth.x), (x, depth.y) })
            {
                float length = kind.CapFoot + foot;
                if (length >= KwPierShape.MinPiling)
                    Box(piece, "piling", new Vector3(side, kind.CapFoot - length * 0.5f, z), new Vector3(kind.Post, length, kind.Post));
            }
        }

        private static void Box(Transform piece, string name, Vector3 centre, Vector3 size)
        {
            GameObject part = new GameObject("bs_pier_" + name) { layer = piece.gameObject.layer };
            part.transform.SetParent(piece, false);
            BoxCollider box = part.AddComponent<BoxCollider>();
            (box.center, box.size) = (centre, size);
        }

        // Inactive children tagged as the game's snap points are, named as its corners are on the build hud.
        private static void Snaps(Transform piece, KwPierSpec spec)
        {
            float half = spec.Width * 0.5f;
            foreach (float z in new[] { 0f, spec.Length })
            {
                foreach (float x in new[] { -half, 0f, half })
                    Snap(piece, new Vector3(x, 0f, z));
            }
            for (int i = 1; i + 1 < spec.Stations; i++)
            {
                Snap(piece, new Vector3(-half, 0f, spec.StationZ(i)));
                Snap(piece, new Vector3(half, 0f, spec.StationZ(i)));
            }
        }

        private static void Snap(Transform piece, Vector3 at)
        {
            GameObject point = new GameObject("$hud_snappoint_corner") { tag = "snappoint", layer = piece.gameObject.layer };
            point.transform.SetParent(piece, false);
            point.transform.localPosition = at;
            point.SetActive(false);
        }

        /// <summary>
        /// The game's support check reads a piece's colliders once and keeps them; asked again now the section has its
        /// own (so a check made before it was built never leaves the pilings out).
        /// </summary>
        public static void Resupport(WearNTear? wear)
        {
            if (wear != null)
                wear.m_colliders = null;
        }
    }
}
