using UnityEngine;
using UnityEngine.Rendering;

namespace EliteCreaturesPack.Custom.Look
{
    /// <summary>
    /// Whether this machine draws anything. A dedicated server has no graphics device: nobody there sees a tint, a texture
    /// or an overlay, so the look step makes no materials, loads no image and wears no effect there. What every peer must
    /// agree on (a creature's size, the parts it registers, whether it fails) never depends on this.
    /// </summary>
    internal static class Drawing
    {
        public static readonly bool Headless = SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null;
    }
}
