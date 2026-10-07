using System.Collections.Generic;
using EliteCrafting.Core;

namespace EliteCrafting.Affixes
{
    /// <summary>
    /// Writes <see cref="StateData"/> into a custom-data dictionary (item-data.md section 3): only our keys, a key
    /// whose value would be empty is removed, <c>ecf_v</c> present exactly when another of our keys is, the reserved
    /// <c>ecf_tier</c> left as it was, the retired keys removed, and every other mod's key untouched.
    /// </summary>
    internal static class ItemWriter
    {
        public static void Serialize(StateData data, Dictionary<string, string> target)
        {
            Set(target, ItemKeys.Rarity, data.RarityId);
            Set(target, ItemKeys.Affixes, ItemCodec.EncodeList(data.Segments));
            target.Remove(ItemKeys.LegacyAffixes);
            Set(target, ItemKeys.Sealed, data.SealedReason);
            Set(target, ItemKeys.Sockets, GemCodec.EncodeSockets(data.Sockets));
            Set(target, ItemKeys.Gems, GemCodec.EncodeGems(data.Gems));
            foreach (string key in ItemKeys.RetiredKeys)
            {
                target.Remove(key);
            }
            Set(target, ItemKeys.Version, AnyStateKey(target) ? Numbers.Format(ItemKeys.CurrentFormat) : null);
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

        /// <summary>Sets a key, or removes it when the value is empty.</summary>
        internal static void Set(Dictionary<string, string> target, string key, string? value)
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
