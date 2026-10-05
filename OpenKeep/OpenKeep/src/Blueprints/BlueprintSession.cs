using OpenKeep.Core;
using UnityEngine;

namespace OpenKeep.Blueprints
{
    /// <summary>
    /// A blueprint while it is selected in the hammer's Blueprints tab: where it would stand (the crosshair's ground,
    /// or the pinned spot), turned so its front faces the player plus the quarter turns of the turn keys, its floor at
    /// the ground plus the height keys (moved by the water rule where the blueprint has water). Draws the preview and
    /// keeps a checked plan for the HUD, re-checked when the spot changes and every second, less often when checking
    /// takes long. Materials are not a problem here: the click places a construction site that collects them, so the
    /// HUD lists what the site will need instead of what the player lacks.
    /// </summary>
    public static class BlueprintSession
    {
        private struct Anchor
        {
            public Vector3 Point;
            public float Yaw;
        }

        private const float RecheckInterval = 1f;

        private static Anchor? pin;

        /// <summary>The blueprint the pin belongs to, so putting the hammer away can still place it.</summary>
        private static string pinnedName;
        private static int turns;
        private static float nudge;
        private static float lift;
        private static SitePlan plan;
        private static float lastPlan = -100f;
        private static float planCost;
        private static Player lastPlayer;
        private static int terrainMask;

        public static bool Pinned => pin.HasValue;

        public static void Tick()
        {
            Player player = Player.m_localPlayer;
            TrackPlayer(player);
            if (!Selected(player, menuClosed: true))
            {
                if (pin.HasValue && HammerPutAway(player))
                    BlueprintSafe.Run("OpenKeep blueprint put away", () => PlaceOnPutAway(player));
                Hide(release: !Selected(player, menuClosed: false));
                return;
            }
            BlueprintKeys.Handle();
            Blueprint bp = SelectedBlueprint(player);
            if (bp == null || !TryFrame(bp, out BuildFrame frame, out bool waterMoved))
            {
                ShowNothing(bp);
                return;
            }
            GhostView.Show(bp, frame);
            Refresh(bp, frame, waterMoved, player);
            SiteOutline.Show(bp, frame, plan?.Problem != null);
            BlueprintHud.Show(bp, plan, Pinned, GhostView.OutlineOnly);
        }

        /// <summary>A blueprint entry is selected in the build tool in hand (with its menu closed when asked), and blueprints are on.</summary>
        private static bool Selected(Player player, bool menuClosed)
        {
            if (!BlueprintSettings.Enabled || player == null || player.IsDead() || !player.InPlaceMode())
                return false;
            return (!menuClosed || !Hud.IsPieceSelectionVisible()) && BlueprintMenu.Owns(player.GetSelectedPiece());
        }

        /// <summary>The blueprint of the selected menu entry, read (or from the cache); null when it cannot be read.</summary>
        private static Blueprint SelectedBlueprint(Player player)
        {
            string name = BlueprintMenu.NameOf(player.GetSelectedPiece());
            return name != null ? BlueprintLibrary.Load(name) : null;
        }

        /// <summary>
        /// Checks again when the spot or blueprint changed, at most every few tenths of a second (longer when a check takes
        /// long: a large site costs milliseconds), and every second anyway (materials, pieces in the way).
        /// </summary>
        private static void Refresh(Blueprint bp, BuildFrame frame, bool waterMoved, Player player)
        {
            bool moved = plan == null || plan.Blueprint != bp || !plan.Frame.SameAs(frame);
            float now = Time.realtimeSinceStartup;
            float gap = Mathf.Clamp(planCost * 10f, 0.2f, 2f);
            if (now < lastPlan + (moved ? gap : Mathf.Max(RecheckInterval, gap)))
                return;
            plan = BlueprintSafe.Call("OpenKeep blueprint plan", () => SitePlanner.Plan(bp, frame, waterMoved, player, needMaterials: false), null);
            lastPlan = Time.realtimeSinceStartup;
            planCost = lastPlan - now;
        }

        /// <summary>A plan made now at the pinned spot, for the click that places the site (never an older one).</summary>
        public static SitePlan PlanNow(Player player)
        {
            Blueprint bp = SelectedBlueprint(player);
            if (bp == null || !TryFrame(bp, out BuildFrame frame, out bool waterMoved))
                return null;
            plan = SitePlanner.Plan(bp, frame, waterMoved, player, needMaterials: false);
            lastPlan = Time.realtimeSinceStartup;
            return plan;
        }

        private static bool TryFrame(Blueprint bp, out BuildFrame frame, out bool waterMoved)
        {
            frame = default;
            waterMoved = false;
            Anchor? anchor = pin ?? Aim();
            if (!anchor.HasValue)
                return false;
            float wanted = anchor.Value.Point.y + lift;
            float ground = WaterRule.Fit(bp, wanted);
            waterMoved = Mathf.Abs(ground - wanted) > 0.01f;
            Vector3 p = anchor.Value.Point;
            frame = new BuildFrame(new Vector3(p.x, ground, p.z), anchor.Value.Yaw + turns * 90f + nudge);
            return true;
        }

        /// <summary>The ground under the crosshair on whole metres, and the camera's yaw snapped to a quarter turn.</summary>
        private static Anchor? Aim()
        {
            GameCamera camera = GameCamera.instance;
            if (terrainMask == 0)
                terrainMask = LayerMask.GetMask("terrain");
            if (camera == null || !Physics.Raycast(camera.transform.position, camera.transform.forward, out RaycastHit hit, BlueprintRules.AimRange, terrainMask))
                return null;
            Vector3 point = new Vector3(Mathf.Round(hit.point.x), 0f, Mathf.Round(hit.point.z));
            point.y = Heightmap.GetHeight(point, out float height) ? height : hit.point.y;
            float yaw = Mathf.Round(camera.transform.eulerAngles.y / 90f) * 90f;
            return new Anchor { Point = point, Yaw = yaw };
        }

        /// <summary>Pins the blueprint where the crosshair puts it now; false when the crosshair finds no ground.</summary>
        public static bool Pin()
        {
            Anchor? anchor = Aim();
            if (!anchor.HasValue)
            {
                Messages.Center(BlueprintWords.Aim);
                return false;
            }
            pin = anchor;
            pinnedName = BlueprintMenu.NameOf(Player.m_localPlayer?.GetSelectedPiece());
            Messages.Center(BlueprintWords.Pinned);
            return true;
        }

        /// <summary>The hammer left the hand (or build mode ended) while the player lives: not just another piece picked in its menu.</summary>
        private static bool HammerPutAway(Player player)
        {
            return player != null && !player.IsDead() && !(player.InPlaceMode() && HammerTable.Is(player.GetBuildTool()));
        }

        /// <summary>
        /// Putting the hammer away with a blueprint pinned places it as a construction site, as the second click does: its
        /// unbuilt pieces stay as a ghost until they are built or taken down. A spot that no longer passes the checks says why.
        /// </summary>
        private static void PlaceOnPutAway(Player player)
        {
            Blueprint bp = pinnedName != null ? BlueprintLibrary.Load(pinnedName) : null;
            if (bp == null || !TryFrame(bp, out BuildFrame frame, out bool waterMoved))
                return;
            SitePlan placed = SitePlanner.Plan(bp, frame, waterMoved, player, needMaterials: false);
            if (placed.Problem != null)
                Messages.Center(placed.Problem);
            else
                Sites.SitePlacement.Place(player, placed);
        }

        /// <summary>A blueprint or folder was renamed or moved: a pinned blueprint inside it is still placed when the hammer goes away.</summary>
        public static void FollowMove(string from, string to) => pinnedName = BlueprintLibrary.Moved(pinnedName, from, to);

        /// <summary>Lets the pinned blueprint follow the crosshair again; false when nothing was pinned.</summary>
        public static bool Release(bool quiet)
        {
            if (!pin.HasValue)
                return false;
            pin = null;
            if (!quiet)
                Messages.Center(BlueprintWords.Unpinned);
            return true;
        }

        public static void Turn(int quarters) => turns = ((turns + quarters) % 4 + 4) % 4;

        /// <summary>Alt + Left / Right: a small turn on top of the quarter turns.</summary>
        public static void Nudge(float degrees) => nudge = Mathf.Repeat(nudge + degrees + 180f, 360f) - 180f;

        public static void ResetTurn()
        {
            turns = 0;
            nudge = 0f;
        }

        public static void Lift(float metres) => lift += metres;

        public static void ResetLift() => lift = 0f;

        private static void ShowNothing(Blueprint bp)
        {
            GhostView.Hide();
            SiteOutline.Hide();
            BlueprintHud.ShowEmpty(bp == null ? BlueprintLibrary.Error : null, bp == null);
        }

        private static void Hide(bool release)
        {
            GhostView.Hide();
            SiteOutline.Hide();
            BlueprintHud.Clear(BlueprintHud.BlueprintBlock);
            if (release)
                pin = null;
        }

        /// <summary>A new local player (login, death, another world): nothing pinned, the preview made again.</summary>
        private static void TrackPlayer(Player player)
        {
            if (ReferenceEquals(player, lastPlayer))
                return;
            lastPlayer = player;
            pin = null;
            plan = null;
            GhostView.Clear();
        }
    }
}
