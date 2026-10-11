using System.Collections.Generic;
using UnityEngine;

namespace EliteCreaturesPack.Custom.Look
{
    /// <summary>
    /// On a creature (and its corpse) whose definition tints the items it carries. The game draws a creature's items by
    /// attaching a copy of each item's model to it (<c>VisEquipment.AttachItem</c> for what it holds or carries on its
    /// back, its helmet, hair and beard; <c>AttachArmor</c> for armour, capes and utility items), on every peer, whenever
    /// what it wears changes. While this is loaded its VisEquipment is in <see cref="ItemTints"/>, so each such copy is
    /// drawn in the tint as it is attached (<see cref="ItemTintAttachPatch"/>): the gear its definition gives it, the gear
    /// its base carries, and a corpse's gear alike. Hair and beards are body, not items, and keep their colours.
    /// </summary>
    public sealed class ItemTint : MonoBehaviour
    {
        /// <summary>The colour, set at build; the prefab instances carry it.</summary>
        public Color m_tint = Color.white;

        private VisEquipment? vis;

        private void Awake()
        {
            vis = GetComponent<VisEquipment>();
            if (vis != null)
            {
                ItemTints.Add(vis, m_tint);
            }
        }

        private void OnDestroy()
        {
            if (vis != null)
            {
                ItemTints.Remove(vis);
            }
        }
    }

    /// <summary>
    /// The loaded tinted creatures' VisEquipments and their tints, filled and emptied by <see cref="ItemTint"/>'s Awake and
    /// OnDestroy, so it never holds a destroyed one. The attach patches ask it first: one count test while none is
    /// loaded, one dictionary lookup otherwise, and only when an item is attached, never per frame.
    /// </summary>
    internal static class ItemTints
    {
        private static readonly Dictionary<VisEquipment, Color> live = new Dictionary<VisEquipment, Color>();

        public static bool Empty => live.Count == 0;

        public static void Add(VisEquipment vis, Color tint) => live[vis] = tint;

        public static void Remove(VisEquipment vis) => live.Remove(vis);

        /// <summary>An item's model just attached to <paramref name="vis"/>: drawn in its tint, unless it is hair or a beard.</summary>
        public static void Attached(VisEquipment vis, int itemHash, GameObject? item)
        {
            if (item == null || Drawing.Headless || !live.TryGetValue(vis, out Color tint) || IsHair(itemHash))
            {
                return;
            }
            Paint(item, tint);
        }

        /// <summary>Armour pieces just attached to <paramref name="vis"/>.</summary>
        public static void Attached(VisEquipment vis, List<GameObject>? items)
        {
            if (items == null || Drawing.Headless || !live.TryGetValue(vis, out Color tint))
            {
                return;
            }
            foreach (GameObject item in items)
            {
                if (item != null)
                {
                    Paint(item, tint);
                }
            }
        }

        private static void Paint(GameObject item, Color tint)
        {
            foreach (Renderer renderer in item.GetComponentsInChildren<Renderer>(true))
            {
                if (renderer is ParticleSystemRenderer || renderer is LineRenderer || renderer is TrailRenderer)
                {
                    continue; // a torch's flame, an enchanted glow: effects of the item, not the item
                }
                Material[] materials = renderer.sharedMaterials;
                for (int i = 0; i < materials.Length; i++)
                {
                    materials[i] = materials[i] != null ? MaterialRecipes.Dressed(materials[i], tint, null, null) : materials[i];
                }
                renderer.sharedMaterials = materials;
            }
        }

        private static bool IsHair(int itemHash)
        {
            GameObject? prefab = ObjectDB.instance != null ? ObjectDB.instance.GetItemPrefab(itemHash) : null;
            ItemDrop? drop = prefab != null ? prefab.GetComponent<ItemDrop>() : null;
            return drop != null && drop.m_itemData.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Customization;
        }
    }
}
