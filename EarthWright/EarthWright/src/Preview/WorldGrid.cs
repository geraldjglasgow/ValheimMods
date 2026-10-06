using EarthWright.Brush;
using EarthWright.Core;
using UnityEngine;

namespace EarthWright.Preview
{
    /// <summary>
    /// The world grid on the ground around the crosshair, toggled with its key (F8) and drawn while building with any
    /// tool (menu closed). Anchored to the world's axes, or to the last building piece aimed at (through its position,
    /// turned with it) when the anchor setting says so. The mesh is rebuilt when the aim moves, the anchor or a grid
    /// setting changes or half a second has passed (so edited ground shows), at most ten times a second; the aim ray is
    /// cast only when a rebuild may happen.
    /// </summary>
    internal static class WorldGrid
    {
        private const float RebuildSeconds = 0.5f;
        private const float MinInterval = 0.1f;
        private const float RayDistance = 50f;

        private static readonly MeshLayer layer = new MeshLayer("EarthWright World Grid");
        private static readonly MeshData data = new MeshData();
        private static Transform anchorPiece;
        private static GridFrame built;
        private static float builtAt = -10f;
        private static int builtSettings = -1;
        private static int settingsVersion;
        private static bool watching;

        public static bool On { get; private set; }

        public static void Update()
        {
            if (Keys.Pressed(HudSettings.GridKey) && Player.m_localPlayer != null)
                Toggle();
            if (!On || !CanShow())
            {
                Hide();
                return;
            }
            float age = Time.time - builtAt;
            if (age < MinInterval)
                return;
            if (!TryFrame(out GridFrame frame))
                Hide();
            else
                Rebuild(frame, age);
        }

        private static void Rebuild(GridFrame frame, float age)
        {
            WatchSettings();
            if (!Moved(frame) && settingsVersion == builtSettings && age < RebuildSeconds)
                return;
            built = frame;
            builtAt = Time.time;
            builtSettings = settingsVersion;
            GridMesh.Build(data, frame);
            layer.Show(data);
        }

        private static void Toggle()
        {
            On = !On;
            Messages.TopLeft(On ? "$ew_preview_grid_on" : "$ew_preview_grid_off");
        }

        private static bool CanShow()
        {
            Player player = Player.m_localPlayer;
            return player != null && !player.IsDead() && player.InPlaceMode() && !Hud.IsPieceSelectionVisible();
        }

        /// <summary>The grid's frame: centred on the brush, the ghost or the aimed point; anchored per the setting.</summary>
        private static bool TryFrame(out GridFrame frame)
        {
            frame = default;
            bool hit = AimRay(out Vector3 point);
            GameObject ghost = LocalTool.Ghost;
            if (BrushState.Active && BrushState.HasAim)
                point = BrushState.Center;
            else if (ghost != null)
                point = ghost.transform.position;
            else if (!hit)
                return false;
            frame.Aim = point;
            bool pieceAnchor = HudSettings.GridAnchorMode.Value == GridAnchor.AimedPiece && anchorPiece != null;
            frame.Origin = pieceAnchor ? anchorPiece.position : Vector3.zero;
            frame.Yaw = pieceAnchor ? anchorPiece.eulerAngles.y : 0f;
            return true;
        }

        /// <summary>Casts the crosshair ray; remembers a building piece it hits as the anchor.</summary>
        private static bool AimRay(out Vector3 point)
        {
            point = Vector3.zero;
            Player player = Player.m_localPlayer;
            if (GameCamera.instance == null || player == null)
                return false;
            Transform camera = GameCamera.instance.transform;
            if (!Physics.Raycast(camera.position, camera.forward, out RaycastHit hit, RayDistance, player.m_placeRayMask))
                return false;
            point = hit.point;
            WearNTear piece = hit.collider.GetComponentInParent<WearNTear>();
            if (piece != null && !Player.IsPlacementGhost(piece.gameObject))
                anchorPiece = piece.transform;
            return true;
        }

        private static bool Moved(GridFrame frame)
        {
            return (frame.Aim - built.Aim).sqrMagnitude > 0.0625f || (frame.Origin - built.Origin).sqrMagnitude > 0.0001f
                || Mathf.Abs(frame.Yaw - built.Yaw) > 0.01f;
        }

        private static void Hide()
        {
            layer.Hide();
            builtAt = -10f;
        }

        /// <summary>Counts the changes of the grid's settings (watched from the first use on), so a change rebuilds the mesh.</summary>
        private static void WatchSettings()
        {
            if (watching)
                return;
            watching = true;
            System.EventHandler changed = (_, __) => settingsVersion++;
            HudSettings.GridRadius.SettingChanged += changed;
            HudSettings.GridSpacing.SettingChanged += changed;
            HudSettings.GridLineWidth.SettingChanged += changed;
            HudSettings.GridColour.SettingChanged += changed;
            HudSettings.GridMajorColour.SettingChanged += changed;
            HudSettings.GridMajorEvery.SettingChanged += changed;
        }
    }
}
