using System;
using UnityEngine;

namespace OpenKeep.Recipes
{
    /// <summary>
    /// The view button's icons, small white shapes drawn in code and coloured by the Image that shows them: three bars
    /// for the list, four thin ones for the compact list, and 3 x 3, 2 x 2 or one square for the small, medium and large
    /// grids.
    /// </summary>
    public static class RecipeSprites
    {
        private const int Size = 16;
        private const int Gap = 2;

        private static readonly Sprite[] views = new Sprite[Enum.GetValues(typeof(RecipeView)).Length];

        public static Sprite Of(RecipeView view)
        {
            int index = Mathf.Clamp((int)view, 0, views.Length - 1);
            if (views[index] == null)
                views[index] = Make(Shape((RecipeView)index));
            return views[index];
        }

        private static Func<int, int, bool> Shape(RecipeView view)
        {
            switch (view)
            {
                case RecipeView.List: return (x, y) => Bar(y, 3);
                case RecipeView.CompactList: return (x, y) => Bar(y, 4);
                case RecipeView.SmallGrid: return (x, y) => Bar(x, 3) && Bar(y, 3);
                case RecipeView.MediumGrid: return (x, y) => Bar(x, 2) && Bar(y, 2);
                default: return (x, y) => true;
            }
        }

        /// <summary>Inside one of n stripes across the icon, with a 2 pixel gap between stripes.</summary>
        private static bool Bar(int at, int stripes)
        {
            int pitch = (Size + Gap) / stripes;
            return at % pitch < pitch - Gap;
        }

        private static Sprite Make(Func<int, int, bool> inside)
        {
            Texture2D texture = new Texture2D(Size, Size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
            };
            for (int y = 0; y < Size; y++)
            {
                for (int x = 0; x < Size; x++)
                    texture.SetPixel(x, y, inside(x, y) ? Color.white : Color.clear);
            }
            texture.Apply();
            Sprite sprite = Sprite.Create(texture, new Rect(0f, 0f, Size, Size), new Vector2(0.5f, 0.5f), 100f);
            sprite.name = "OpenKeep_sprite";
            return sprite;
        }
    }
}
