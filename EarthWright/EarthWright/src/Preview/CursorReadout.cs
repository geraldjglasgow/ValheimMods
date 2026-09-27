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
    /// looks at the terrain only, so aiming at a floor reads the ground below it.
    /// </summary>
    internal static class CursorReadout
    {
        private const float MaxDistance = 100f;

        private static int terrainMask;

        /// <summary>The localized line, or null when there is nothing to show.</summary>
        public static string Text { get; private set; }

        public static void Update()
        {
            Text = null;
            if (!HudSettings.ShowReadout.Value || !HudSettings.ShowHud.Value || !LocalTool.InTerrainTool || GameCamera.instance == null)
                return;
            if (terrainMask == 0)
                terrainMask = LayerMask.GetMask("terrain");
            Transform camera = GameCamera.instance.transform;
            if (Physics.Raycast(camera.position, camera.forward, out RaycastHit hit, MaxDistance, terrainMask))
                Text = Language.Localize(Describe(hit.point));
        }

        private static string Describe(Vector3 point)
        {
            Vector3Int tile = TerrainRead.VertexOf(point);
            bool known = TerrainRead.TryVertex(point, out VertexInfo vertex);
            float ground = known ? vertex.Current : point.y;
            float edited = known ? vertex.Current - vertex.Base : 0f;
            string text = string.Format(CultureInfo.InvariantCulture, "$ew_preview_tile {0}, {1} · $ew_preview_ground {2:0.00} m", tile.x, tile.z, ground);
            if (Mathf.Abs(edited) >= 0.01f)
                text += string.Format(CultureInfo.InvariantCulture, " ($ew_preview_edited {0:+0.00;-0.00} m)", edited);
            if (BrushState.Active && TargetHeight.Used(BrushState.Action))
                text += string.Format(CultureInfo.InvariantCulture, " · $ew_preview_totarget {0:+0.00;-0.00;0.00} m", BrushState.TargetHeight - ground);
            return text;
        }
    }
}
