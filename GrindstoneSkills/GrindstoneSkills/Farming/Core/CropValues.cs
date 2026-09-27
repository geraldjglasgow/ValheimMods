using System.Collections.Generic;
using UnityEngine;

namespace GrindstoneSkills
{
    /// <summary>
    /// A crop plant's value for experience tiers: the best among its seed and crops of an item's food value (health +
    /// stamina + eitr), else what it is milled into, else the best kitchen dish it goes into (<see cref="KitchenUses"/>).
    /// Carrots are worth 42, poteitr 105, barley what its flour bakes into. Computed on first use per plant and cleared at
    /// each discovery.
    /// </summary>
    public static class CropValues
    {
        private const int MaxChain = 4;

        private static readonly Dictionary<string, float> values = new Dictionary<string, float>();

        public static void Clear() => values.Clear();

        /// <summary>The plant's value; 0 for null or a plant whose items feed nothing.</summary>
        public static float Of(CropPlant crop)
        {
            if (crop == null)
                return 0f;
            if (values.TryGetValue(crop.Prefab, out float value))
                return value;
            foreach (ItemDrop item in crop.Items())
                value = Mathf.Max(value, ItemValue(item, 0));
            values[crop.Prefab] = value;
            return value;
        }

        private static float ItemValue(ItemDrop item, int depth)
        {
            if (item == null || depth > MaxChain)
                return 0f;
            float value = Kitchen.Value(item.m_itemData.m_shared);
            if (value > 0f)
                return value;
            value = KitchenUses.BestDish(item.name);
            foreach (KitchenUses.Conversion conversion in KitchenUses.Conversions)
            {
                if (conversion.From == item)
                    value = Mathf.Max(value, ItemValue(conversion.To, depth + 1));
            }
            return value;
        }
    }
}
