using System.Collections.Generic;
using EliteEquipment.Core;

namespace EliteEquipment.Weapons
{
    /// <summary>
    /// A recipe per new weapon beside a same-tier game weapon (<see cref="RecipeBeside"/>: same station, station level,
    /// repair station, order and upgrade kits), costing its own materials (<see cref="WeaponSets"/>). In every database the
    /// game builds or copies once the items exist; enabled while the switch is on.
    /// </summary>
    public static class WeaponRecipes
    {
        private static readonly Dictionary<WeaponSet, Recipe> made = new Dictionary<WeaponSet, Recipe>();

        /// <summary>Adds or updates the recipes in this database (from <see cref="WeaponsSwitch"/>).</summary>
        public static void Install(ObjectDB db)
        {
            if (db == null)
                return;
            foreach (WeaponSet set in WeaponSets.All)
            {
                Recipe beside = set.Item != null ? RecipeBeside.Find(db, set.Beside) : null;
                if (beside == null)
                    continue;
                if (!made.TryGetValue(set, out Recipe recipe))
                    made[set] = recipe = RecipeBeside.Create(set.Item);
                RecipeBeside.Install(db, recipe, beside, set.Cost, WeaponsSettings.On);
            }
        }
    }
}
