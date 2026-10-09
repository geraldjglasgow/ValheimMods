using System.Collections.Generic;
using UnityEngine;

namespace EliteEquipment.Fitted
{
    /// <summary>
    /// A chest drawn in over fitted leggings (<see cref="ChestPull"/>): which of the game's chests, where its hanging cloth
    /// lies on its texture (atlas texels from the top-left, at <see cref="Base"/> px), and how low that cloth may hang
    /// afterwards. Everything else below the waist counts as plates.
    /// </summary>
    internal sealed class ChestShape
    {
        private static readonly Dictionary<int, ChestShape> byChest = new Dictionary<int, ChestShape>();

        static ChestShape()
        {
            // The Protector breastplate: its blue tabards, front and back, hung over the balloon trousers to the shins.
            Add(new ChestShape("ArmorDeepNorthHeavyChest", 256, 0.62f, new RectInt(222, 95, 34, 115), new RectInt(40, 205, 88, 51)));
        }

        private ChestShape(string chest, int atlas, float clothBottom, params RectInt[] cloth)
        {
            Hash = chest.GetStableHashCode();
            Base = atlas;
            ClothBottom = clothBottom;
            Cloth = cloth;
        }

        public int Hash { get; }
        public int Base { get; }

        /// <summary>The lowest the cloth hangs once drawn in, metres in rest space.</summary>
        public float ClothBottom { get; }

        public RectInt[] Cloth { get; }

        public static ChestShape Of(int chestHash) => byChest.TryGetValue(chestHash, out ChestShape shape) ? shape : null;

        /// <summary>True for a vertex whose texture is the hanging cloth's.</summary>
        public bool IsCloth(Vector2 uv)
        {
            var texel = new Vector2(uv.x * Base, (1f - uv.y) * Base);
            foreach (RectInt rect in Cloth)
            {
                if (texel.x >= rect.xMin && texel.x <= rect.xMax && texel.y >= rect.yMin && texel.y <= rect.yMax)
                    return true;
            }
            return false;
        }

        private static void Add(ChestShape shape) => byChest[shape.Hash] = shape;
    }
}
