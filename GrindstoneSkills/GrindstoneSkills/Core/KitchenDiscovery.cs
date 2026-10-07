using System.Collections.Generic;
using HarmonyLib;

namespace GrindstoneSkills
{
    /// <summary>
    /// Fills <see cref="Kitchen"/> from the scene's prefabs (cooking stations, fermenters, read once through
    /// <see cref="PrefabIndex"/>) and the item database's recipes (kitchen crafting stations). Runs last after both ZNetScene.Awake and ObjectDB.Awake, whichever comes
    /// second finds both, so prefabs and recipes another mod registers in its own Awake postfix are found too.
    /// Adding is idempotent, so running twice is harmless.
    /// </summary>
    public static class KitchenDiscovery
    {
        [HarmonyPatch(typeof(ZNetScene), nameof(ZNetScene.Awake))]
        private static class SceneAwake
        {
            [HarmonyPostfix]
            [HarmonyPriority(Priority.Last)]
            private static void Postfix() => Discover();
        }

        [HarmonyPatch(typeof(ObjectDB), nameof(ObjectDB.Awake))]
        private static class DatabaseAwake
        {
            [HarmonyPostfix]
            [HarmonyPriority(Priority.Last)]
            private static void Postfix() => Discover();
        }

        public static void Discover()
        {
            int before = Kitchen.Count;
            if (ZNetScene.instance != null)
                foreach (PrefabIndex.Converter converter in PrefabIndex.Scene().Converters)
                    AddStation(converter);
            if (ObjectDB.instance != null)
                foreach (Recipe recipe in ObjectDB.instance.m_recipes)
                    AddRecipe(recipe);
            if (Kitchen.Count != before)
                GrindstoneSkills.Log.LogInfo($"Kitchen products: {Kitchen.Count}.");
        }

        private static void AddStation(PrefabIndex.Converter converter)
        {
            CookingStation station = converter.Station;
            if (Kitchen.IsKitchen(station))
                foreach (CookingStation.ItemConversion conversion in station.m_conversion)
                    AddConversion(conversion.m_from, conversion.m_to);
            Fermenter fermenter = converter.Fermenter;
            if (fermenter != null)
                foreach (Fermenter.ItemConversion conversion in fermenter.m_conversion)
                    AddConversion(conversion.m_from, conversion.m_to);
        }

        private static void AddConversion(ItemDrop from, ItemDrop to)
        {
            Kitchen.AddItem(to);
            Kitchen.AddConversion(from, to);
        }

        private static void AddRecipe(Recipe recipe)
        {
            if (recipe != null && recipe.m_item != null && Kitchen.IsKitchen(recipe.m_craftingStation))
                Kitchen.AddItem(recipe.m_item);
        }
    }
}
