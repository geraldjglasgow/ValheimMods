using OpenKeep.Core;

namespace OpenKeep.Salvage
{
    /// <summary>One entry of the YAML <c>overrides:</c> map: a vocabulary key and the return fraction for the items it matches.</summary>
    public sealed class FractionOverride
    {
        public FractionOverride(string key, float fraction)
        {
            Key = key;
            Fraction = fraction;
        }

        public string Key { get; }

        public float Fraction { get; }

        /// <summary>Parsed once every group of every file is known (in the model's Verify).</summary>
        public ItemMatcher Matcher { get; set; }

        public bool IsExact => Matcher != null && Matcher.IsExactName;

        public bool Matches(ItemDrop.ItemData item) => Matcher != null && Matcher.Matches(item);
    }
}
