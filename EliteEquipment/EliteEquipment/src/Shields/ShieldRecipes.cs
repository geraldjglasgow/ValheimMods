using System.Collections.Generic;
using EliteEquipment.Core;

namespace EliteEquipment.Shields
{
    /// <summary>
    /// A recipe per new shield, beside the game shield of its biome (<see cref="RecipeBeside"/>: same station, station
    /// level, repair station, order and upgrade kits), costing the new shield's own materials (<see cref="ShieldSets"/>).
    /// In every database the game builds or copies once the items exist; enabled while the switch is on.
    /// </summary>
    public static class ShieldRecipes
    {
        private static readonly Dictionary<ShieldSet, Recipe> made = new Dictionary<ShieldSet, Recipe>();

        /// <summary>Adds or updates the recipes in this database (from <see cref="ShieldsSwitch"/>).</summary>
        public static void Install(ObjectDB db)
        {
            if (db == null)
                return;
            foreach (ShieldSet set in ShieldSets.All)
            {
                Recipe beside = set.Item != null ? RecipeBeside.Find(db, set.Shield) : null;
                if (beside == null)
                    continue;
                if (!made.TryGetValue(set, out Recipe recipe))
                    made[set] = recipe = RecipeBeside.Create(set.Item);
                RecipeBeside.Install(db, recipe, beside, set.Cost, ShieldsSettings.On);
            }
        }
    }
}
