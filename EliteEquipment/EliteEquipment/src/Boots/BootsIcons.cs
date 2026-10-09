using System.Collections.Generic;
using PlateColumn;
using UnityEngine;
using UnityEngine.Rendering;

namespace EliteEquipment.Boots
{
    /// <summary>
    /// The workshop's icons for the split pieces (ValheimAssets <c>Assets/Gear/SeparatedLegArmor/NativeSplit_v001/Icons/v001/64</c>:
    /// renders of the cut trousers and boots, the rag shoes' new paint included, not the game's sprites), embedded as
    /// <c>assets/boots/boots_&lt;set&gt;.png</c> and <c>pants_&lt;set&gt;.png</c> (set = the key in lower case). Each is
    /// decoded once, on first use; none on a dedicated server, which draws nothing. Null when missing (logged).
    /// </summary>
    public static class BootsIcons
    {
        private static readonly Dictionary<string, Sprite> loaded = new Dictionary<string, Sprite>();

        public static Sprite Boots(BootSet set) => Load("boots_" + set.Key.ToLowerInvariant());

        public static Sprite Pants(BootSet set) => Load("pants_" + set.Key.ToLowerInvariant());

        private static Sprite Load(string name)
        {
            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null)
                return null;
            if (loaded.TryGetValue(name, out Sprite sprite))
                return sprite;
            sprite = EmbeddedSprite.Load(typeof(BootsIcons).Assembly, "EliteEquipment.assets.boots." + name + ".png", "EE_" + name);
            if (sprite == null)
                Plugin.Log.LogWarning($"EliteEquipment: the icon {name}.png is missing; the item keeps a cut of the game's icon");
            loaded[name] = sprite;
            return sprite;
        }
    }
}
