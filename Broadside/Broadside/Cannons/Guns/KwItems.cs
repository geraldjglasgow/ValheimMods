using System.Collections.Generic;
using BundlePrefabs;
using HarmonyLib;
using PatchGuard;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// The cannons' own items (<see cref="KwItemDef"/>: the cannonballs and black powder) in the game, the same on every
    /// peer: built once when the network scene first wakes (a copy of a game item wearing its own model and icon,
    /// <see cref="KwItemModel"/>), registered with the scene every time it wakes, and kept with their recipes in the live
    /// item database and every one the game builds or copies later. An item carries no damage of its own (a weapon's shot says what it does). A recipe's station is looked up each time (none in the main menu: the recipe waits, disabled).
    /// An item of a tier not offered yet (<see cref="KwTiers.Shown"/>: the iron and flametal cannonballs) is made and
    /// registered all the same, so a world's stacks of it still load, but has no recipe.
    /// </summary>
    [HarmonyPatch]
    public static class KwItems
    {
        private static readonly Dictionary<string, GameObject> Built = new Dictionary<string, GameObject>();
        private static readonly Dictionary<string, Recipe> Recipes = new Dictionary<string, Recipe>();

        /// <summary>A cannon item's prefab by name; null before the scene woke or when it could not be made.</summary>
        public static GameObject? Prefab(string name) => Built.TryGetValue(name, out GameObject item) ? item : null;

        /// <summary>Builds the items not built yet, registers them with the scene, and adds them to the live item database.</summary>
        public static void Install(ZNetScene scene)
        {
            foreach (KwItemDef def in KwArmory.Items)
                Make(scene, def);
            foreach (GameObject item in Built.Values)
                NetPrefabs.Register(scene, item);
            if (ObjectDB.instance != null)
                Add(ObjectDB.instance);
        }

        [HarmonyPatch(typeof(ObjectDB), nameof(ObjectDB.Awake))]
        [HarmonyPostfix]
        private static void Woke(ObjectDB __instance) => Safely(__instance);

        [HarmonyPatch(typeof(ObjectDB), nameof(ObjectDB.CopyOtherDB))]
        [HarmonyPostfix]
        private static void Copied(ObjectDB __instance) => Safely(__instance);

        // Logged by the guard and kept in: the item database must still finish waking.
        private static void Safely(ObjectDB db)
        {
            try
            {
                Guard.Run("Broadside cannon items", static d => Add(d), db);
            }
            catch (System.Exception)
            {
            }
        }

        private static void Make(ZNetScene scene, KwItemDef def)
        {
            if (Built.ContainsKey(def.Name))
                return;
            GameObject? game = scene.GetPrefab(def.Base);
            if (game == null || game.GetComponent<ItemDrop>() == null)
            {
                Log.Warn($"Broadside cannon items: {def.Name} not made: the game has no item {def.Base} to copy.");
                return;
            }
            GameObject copy = PrefabBench.Copy(game, def.Name);
            KwItemModel.Dress(copy, def);
            def.Finish?.Invoke(copy);
            Describe(copy.GetComponent<ItemDrop>(), def, Icon(copy, def.Name));
            Built[def.Name] = copy;
        }

        // The item's icon rendered from its own model (Broadside's icon camera), or kept from an earlier launch
        // (KwIconCache); none on a dedicated server or a failed render.
        private static Sprite? Icon(GameObject item, string name)
        {
            if (!KwIcons.CanRender)
                return null;
            KwIconRig? rig = null;
            try
            {
                return KwIcons.Icon(ref rig, item.transform, name, null);
            }
            finally
            {
                rig?.Dispose();
            }
        }

        private static void Describe(ItemDrop drop, KwItemDef def, Sprite? icon)
        {
            drop.m_itemData.m_dropPrefab = drop.gameObject;
            ItemDrop.ItemData.SharedData shared = drop.m_itemData.m_shared;
            (shared.m_name, shared.m_description, shared.m_itemType, shared.m_ammoType) = (def.Label, def.Description, def.Type, def.AmmoType);
            (shared.m_maxStackSize, shared.m_weight, shared.m_value, shared.m_teleportable) = (def.Stack, def.Weight, def.Value, def.Teleports);
            shared.m_damages = new HitData.DamageTypes();
            if (icon != null)
                shared.m_icons = new[] { icon };
        }

        private static void Add(ObjectDB db)
        {
            foreach (GameObject item in Built.Values)
            {
                int hash = item.name.GetStableHashCode();
                if (db.m_itemByHash.ContainsKey(hash))
                    continue;
                db.m_items.Add(item);
                db.m_itemByHash[hash] = item;
            }
            foreach (KwItemDef def in KwArmory.Items)
                Recipe(db, def);
        }

        private static void Recipe(ObjectDB db, KwItemDef def)
        {
            if (!Built.TryGetValue(def.Name, out GameObject item) || def.Cost.Count == 0 || !KwTiers.Shown(def.Tier))
                return;
            if (!Recipes.TryGetValue(def.Name, out Recipe recipe))
            {
                recipe = ScriptableObject.CreateInstance<Recipe>();
                (recipe.name, recipe.m_item, recipe.m_amount) = ("Recipe_" + def.Name, item.GetComponent<ItemDrop>(), Mathf.Max(1, def.Amount));
                Recipes[def.Name] = recipe;
            }
            (string station, int level) = def.Station != null ? (def.Station, def.StationLevel) : KwTiers.Station(def.Tier);
            GameObject? bench = station.Length > 0 && ZNetScene.instance != null ? ZNetScene.instance.GetPrefab(station) : null;
            recipe.m_craftingStation = bench != null ? bench.GetComponent<CraftingStation>() : null;
            recipe.m_minStationLevel = level;
            recipe.m_resources = Cost(db, def);
            recipe.m_enabled = recipe.m_craftingStation != null || station.Length == 0;
            if (!db.m_recipes.Contains(recipe))
                db.m_recipes.Add(recipe);
        }

        private static Piece.Requirement[] Cost(ObjectDB db, KwItemDef def)
        {
            List<Piece.Requirement> cost = new List<Piece.Requirement>();
            foreach ((string name, int amount) in def.Cost)
            {
                ItemDrop? drop = db.GetItemPrefab(name)?.GetComponent<ItemDrop>();
                if (drop != null)
                    cost.Add(new Piece.Requirement { m_resItem = drop, m_amount = amount, m_recover = true });
                else
                    Log.Warn($"Broadside cannon items: {def.Name} costs {name}, which the game does not have; left out.");
            }
            return cost.ToArray();
        }
    }
}
