using UnityEngine;

namespace Sample.HaloMenuDemo
{
    /// <summary>A flat-colored square sprite, generated at runtime so this sample needs no shipped art asset.
    /// A real mod would reference its own bundled icon instead.</summary>
    internal static class DemoIcon
    {
        public static Sprite Create(Color color)
        {
            Texture2D texture = new Texture2D(2, 2);
            texture.SetPixels(new[] { color, color, color, color });
            texture.Apply();
            return Sprite.Create(texture, new Rect(0, 0, 2, 2), new Vector2(0.5f, 0.5f));
        }
    }
}
