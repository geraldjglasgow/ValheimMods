using OpenKeep.Stacks;

namespace OpenKeep.Boots
{
    /// <summary>
    /// The weight half of the 80/20 split. Stacks writes every item's weight from the weight it remembered as the game's
    /// (into the prefab, every live copy and every new one), so the split is a share of that remembered weight, taken in
    /// Stacks' own apply: a split leggings weighs 80% of its own while boots are on, before the multiplier, the per item
    /// entries and the YAML rules. The boots were made at 20% of their leggings' weight and Stacks remembers that as theirs.
    /// </summary>
    public static class BootsShare
    {
        public static ItemValue Weighed(string prefabName, ItemValue vanilla) =>
            BootsSettings.On && BootSets.ByLegs(prefabName) != null
                ? new ItemValue(vanilla.Stack, vanilla.Weight * BootsStats.LegsShare)
                : vanilla;

        /// <summary>A leggings' own weight, as Stacks remembered it when it already has (else the prefab's).</summary>
        public static float LegsWeight(string legsName, ItemDrop.ItemData.SharedData shared) =>
            VanillaValues.TryGet(legsName, out ItemValue value) ? value.Weight : shared.m_weight;
    }
}
