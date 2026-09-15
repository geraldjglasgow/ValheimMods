using OpenKeep.Core;

namespace OpenKeep.Signs
{
    /// <summary>Which containers get a sign: the module switch, the prefab's YAML rule, no ships or carts, placed by a player.</summary>
    public static class SignRules
    {
        /// <summary>The prefab's rule from the current YAML model, or the default when it is not listed.</summary>
        public static SignRule RuleFor(string prefab)
        {
            SignsModel model = SignsModule.Set != null ? SignsModule.Set.Current as SignsModel : null;
            if (model != null && !string.IsNullOrEmpty(prefab) && model.Containers.TryGetValue(prefab, out SignRule rule))
                return rule;
            return SignRule.Default;
        }

        public static SignRule RuleFor(Container container) => RuleFor(ContainerScan.PrefabName(container));

        /// <summary>
        /// Module on, prefab not switched off, not a ship hold or a cart (the sign would not travel), and a piece a
        /// player placed (dungeon chests, spawned treasure and creature inventories have no creator).
        /// </summary>
        public static bool Allows(Container container)
        {
            if (container == null || !SignsSettings.Enabled.Value)
                return false;
            if (ContainerScan.IsShip(container) || ContainerScan.IsCart(container))
                return false;
            Piece piece = container.m_piece;
            if (piece == null || !piece.IsPlacedByPlayer())
                return false;
            return RuleFor(container).Enabled;
        }
    }
}
