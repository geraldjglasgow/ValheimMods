using UnityEngine;

namespace OpenKeep.Salvage
{
    /// <summary>One row of the salvage list: an inventory stack and its list element (the game's recipe element prefab).</summary>
    public sealed class SalvageRow
    {
        public SalvageRow(ItemDrop.ItemData item, GameObject element)
        {
            Item = item;
            Element = element;
        }

        public ItemDrop.ItemData Item { get; }

        public GameObject Element { get; }

        public void SetSelected(bool selected)
        {
            if (Element == null)
                return;
            Transform marker = Element.transform.Find("selected");
            if (marker != null && marker.gameObject.activeSelf != selected)
                marker.gameObject.SetActive(selected);
        }
    }
}
