using System.Collections.Generic;
using EarthWright.Core;
using UnityEngine;

namespace EarthWright.Gear
{
    /// <summary>
    /// The shovel's crafting recipe: one Recipe object per process, added to every object database's recipe list once
    /// and rewritten from the settings (on/off, station, materials, upgrade cost) whenever they change. The same recipe
    /// is what the game upgrades and repairs the shovel with. Every machine builds it the same way, so crafting agrees
    /// between client and server.
    /// </summary>
    public static class ShovelRecipe
    {
        public static Recipe Recipe { get; private set; }

        /// <summary>Adds the recipe to the database when missing and writes the current settings into it.</summary>
        public static void RegisterIn(ObjectDB db)
        {
            ItemDrop shovel = ShovelPrefab.Drop;
            if (db == null || shovel == null)
                return;
            if (Recipe == null)
            {
                Recipe = ScriptableObject.CreateInstance<Recipe>();
                Recipe.name = "Recipe_" + ToolNames.Shovel;
                Recipe.m_item = shovel;
                Recipe.m_amount = 1;
                Recipe.hideFlags = HideFlags.DontUnloadUnusedAsset;
            }
            if (!db.m_recipes.Contains(Recipe))
                db.m_recipes.Add(Recipe);
            Apply(db);
        }

        /// <summary>Writes enabled state, station, crafting materials and the upgrade cost per level.</summary>
        public static void Apply(ObjectDB db)
        {
            if (Recipe == null || db == null)
                return;
            Recipe.m_enabled = GearSettings.ShovelEnabled.Value && GeneralSettings.Enabled.Value;
            // An unknown station name falls back to the hoe's station (the workbench), never to crafting by hand.
            Recipe.m_craftingStation = RecipeParts.Station(db, GearSettings.ShovelStation.Value) ?? ToolRecipes.Find(db, ToolNames.Hoe)?.m_craftingStation;
            Recipe.m_repairStation = null;
            Recipe.m_minStationLevel = 1;
            List<Piece.Requirement> list = RecipeParts.Crafting(db, GearSettings.ShovelRecipe.Value);
            list.AddRange(UpgraderResources(db));
            RecipeParts.SetPerLevel(db, list, GearSettings.Shovel.UpgradeCost.Value);
            Recipe.m_resources = list.ToArray();
        }

        /// <summary>
        /// The hoe recipe's Refinement Forge resources, copied so the shovel can be refined there like the hoe (the game
        /// skips, with a warning, any upgradable recipe that has none).
        /// </summary>
        private static IEnumerable<Piece.Requirement> UpgraderResources(ObjectDB db)
        {
            Recipe hoe = ToolRecipes.Find(db, ToolNames.Hoe);
            if (hoe == null)
                yield break;
            foreach (Piece.Requirement r in ToolRecipes.Original(hoe))
            {
                if (r.m_upgraderResource)
                    yield return RecipeParts.Clone(r);
            }
        }
    }
}
