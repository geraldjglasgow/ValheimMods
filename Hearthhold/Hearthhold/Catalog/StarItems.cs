using UnityEngine;

namespace Hearthhold
{
    /// <summary>
    /// Makes Hearthhold's items star items through GrindstoneSkills (<see cref="GrindstoneLink.AddStarItem"/>): every
    /// kitchen product and every forage, crop and meat item. Feasts (items with a Feast) and unstackable items never carry
    /// stars; a Piece alone does not make a feast (this game lets ordinary items such as raspberries be placed). Quality also scales an item's size and weight when its shared data sets m_scaleByQuality or
    /// m_scaleWeightByQuality; both are cleared so a starred item looks and weighs the same as a plain one (at quality 1,
    /// the only quality these items have without Hearthhold, both factors do nothing).
    /// </summary>
    public static class StarItems
    {
        private static int registered;

        internal static void RegisterAll()
        {
            int count = 0;
            foreach (GameObject prefab in ObjectDB.instance.m_items)
            {
                if (prefab != null && Wanted(prefab.name) && Register(prefab))
                    count++;
            }
            if (count != registered)
                Hearthhold.Log.LogInfo($"Items that carry stars: {count}.");
            registered = count;
        }

        private static bool Wanted(string name) =>
            Kitchen.IsProduct(name) || Sources.IsForageItem(name) || Sources.IsCropItem(name) || Sources.IsMeatItem(name);

        private static bool Register(GameObject prefab)
        {
            ItemDrop drop = prefab.GetComponent<ItemDrop>();
            ItemDrop.ItemData.SharedData shared = drop?.m_itemData?.m_shared;
            if (shared == null || shared.m_maxStackSize <= 1 || prefab.GetComponent<Feast>() != null)
                return false;
            shared.m_scaleByQuality = 0f;
            shared.m_scaleWeightByQuality = 0f;
            GrindstoneLink.AddStarItem(prefab);
            return true;
        }
    }
}
