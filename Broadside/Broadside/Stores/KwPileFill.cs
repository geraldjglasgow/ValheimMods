using PatchGuard;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// How full an ammunition pile looks, on every peer with a screen: its model's fill steps (<c>fill_0</c> to <c>fill_9</c>,
    /// <see cref="KwPileModel.Fills"/>) shown in order, step i once the pile is (i + 1) / 10 full and at least the first
    /// while anything is in it (<see cref="Shown"/>). Full is every slot a full stack: each stack counts its share of its
    /// item's own stack size. Worked out again only when the pile's inventory changes (its <c>m_onChanged</c>, which the
    /// game calls on the owner as items move and on every other peer as it loads the inventory from the ZDO), never by
    /// polling. Sends and saves nothing. A placement ghost (no inventory) and a dedicated server leave it alone.
    /// </summary>
    public sealed class KwPileFill : MonoBehaviour
    {
        public const int Steps = 10;

        private GameObject?[] fills = new GameObject?[Steps];
        private Inventory? inventory;
        private int shown = -1;

        /// <summary>How many steps a pile so full shows (0 to 1): (i + 1) / 10 reached shows step i, anything in it at least one.</summary>
        public static int Shown(float full, bool any) => any ? Mathf.Clamp(Mathf.FloorToInt(full * Steps + 0.0001f), 1, Steps) : 0;

        /// <summary>How full an inventory is, 0 to 1: its stacks over every slot holding a full stack.</summary>
        public static float Full(Inventory inventory)
        {
            float stacks = 0f;
            foreach (ItemDrop.ItemData item in inventory.GetAllItems())
                stacks += item.m_stack / (float)Mathf.Max(1, item.m_shared.m_maxStackSize);
            return Mathf.Clamp01(stacks / Mathf.Max(1, inventory.GetWidth() * inventory.GetHeight()));
        }

        private void Start()
        {
            Container? box = GetComponent<Container>();
            ZNetView? view = GetComponent<ZNetView>();
            inventory = box != null && view != null && view.GetZDO() != null ? box.GetInventory() : null;
            Transform? look = transform.Find(KwPileModel.LookName);
            if (inventory == null || look == null || !KwIcons.CanRender)
            {
                enabled = false;
                return;
            }
            fills = KwPileModel.Fills(look);
            inventory.m_onChanged += Changed;
            Changed();
        }

        private void OnDestroy()
        {
            if (inventory != null)
                inventory.m_onChanged -= Changed;
        }

        // Called inside the game's inventory code: a throw is reported, never passed on into it.
        private void Changed()
        {
            try
            {
                Show();
            }
            catch (System.Exception e)
            {
                Guard.Report(e, "Broadside pile fill");
            }
        }

        // Only steps whose state changes are touched.
        private void Show()
        {
            if (this == null || inventory == null)
                return;
            int count = Shown(Full(inventory), inventory.NrOfItems() > 0);
            if (count == shown)
                return;
            shown = count;
            for (int i = 0; i < fills.Length; i++)
            {
                GameObject? fill = fills[i];
                if (fill != null && fill.activeSelf != i < count)
                    fill.SetActive(i < count);
            }
        }
    }
}
