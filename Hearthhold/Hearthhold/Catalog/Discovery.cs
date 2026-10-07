using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace Hearthhold
{
    /// <summary>
    /// Fills <see cref="Kitchen"/> and <see cref="Sources"/> from the item database's recipes and the scene's prefabs,
    /// then makes every product and source item a star item (<see cref="StarItems"/>). Runs last after both
    /// ZNetScene.Awake and ObjectDB.Awake; whichever comes second finds both, so prefabs and recipes another mod
    /// registers in its own Awake postfix are found too. Adding is idempotent, so running twice is harmless. Runs on every
    /// machine, the dedicated server included.
    /// </summary>
    public static class Discovery
    {
        [HarmonyPatch(typeof(ZNetScene), nameof(ZNetScene.Awake))]
        private static class SceneAwake
        {
            [HarmonyPostfix]
            [HarmonyPriority(Priority.Last)]
            private static void Postfix() => HookGuard.Run("discovery", Run);
        }

        [HarmonyPatch(typeof(ObjectDB), nameof(ObjectDB.Awake))]
        private static class DatabaseAwake
        {
            [HarmonyPostfix]
            [HarmonyPriority(Priority.Last)]
            private static void Postfix() => HookGuard.Run("discovery", Run);
        }

        private static void Run()
        {
            if (ZNetScene.instance == null || ObjectDB.instance == null || ObjectDB.instance.m_items.Count == 0)
                return;
            List<GameObject> prefabs = ZNetScene.instance.m_prefabs;
            foreach (GameObject prefab in prefabs)
                if (prefab != null)
                    AddKitchenAndPlants(prefab);
            foreach (Recipe recipe in ObjectDB.instance.m_recipes)
                AddRecipe(recipe);
            foreach (GameObject prefab in prefabs)
                if (prefab != null)
                    AddSources(prefab);
            StarItems.RegisterAll();
        }

        private static void AddRecipe(Recipe recipe)
        {
            if (recipe == null || recipe.m_item == null)
                return;
            if (!Kitchen.IsKitchen(recipe.m_craftingStation) || !IsFoodish(recipe.m_item))
                return;
            Kitchen.AddProduct(recipe.m_item, false);
            foreach (Piece.Requirement requirement in recipe.m_resources)
                Kitchen.AddIngredient(requirement?.m_resItem);
        }

        private static void AddKitchenAndPlants(GameObject prefab)
        {
            CookingStation station = prefab.GetComponent<CookingStation>();
            if (Kitchen.IsKitchen(station))
                foreach (CookingStation.ItemConversion conversion in station.m_conversion)
                    AddConversion(conversion.m_from, conversion.m_to, false);
            Fermenter fermenter = prefab.GetComponent<Fermenter>();
            if (fermenter != null)
                foreach (Fermenter.ItemConversion conversion in fermenter.m_conversion)
                    AddConversion(conversion.m_from, conversion.m_to, true);
            Plant plant = prefab.GetComponent<Plant>();
            if (plant?.m_grownPrefabs != null)
                foreach (GameObject grown in plant.m_grownPrefabs)
                    if (grown != null && grown.GetComponent<Pickable>() != null)
                        Sources.AddCropPickable(grown.name);
        }

        private static void AddConversion(ItemDrop from, ItemDrop to, bool fermenter)
        {
            Kitchen.AddIngredient(from);
            Kitchen.AddProduct(to, fermenter);
            Kitchen.AddConversion(from, to);
        }

        private static void AddSources(GameObject prefab)
        {
            Pickable pickable = prefab.GetComponent<Pickable>();
            if (pickable != null && pickable.m_itemPrefab != null)
                AddPick(prefab.name, pickable.m_itemPrefab);
            Beehive hive = prefab.GetComponent<Beehive>();
            if (hive != null && hive.m_honeyItem != null)
                AddProduce(hive.m_honeyItem.gameObject);
            SapCollector sap = prefab.GetComponent<SapCollector>();
            if (sap != null && sap.m_spawnItem != null)
                AddProduce(sap.m_spawnItem.gameObject);
            AddMeat(prefab.GetComponent<CharacterDrop>());
        }

        /// <summary>A crop counts when it is food or a kitchen uses it; forage when it is food or on <see cref="Sources.ForageExtras"/>.</summary>
        private static void AddPick(string pickable, GameObject item)
        {
            if (Sources.IsCropPickable(pickable))
            {
                if (IsFood(item) || Kitchen.IsIngredient(item.name))
                    Sources.AddCrop(item.name);
            }
            else if (IsFood(item) || Sources.ForageExtras.Contains(item.name))
                Sources.AddForage(item.name);
        }

        /// <summary>What a hive or sap collector gives, when it is food or on the list (honey, sap; not a modded nest's feathers).</summary>
        private static void AddProduce(GameObject item)
        {
            if (IsFood(item) || Sources.ForageExtras.Contains(item.name))
                Sources.AddForage(item.name);
        }

        private static void AddMeat(CharacterDrop drops)
        {
            if (drops?.m_drops == null)
                return;
            foreach (CharacterDrop.Drop drop in drops.m_drops)
                if (drop?.m_prefab != null && (IsFood(drop.m_prefab) || Sources.MeatExtras.Contains(drop.m_prefab.name))
                    && drop.m_prefab.GetComponent<EggGrow>() == null)
                    Sources.AddMeat(drop.m_prefab.name);
        }

        /// <summary>
        /// A kitchen recipe that makes food (a food value) or an intermediate a station or barrel turns into food (mead
        /// bases, dough, uncooked pies, raw fish). Fishing bait and feast materials are made in kitchens too; their recipes
        /// are left out, or the creature parts they take (feathers, eyes, ice, coal...) would count as meat. Runs after
        /// the stations, so their inputs are known.
        /// </summary>
        private static bool IsFoodish(ItemDrop item)
        {
            ItemDrop.ItemData.SharedData shared = item.m_itemData?.m_shared;
            return shared != null && (Kitchen.Value(shared) > 0f || Kitchen.IsStationInput(item.gameObject.name));
        }

        /// <summary>Food: an item that can be eaten, or one a cooking station or barrel turns into food (raw meat).</summary>
        private static bool IsFood(GameObject item)
        {
            ItemDrop drop = item.GetComponent<ItemDrop>();
            if (drop?.m_itemData?.m_shared == null)
                return false;
            return Kitchen.Value(drop.m_itemData.m_shared) > 0f || Kitchen.IsStationInput(item.name);
        }
    }
}
