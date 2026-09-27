using PlateColumn;
using UnityEngine;
using UnityEngine.Rendering;

namespace EarthWright.Gear
{
    /// <summary>
    /// The shovel's inventory icon, a PNG embedded in the DLL (<c>assets/gear_shovel.png</c>). Loaded once, on machines
    /// with a graphics device only: a dedicated server never shows icons, so the shovel keeps the hoe's icon there.
    /// When the PNG cannot be read the shovel keeps the hoe's icon too, and the log says why.
    /// </summary>
    public static class ShovelIcon
    {
        private const string Resource = "EarthWright.assets.gear_shovel.png";

        private static Sprite sprite;
        private static bool tried;

        /// <summary>The icon, or null to keep the hoe's.</summary>
        public static Sprite Get()
        {
            if (tried)
                return sprite;
            tried = true;
            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null)
                return null;
            sprite = EmbeddedSprite.Load(typeof(ShovelIcon).Assembly, Resource, "ew_shovel_icon");
            if (sprite == null)
                Plugin.Log.LogWarning("EarthWright: the shovel icon " + Resource + " could not be loaded; the shovel shows the hoe's icon.");
            return sprite;
        }
    }
}
