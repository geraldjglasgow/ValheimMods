using System.Collections.Generic;
using PlateColumn;
using UnityEngine;
using UnityEngine.Rendering;

namespace GrindstoneSkills
{
    /// <summary>
    /// A skill icon embedded in the DLL, <c>assets/&lt;name&gt;.png</c> (64x64, like the game's skill icons), decoded once
    /// per name. Null on a machine without graphics (a dedicated server never shows icons) and when the file is missing
    /// or will not decode; the log says which.
    /// </summary>
    public static class EmbeddedIcon
    {
        private static readonly Dictionary<string, Sprite> loaded = new Dictionary<string, Sprite>();

        public static Sprite Load(string name)
        {
            if (loaded.TryGetValue(name, out Sprite sprite))
                return sprite;
            if (SystemInfo.graphicsDeviceType != GraphicsDeviceType.Null)
                sprite = EmbeddedSprite.Load(typeof(EmbeddedIcon).Assembly, "GrindstoneSkills.assets." + name + ".png", name);
            if (sprite == null && SystemInfo.graphicsDeviceType != GraphicsDeviceType.Null)
                GrindstoneSkills.Log.LogWarning("The icon assets/" + name + ".png could not be loaded; the game's icon stays.");
            loaded[name] = sprite;
            return sprite;
        }
    }
}
