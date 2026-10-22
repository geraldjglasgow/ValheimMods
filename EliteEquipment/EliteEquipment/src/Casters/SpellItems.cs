using BundlePrefabs;
using Translations;
using UnityEngine;

using Object = UnityEngine.Object;
using SharedData = ItemDrop.ItemData.SharedData;

namespace EliteEquipment.Casters
{
    /// <summary>
    /// A spell item (<see cref="Spell.Item"/>): a copy of the game's raspberries (a Consumable, so using it from the
    /// inventory or the hotbar goes to <c>Player.ConsumeItem</c>, where <see cref="SpellLearning"/> teaches the spell
    /// instead of eating), with no food in it, wearing the workshop's spell scroll on the ground and its own icon.
    /// </summary>
    public static class SpellItems
    {
        private const int Stack = 10;

        public static GameObject Build(Spell spell, GameObject berries)
        {
            GameObject item = PrefabBench.Copy(berries, spell.Item);
            Look(item, CasterBundles.Prefab(CasterBundles.Spells, "ee_spell_scroll"));
            SharedData shared = item.GetComponent<ItemDrop>().m_itemData.m_shared;
            shared.m_name = spell.ItemToken;
            shared.m_description = Lang.Token("ee_spellitem_desc") + "\n\n" + spell.DescriptionToken;
            shared.m_variants = 0;
            CasterItemLook.Icon(shared, CasterBundles.Sprite(CasterBundles.Spells, spell.ItemIcon));
            NoFood(shared);
            return item;
        }

        private static void NoFood(SharedData shared)
        {
            shared.m_food = 0f;
            shared.m_foodStamina = 0f;
            shared.m_foodEitr = 0f;
            shared.m_foodRegen = 0f;
            shared.m_foodBurnTime = 0f;
            shared.m_consumeStatusEffect = null;
            shared.m_maxStackSize = Stack;
            shared.m_weight = 0.1f;
            shared.m_teleportable = true;
        }

        /// <summary>
        /// The berries' meshes and colliders go, the scroll goes in (in the berries' own item material wearing its bake)
        /// with a box round it; without the scroll the berries stay.
        /// </summary>
        private static void Look(GameObject item, GameObject scroll)
        {
            if (scroll == null)
                return;
            Material game = GameMaterials.Borrow(item);
            Strip(item);
            GameObject model = Object.Instantiate(scroll, item.transform, false);
            model.name = "model";
            foreach (Collider collider in model.GetComponentsInChildren<Collider>(true))
                Object.DestroyImmediate(collider);
            foreach (Transform part in model.GetComponentsInChildren<Transform>(true))
                part.gameObject.layer = item.layer;
            foreach (Renderer renderer in model.GetComponentsInChildren<Renderer>(true))
            {
                if (game != null && renderer.sharedMaterial != null)
                    renderer.sharedMaterial = GameMaterials.Plain(GameMaterials.Dress(game, renderer.sharedMaterial), 0.1f);
            }
            Bounds bounds = ModelBounds.In(model, item.transform);
            BoxCollider box = item.AddComponent<BoxCollider>();
            box.center = bounds.center;
            box.size = Vector3.Max(bounds.size, Vector3.one * 0.05f);
        }

        private static void Strip(GameObject item)
        {
            foreach (Renderer renderer in item.GetComponentsInChildren<Renderer>(true))
            {
                if (renderer == null)
                    continue;
                if (renderer.gameObject != item)
                    Object.DestroyImmediate(renderer.gameObject);
                else
                    Object.DestroyImmediate(renderer);
            }
            foreach (MeshFilter filter in item.GetComponents<MeshFilter>())
                Object.DestroyImmediate(filter);
            foreach (Collider collider in item.GetComponentsInChildren<Collider>(true))
                Object.DestroyImmediate(collider);
            foreach (LODGroup lod in item.GetComponentsInChildren<LODGroup>(true))
                Object.DestroyImmediate(lod);
        }
    }
}
