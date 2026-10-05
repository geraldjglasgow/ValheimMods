using UnityEngine;

namespace Wayfare.Targeting
{
    /// <summary>The portal icons' look: the game's own portal map icon (the pin bar's portal, <c>PinType.Icon4</c>) tinted
    /// gold, a filled circle only while the map has none, and a ring drawn around a favourite. The circle and the ring are
    /// built once at runtime so nothing needs to ship as a binary asset; all are cached and cheap to call every frame.</summary>
    public static class IconFactory
    {
        private const int Size = 32;
        public static readonly Color Gold = new Color(1f, 0.8f, 0.25f, 1f);
        public static readonly Color HereRed = new Color(1f, 0.25f, 0.2f, 1f);
        private static readonly Color FavouriteRingColor = new Color(1f, 0.92f, 0.6f, 0.95f);

        private static Sprite portal;
        private static Sprite circle;
        private static Sprite favourite;

        public static Sprite Portal
        {
            get
            {
                if (portal == null)
                    portal = GamePortal();
                return portal != null ? portal : Circle;
            }
        }

        public static Sprite Favourite => favourite != null ? favourite : (favourite = BuildRing(FavouriteRingColor));

        private static Sprite Circle => circle != null ? circle : (circle = BuildCircle(Color.white));

        private static Sprite GamePortal()
        {
            if (Minimap.instance == null)
                return null;
            foreach (Minimap.SpriteData data in Minimap.instance.m_icons)
            {
                if (data.m_name == Minimap.PinType.Icon4)
                    return data.m_icon;
            }
            return null;
        }

        private static Sprite BuildCircle(Color color)
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
            float innerRadius = outerRadius - 3f;
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
