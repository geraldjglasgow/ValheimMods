using System.Collections.Generic;
using ItemCopies;
using UnityEngine;
using UnityEngine.Rendering;

namespace EliteEquipment.Core
{
    /// <summary>
    /// A set of the game's own items shown with icons remade in the workshop's style while a feature is on, and with their
    /// own icons again when it is off (remembered at first sight). The sprites come from the feature's bundle: one per
    /// style variant (<c>ee_icon_&lt;prefab&gt;_&lt;variant&gt;</c>) for an item with variants, else one
    /// (<c>ee_icon_&lt;prefab&gt;</c>); an item whose sprites are missing keeps its own. Written into the prefabs and every
    /// live copy (ItemCopies). Display only: every client applies it from the synced switch; nothing on a dedicated server.
    /// </summary>
    public sealed class GameIcons
    {
        private const string Prefix = "ee_icon_";

        private readonly string[] items;
        private readonly Dictionary<string, Sprite[]> originals = new Dictionary<string, Sprite[]>();
        private readonly Dictionary<string, Sprite[]> remade = new Dictionary<string, Sprite[]>();

        public GameIcons(params string[] items)
        {
            this.items = items;
        }

        /// <summary>The remade icons from the bundle and each game item's own, once, when the feature's items are made.</summary>
        public void Prepare(List<GameObject> prefabs, AssetBundle bundle)
        {
            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null)
                return;
            foreach (string name in items)
            {
                Sprite[] own = prefabs.Find(p => p != null && p.name == name)?.GetComponent<ItemDrop>()?.m_itemData.m_shared.m_icons;
                Sprite[] icons = own != null ? Remade(bundle, name, own.Length) : null;
                if (icons == null)
                {
                    Plugin.Log.LogWarning($"EliteEquipment: no remade icons for the game's {name}; it keeps its own");
                    continue;
                }
                originals[name] = own;
                remade[name] = icons;
            }
        }

        public void Apply(bool on)
        {
            if (originals.Count > 0)
                Copies.Apply(originals.Keys, (name, shared) => shared.m_icons = on ? remade[name] : originals[name]);
        }

        /// <summary>One remade icon per variant the game's item has; null when the bundle lacks any of them.</summary>
        private static Sprite[] Remade(AssetBundle bundle, string name, int count)
        {
            var icons = new Sprite[Mathf.Max(1, count)];
            for (int i = 0; i < icons.Length; i++)
            {
                icons[i] = bundle.LoadAsset<Sprite>(count > 1 ? $"{Prefix}{name}_{i}" : Prefix + name);
                if (icons[i] == null)
                    return null;
            }
            return icons;
        }
    }
}
