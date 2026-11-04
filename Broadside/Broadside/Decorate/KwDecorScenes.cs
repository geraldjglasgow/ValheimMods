using System.Collections.Generic;

namespace Broadside
{
    /// <summary>
    /// The scenes the tool composes a shipyard from (DECORATE.md, "Scenes", has each one's rules), in the order they are
    /// placed: the pickiest and largest first, so they get the places they need; the density each first shows at is its
    /// own (<see cref="KwDecorScene.Level"/>). Each scene's frame: its front +Z (the side people work from), x to its right,
    /// metres from its middle; the sizes are the pieces' own footprints (<see cref="KwDecorFit"/>), so the gaps written here
    /// are the gaps a player sees. The quay's furniture along docks is laid out by <see cref="KwDecorQuay"/>. Plain data.
    /// </summary>
    public static class KwDecorScenes
    {
        private const KwDecorPlace Shore = KwDecorPlace.Shore, Wall = KwDecorPlace.Wall;

        public static readonly KwDecorScene[] All =
        {
            // The shed's open front faces the yard; sawhorses on their shavings 1.5 m in front of its eave, the drying rack
            // 0.8 m off its right side, nail barrels at its front left corner, the tool wall on a building near.
            // Slots for pieces not in the game yet: the pit saw off its left side, the figurehead bench under its roof.
            new KwDecorScene("carpentry", "a carpentry yard", 1, KwFacing.Open)
            {
                Needs = KwNeed.Land, Likes = KwLike.Yard | KwLike.BackToWall | KwLike.Dry | KwLike.Central, Copies = 2, Later = 4, Apart = 20f,
                Members =
                {
                    M("leanto", 0f, -1.6f),
                    M("shavings", -0.5f, 2f),
                    M("sawhorses", -0.5f, 2f),
                    M("dryingrack", 3.55f, -1.6f, 90f, 1),
                    M("nailbarrels", -3f, -0.3f, 0f, 1),
                    M("toolwall", 0f, 0f, 0f, 1, Wall),
                    M("pitsaw", -5.3f, -1.6f, 90f, 1),
                    M("figurebench", 0f, -2.55f, 0f, 1),
                },
                Aprons = { (-2.4f, 3f, 2.4f, 5.5f) },
            },
            // At the water's edge, facing it: the net drying rack, lobster pots in front, the stone anchor and a mooring
            // post at the water, a second stack of pots at the side, the hanging net on a building near.
            new KwDecorScene("fishing", "a fishing corner", 1, KwFacing.Water)
            {
                Needs = KwNeed.WaterFront, Likes = KwLike.Quay, ApronWet = true, Copies = 2, Later = 3, Apart = 16f,
                Members =
                {
                    M("netrack", 0f, -1f),
                    M("creels", -1.1f, 0.6f),
                    M("anchor", 1.1f, 0.7f, 0f, 1, Shore),
                    M("mooringpost", 2.5f, 1.9f, 0f, 1, Shore),
                    M("hangnet", 0f, 0f, 0f, 1, Wall),
                    M("creels", -2.7f, -0.7f, 90f, 2),
                },
                Aprons = { (-1.8f, 1.2f, 1.8f, 3f) },
            },
            // Beside the water or the dry dock: the hull frames along it, the scaffold on the land side 0.45 m off the
            // hull with its toe board to it, sawhorses on shavings behind the scaffold, nail barrels at its far end.
            new KwDecorScene("slip", "a ship slip", 3, KwFacing.Slip)
            {
                Needs = KwNeed.Slip | KwNeed.Land, Likes = KwLike.Yard, ApronWet = true, Margin = 1.5f,
                Members =
                {
                    M("hullribs", 0f, 0.4f, 90f),
                    M("scaffold", 0.4f, -2.45f),
                    M("shavings", -3.7f, -4.3f, 0f, 1),
                    M("sawhorses", -3.7f, -4.3f, 0f, 1),
                    M("nailbarrels", 3.3f, -4f, 0f, 2),
                },
                Aprons = { (-5f, 2.2f, 5f, 3.4f) },
            },
            // Facing the dry dock (the yard's heart): the steam box with its kettle's end kept clear, the spar rack behind it,
            // the tar pot 2.35 m off its open end.
            new KwDecorScene("steam", "steam bending", 2, KwFacing.Heart)
            {
                Needs = KwNeed.Land, Likes = KwLike.Yard | KwLike.Dry, Beside = "carpentry",
                Members =
                {
                    M("steambox", 0f, 0f, 0f, 0, KwDecorPlace.Ground, true),
                    M("sparrack", 0.2f, -2f),
                    M("tarpot", 4f, 1f),
                },
                Aprons = { (-2f, 0.8f, 2.6f, 3f), (-3.6f, -0.8f, -1.7f, 0.8f) },
            },
            // A long strip: the rope walk, the rope spool at its jack's end facing along it, coils at its sledge's end,
            // spare coils behind the spool; people walk along its front.
            new KwDecorScene("rope", "rope works", 3, KwFacing.Path)
            {
                Needs = KwNeed.Land, Likes = KwLike.Path | KwLike.Dry,
                Members =
                {
                    M("ropewalk", 0f, 0f, 0f, 0, KwDecorPlace.Ground, true),
                    M("ropespool", -3.4f, 0f, 90f),
                    M("ropecoils", 3.2f, 0.1f),
                    M("ropecoils", -3.3f, -1.6f, 0f, 1),
                },
                Aprons = { (-4f, 0.7f, 4f, 2f) },
            },
            // Near the docks, facing the path: oar rack, shield rack and spar rack in a row, backs in line, the ship's
            // anchor lying at the oar rack's end, nail barrels at the spar rack's.
            new KwDecorScene("gear", "a ship gear store", 2, KwFacing.Path)
            {
                Needs = KwNeed.NearWater, Likes = KwLike.Quay | KwLike.Path,
                Members =
                {
                    M("oarrack", -3.4f, 0f),
                    M("shieldrack", -1f, -0.05f),
                    M("sparrack", 2.6f, 0.15f),
                    M("ironanchor", -5.6f, 0.35f, 90f, 1),
                    M("nailbarrels", 5.6f, 0.15f, 0f, 2),
                },
                Aprons = { (-4.4f, 0.8f, 4.8f, 2.4f) },
            },
            // The sail frame facing its working side, sail bolts beside it, the pennant line strung behind.
            new KwDecorScene("sail", "a sail loft", 4, KwFacing.Open)
            {
                Likes = KwLike.BackToWall, Beside = "rope",
                Members =
                {
                    M("sailframe", 0f, 0f),
                    M("sailbolts", 2.45f, 0.2f),
                    M("pennants", 0.8f, -1.5f, 0f, 1),
                },
                Aprons = { (-1.5f, 0.6f, 3.3f, 2.6f) },
            },
            // At a way into the yard, facing the path.
            new KwDecorScene("notice", "a notice board", 2, KwFacing.Path)
            {
                Needs = KwNeed.PathFront, Likes = KwLike.Entrance, Margin = 1f,
                Members = { M("noticeboard", 0f, 0f) },
                Aprons = { (-1f, 0.3f, 1f, 1.6f) },
            },
            // On high open ground, 2 m clear round it.
            new KwDecorScene("vane", "a weather vane", 3, KwFacing.Heart)
            {
                Needs = KwNeed.Land, Likes = KwLike.High, Margin = 2f,
                Members = { M("vane", 0f, 0f) },
            },
            // At the yard's edge, out of the way.
            new KwDecorScene("salvage", "a salvage heap", 4, KwFacing.Heart)
            {
                Needs = KwNeed.Land, Likes = KwLike.Outskirts | KwLike.Wet, Margin = 1.5f,
                Members = { M("salvage", 0f, 0f) },
            },
            // On the ground by where a dock is stepped onto, facing the water.
            new KwDecorScene("quayanchor", "a ship's anchor by the quay", 4, KwFacing.Water)
            {
                Needs = KwNeed.Quay | KwNeed.Land, Likes = KwLike.Quay, Margin = 1f,
                Members = { M("ironanchor", 0f, 0f) },
            },
        };

        private static KwSceneMember M(string piece, float x, float z, float yaw = 0f, int extra = 0, KwDecorPlace place = KwDecorPlace.Ground, bool ends = false) =>
            new KwSceneMember(piece, x, z, yaw, extra, place, ends);

        /// <summary>A scene by id; null for none.</summary>
        public static KwDecorScene? Find(string id) => System.Array.Find(All, s => s.Id == id);

        /// <summary>The scene's copies in the order they are placed: every first one in the list's order, then the second copies.</summary>
        public static List<(KwDecorScene Scene, int Copy)> Order()
        {
            List<(KwDecorScene, int)> order = new List<(KwDecorScene, int)>();
            foreach (KwDecorScene scene in All)
                order.Add((scene, 0));
            foreach (KwDecorScene scene in All)
            {
                for (int copy = 1; copy < scene.Copies; copy++)
                    order.Add((scene, copy));
            }
            return order;
        }
    }
}
