using System.Globalization;
using EarthWright.Brush;
using EarthWright.Core;
using EarthWright.Terrain;
using UnityEngine;

namespace EarthWright.Preview
{
    /// <summary>
    /// The readout line under the crosshair: the terrain tile (the nearest vertex, whole metres), the ground height of
    /// that vertex and how far it is from its height before any edit, and for levelling entries how far the target lies from
    /// that ground (the Brush module's own line shows the target itself and where it comes from). The crosshair ray
    /// looks at the terrain only, so aiming at a floor reads the ground below it. The line is built again only when one
    /// of its numbers changed (in whole centimetres) or the language did; its words are localized token by token.
    /// </summary>
    internal static class CursorReadout
    {
        private const float MaxDistance = 100f;
        private const int NoTarget = int.MinValue;

        private static int terrainMask;
        private static Vector3Int shownTile;
        private static int shownGround, shownEdited, shownTarget, shownFor = -1;

        /// <summary>The localized line, or null when there is nothing to show.</summary>
        public static string Text { get; private set; }

        public static void Update()
        {
            if (!HudSettings.ShowReadout.Value || !HudSettings.ShowHud.Value || !LocalTool.InTerrainTool || GameCamera.instance == null)
            {
                Text = null;
                return;
            }
            if (terrainMask == 0)
                terrainMask = LayerMask.GetMask("terrain");
            Transform camera = GameCamera.instance.transform;
            if (Physics.Raycast(camera.position, camera.forward, out RaycastHit hit, MaxDistance, terrainMask))
                Read(hit.point);
            else
                Text = null;
        }

        private static void Read(Vector3 point)
        {
            Vector3Int tile = TerrainRead.VertexOf(point);
            bool known = TerrainRead.TryVertex(point, out VertexInfo vertex);
            float ground = known ? vertex.Current : point.y;
            int edited = known ? Centimetres(vertex.Current - vertex.Base) : 0;
            int target = BrushState.Active && TargetHeight.Used(BrushState.Action) ? Centimetres(BrushState.TargetHeight - ground) : NoTarget;
            int groundCm = Centimetres(ground);
            if (Text != null && tile == shownTile && groundCm == shownGround && edited == shownEdited && target == shownTarget
                && shownFor == Language.Version)
                return;
            shownTile = tile;
            shownGround = groundCm;
            shownEdited = edited;
            shownTarget = target;
            shownFor = Language.Version;
            Text = TokenText.Localize(Describe(tile, groundCm, edited, target));
        }

        private static string Describe(Vector3Int tile, int ground, int edited, int target)
        {
            string text = string.Format(CultureInfo.InvariantCulture, "$ew_preview_tile {0}, {1} · $ew_preview_ground {2:0.00} m", tile.x, tile.z, ground / 100f);
            if (edited != 0)
                text += string.Format(CultureInfo.InvariantCulture, " ($ew_preview_edited {0:+0.00;-0.00} m)", edited / 100f);
            if (target != NoTarget)
                text += string.Format(CultureInfo.InvariantCulture, " · $ew_preview_totarget {0:+0.00;-0.00;0.00} m", target / 100f);
            return text;
        }

        private static int Centimetres(float metres) => Mathf.RoundToInt(metres * 100f);
    }
}
