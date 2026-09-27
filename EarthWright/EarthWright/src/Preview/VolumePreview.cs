using System.Collections.Generic;
using EarthWright.Terrain;
using UnityEngine;

namespace EarthWright.Preview
{
    /// <summary>
    /// The 3D volume preview: a see-through cylinder or box (the brush's own shape) between the current ground and the
    /// height the click aims for. Coloured by reach: the volume colour within reach, the beyond-reach colour past it,
    /// the blocked colour while a module reports the click would be refused; all at the configured opacity.
    /// </summary>
    internal static class VolumePreview
    {
        private static readonly MeshLayer layer = new MeshLayer("EarthWright Volume");
        private static readonly MeshData data = new MeshData();
        private static int loopsVersion = -1;
        private static Color lastColour;
        private static Vector3 lastAim;
        private static bool shown;

        public static void Update()
        {
            BrushStroke stroke = PreviewFrame.Stroke;
            if (!PreviewFrame.BrushVisible || stroke == null || !PreviewSettings.ShowVolume.Value || !VolumeMesh.HasSurface(stroke.Height))
            {
                Hide();
                return;
            }
            GroundLoops.Brush.Get(PreviewFrame.HeightSpec, out List<Vector3[]> ground);
            bool rebuilt = GroundLoops.Brush.Version != loopsVersion;
            loopsVersion = GroundLoops.Brush.Version;
            Color colour = Colour();
            Vector3 aim = new Vector3((float)stroke.Height, stroke.Target, stroke.Amount);
            if (shown && !rebuilt && colour == lastColour && aim == lastAim)
                return;
            lastColour = colour;
            lastAim = aim;
            shown = VolumeMesh.Build(data, PreviewFrame.Params, ground, colour);
            if (shown)
                layer.Show(data);
            else
                layer.Hide();
        }

        private static Color Colour()
        {
            Color colour;
            switch (PreviewFrame.Tone)
            {
                case PreviewTone.OutOfReach: colour = PreviewSettings.VolumeBeyondReachColour.Value; break;
                case PreviewTone.Blocked: colour = PreviewSettings.BlockedColour.Value; break;
                default: colour = PreviewSettings.VolumeColour.Value; break;
            }
            colour.a *= PreviewSettings.VolumeOpacity.Value;
            return colour;
        }

        public static void Hide()
        {
            layer.Hide();
            shown = false;
        }
    }
}
