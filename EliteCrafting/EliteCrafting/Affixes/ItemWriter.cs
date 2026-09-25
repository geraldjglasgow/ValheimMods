using System.Collections.Generic;
using EliteCrafting.Core;

namespace EliteCrafting.Affixes
{
    /// <summary>
    /// Writes <see cref="StateData"/> into a custom-data dictionary (item-data.md section 3): only our keys, a key
    /// whose value would be empty is removed, <c>ecf_v</c> present exactly when another of our keys is, the reserved
    /// <c>ecf_tier</c> left as it was, and every other mod's key untouched.
    /// </summary>
    internal static class ItemWriter
    {
        public static void Serialize(StateData data, Dictionary<string, string> target)
        {
            Set(target, ItemKeys.Rarity, data.RarityId);
            Set(target, ItemKeys.Affixes, ItemCodec.EncodeList(data.Segments));
            Set(target, ItemKeys.Bound, BoundOnItem(data));
            Set(target, ItemKeys.Refine, data.Refine != 0f ? Numbers.Format(data.Refine) : data.RefineRaw);
            Set(target, ItemKeys.Sealed, data.SealedReason);
            Set(target, ItemKeys.Sigil, data.SigilId);
            Set(target, ItemKeys.Version, AnyStateKey(target) ? Numbers.Format(ItemKeys.CurrentFormat) : null);
        }

        private static string? BoundOnItem(StateData data)
        {
            foreach (ItemSegment segment in data.Segments)
            {
                if (segment.IsRoll && segment.Roll.Id == data.BoundId)
                {
                    return data.BoundId;
                }
            }
            return null;
        }

        private static bool AnyStateKey(Dictionary<string, string> target)
        {
            foreach (string key in ItemKeys.StateKeys)
            {
                if (target.ContainsKey(key))
                {
                    return true;
                }
            }
            return false;
        }

        private static void Set(Dictionary<string, string> target, string key, string? value)
        {
            if (string.IsNullOrEmpty(value))
            {
                target.Remove(key);
            }
            else
            {
                target[key] = value!;
            }
        }
    }
}
