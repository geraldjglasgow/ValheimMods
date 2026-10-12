using BundlePrefabs;
using UnityEngine;

using Object = UnityEngine.Object;
using SharedData = ItemDrop.ItemData.SharedData;

namespace EliteEquipment.Casters
{
    /// <summary>
    /// An eitr food (<see cref="CasterFood"/>): a copy of a game dish (its eating sound and animation, stack, sparkle and
    /// the way the game shows it in the hand while eaten), wearing the workshop's model under its <c>attach</c> child in
    /// the dish's own material with the bake, with its own words, icon and food values. A cold dish loses the soup's steam.
    /// The game shows the eitr in its tooltip as for any food.
    /// </summary>
    public static class FoodItems
    {
        private const string Holder = "attach";
        private const string Steam = "fx_FoodSteam_Small";
        private const float Gloss = 0.1f;

        public static GameObject Build(FoodPiece food, GameObject dish)
        {
            GameObject item = PrefabBench.Copy(dish, food.Prefab);
            Look(item, CasterBundles.Prefab(food.Bundle, food.Asset));
            SharedData shared = item.GetComponent<ItemDrop>().m_itemData.m_shared;
            shared.m_name = food.Token;
            shared.m_description = food.DescriptionToken;
            shared.m_variants = 0;
            CasterItemLook.Icon(shared, CasterBundles.Sprite(food.Bundle, food.Asset + "_icon"));
            shared.m_food = food.Health;
            shared.m_foodStamina = food.Stamina;
            shared.m_foodEitr = food.Eitr;
            shared.m_foodBurnTime = food.Seconds;
            shared.m_foodRegen = food.Regen;
            return item;
        }

        private static void Look(GameObject item, GameObject model)
        {
            Transform steam = item.transform.Find(Steam);
            if (steam != null)
                Object.DestroyImmediate(steam.gameObject);
            Transform holder = item.transform.Find(Holder);
            if (holder == null || model == null)
            {
                Plugin.Log.LogWarning($"EliteEquipment: {item.name} keeps the game dish's look (no workshop model)");
                return;
            }
            Material game = GameMaterials.Borrow(item, Holder);
            Strip(holder);
            GameObject copy = Object.Instantiate(model, holder, false);
            copy.name = "model";
            Dress(copy, game, holder.gameObject.layer);
            if (item.GetComponentsInChildren<Collider>(true).Length == 0)
                Collide(holder, ModelBounds.In(copy, holder));
        }

        /// <summary>
        /// The dish's own look goes: the bowl drawn on the <c>attach</c> node itself (the soup's, at a scale of 0.171) and its
        /// children; the node keeps its place at scale 1, since the workshop model is built at its true size.
        /// </summary>
        private static void Strip(Transform holder)
        {
            for (int i = holder.childCount - 1; i >= 0; i--)
                Object.DestroyImmediate(holder.GetChild(i).gameObject);
            foreach (Component part in holder.GetComponents<Component>())
            {
                if (part is Renderer || part is MeshFilter || part is Collider || part is LODGroup)
                    Object.DestroyImmediate(part);
            }
            holder.localScale = Vector3.one;
        }

        private static void Dress(GameObject model, Material game, int layer)
        {
            foreach (Renderer renderer in model.GetComponentsInChildren<Renderer>(true))
            {
                renderer.gameObject.layer = layer;
                if (game != null && renderer.sharedMaterial != null)
                    renderer.sharedMaterial = GameMaterials.Plain(GameMaterials.Dress(game, renderer.sharedMaterial), Gloss);
            }
        }

        private static void Collide(Transform holder, Bounds bounds)
        {
            BoxCollider box = holder.gameObject.AddComponent<BoxCollider>();
            box.center = bounds.center;
            box.size = Vector3.Max(bounds.size, Vector3.one * 0.05f);
        }
    }
}
