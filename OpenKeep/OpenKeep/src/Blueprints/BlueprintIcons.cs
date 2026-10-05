using System.Collections.Generic;
using PlateColumn;
using UnityEngine;

namespace OpenKeep.Blueprints
{
    /// <summary>
    /// The Blueprints tab's icons, one embedded 128 px PNG each (assets/*.png), loaded once: a blueprint, Fix ground,
    /// the Site planner, Copy building, the Construction ghosts switch (an eye, crossed out while hidden), and on the folder panel a folder row, the up row and the New folder button. A
    /// missing image falls back to the blueprint's.
    /// </summary>
    public static class BlueprintIcons
    {
        public const string Blueprint = "blueprint";
        public const string FixGround = "fixground";
        public const string Planner = "planner";
        public const string Copy = "copy";
        public const string GhostsShown = "ghostsshown";
        public const string GhostsHidden = "ghostshidden";
        public const string Folder = "folder";
        public const string FolderUp = "folderup";
        public const string FolderNew = "foldernew";

        private static readonly Dictionary<string, Sprite> loaded = new Dictionary<string, Sprite>();

        public static Sprite Get(string name)
        {
            if (loaded.TryGetValue(name, out Sprite known) && known != null)
                return known;
            Sprite sprite = EmbeddedSprite.Load(typeof(BlueprintIcons).Assembly, "OpenKeep.assets." + name + ".png", "OpenKeep_" + name);
            if (sprite == null && name != Blueprint)
                sprite = Get(Blueprint);
            loaded[name] = sprite;
            return sprite;
        }
    }
}
