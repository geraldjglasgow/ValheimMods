using UnityEngine;

namespace Wayfare.Targeting
{
    /// <summary>Two small procedural sprites - a filled circle for a plain portal, the same circle with an outer
    /// ring for a favourite - built once at runtime so nothing needs to ship as a binary asset. Built lazily and
    /// cached; <see cref="Portal"/>/<see cref="Favourite"/> are cheap to call every frame once built.</summary>
    public static class IconFactory
    {
        private const int Size = 32;
        private static readonly Color PortalColor = new Color(0.35f, 0.8f, 1f, 0.95f);
        private static readonly Color FavouriteRingColor = new Color(1f, 0.85f, 0.25f, 0.95f);

        private static Sprite portal;
        private static Sprite favourite;

        public static Sprite Portal => portal != null ? portal : (portal = BuildCircle(PortalColor, ringOnly: false));

        public static Sprite Favourite => favourite != null ? favourite : (favourite = BuildRing(FavouriteRingColor));

        private static Sprite BuildCircle(Color color, bool ringOnly)
        {
            Texture2D texture = NewTexture();
            float center = (Size - 1) / 2f;
            float outerRadius = Size / 2f - 1f;
            for (int y = 0; y < Size; y++)
            {
                for (int x = 0; x < Size; x++)
                {
                    float distance = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                    texture.SetPixel(x, y, distance <= outerRadius ? color : Color.clear);
                }
            }
            texture.Apply();
            return ToSprite(texture);
        }

        private static Sprite BuildRing(Color ringColor)
        {
            Texture2D texture = NewTexture();
            float center = (Size - 1) / 2f;
            float outerRadius = Size / 2f - 1f;
            float innerRadius = outerRadius - 4f;
            for (int y = 0; y < Size; y++)
            {
                for (int x = 0; x < Size; x++)
                {
                    float distance = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                    bool inRing = distance <= outerRadius && distance >= innerRadius;
                    texture.SetPixel(x, y, inRing ? ringColor : Color.clear);
                }
            }
            texture.Apply();
            return ToSprite(texture);
        }

        private static Texture2D NewTexture()
        {
            return new Texture2D(Size, Size, TextureFormat.RGBA32, mipChain: false) { filterMode = FilterMode.Bilinear };
        }

        private static Sprite ToSprite(Texture2D texture)
        {
            return Sprite.Create(texture, new Rect(0, 0, Size, Size), new Vector2(0.5f, 0.5f), Size);
        }
    }
}
