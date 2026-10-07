using System;
using System.Collections.Generic;
using System.Reflection;
using EliteCrafting.Core;
using UnityEngine;

namespace EliteCrafting.Tables.Window
{
    /// <summary>
    /// The essences' own icons (user request 2026-10-07: icons that show what each essence does, "like damage can be a bow
    /// and sword"): 64 px PNGs painted in the game's icon style in ValheimAssets <c>Assets/Icons/Essences</c>, embedded as
    /// <c>EliteCrafting.assets.icons.ecf_essence_&lt;id&gt;.png</c> and decoded once, point filtered like the game's icons.
    /// An essence without one shows its game item's icon (<see cref="Essence.IconItem"/>). Display only.
    /// </summary>
    internal static class EssenceIcons
    {
        private const string Prefix = "EliteCrafting.assets.icons.ecf_essence_";

        private static readonly Dictionary<string, Sprite?> Made = new Dictionary<string, Sprite?>();

        // The game's PNG decoder, ImageConversion.LoadImage(Texture2D, byte[], bool), by reflection: its module is built
        // against a newer netstandard than this net48 project may reference (PlateColumn's EmbeddedSprite does the same).
        private static readonly MethodInfo? LoadImage = Type.GetType("UnityEngine.ImageConversion, UnityEngine.ImageConversionModule")
            ?.GetMethod("LoadImage", new[] { typeof(Texture2D), typeof(byte[]), typeof(bool) });

        /// <summary>The pure essence item's own icon (<c>ecf_essence_item.png</c>), or null: it keeps its base's.</summary>
        public static Sprite? Item()
        {
            if (!Made.TryGetValue(ItemId, out Sprite? sprite))
            {
                sprite = Decode(ItemId);
                Made[ItemId] = sprite;
            }
            return sprite;
        }

        private const string ItemId = "item";

        public static Sprite? Of(Essence essence)
        {
            if (!Made.TryGetValue(essence.Id, out Sprite? sprite))
            {
                sprite = Decode(essence.Id);
                Made[essence.Id] = sprite;
            }
            return sprite != null ? sprite : GameIcon(essence);
        }

        private static Sprite? Decode(string id)
        {
            byte[]? png = Embedded.Bytes(Prefix + id + ".png");
            if (png == null)
            {
                return null;
            }
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false) { name = "ecf_essence_" + id };
            if (LoadImage == null || !(LoadImage.Invoke(null, new object[] { texture, png, true }) is bool loaded) || !loaded)
            {
                Log.Warn($"the {id} essence icon did not decode; it shows its game item's icon");
                return null;
            }
            texture.filterMode = FilterMode.Point;
            texture.wrapMode = TextureWrapMode.Clamp;
            Sprite sprite = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f));
            sprite.name = texture.name;
            return sprite;
        }

        private static Sprite? GameIcon(Essence essence)
        {
            GameObject? prefab = ObjectDB.instance != null ? ObjectDB.instance.GetItemPrefab(essence.IconItem) : null;
            return prefab != null ? prefab.GetComponent<ItemDrop>()?.m_itemData.GetIcon() : null;
        }
    }
}
