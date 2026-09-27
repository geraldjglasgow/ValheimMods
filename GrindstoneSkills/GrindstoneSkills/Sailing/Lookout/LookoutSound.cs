using UnityEngine;

namespace GrindstoneSkills
{
    /// <summary>
    /// The lookout's sound: the Wishbone's ping (the effect list of its SE_Finder), played at the ship. Every client
    /// handles the pulse itself, so each plays a local copy: instantiated with network views disabled, the way the game
    /// makes its build ghost, so no ZDO is made and nobody gets a second, networked copy. A copy is removed after a few
    /// seconds whatever its own timer does. Without a Wishbone in the game the pulse is silent.
    /// </summary>
    internal static class LookoutSound
    {
        private const float Life = 8f;

        private static EffectList ping;

        public static void Play(Vector3 position)
        {
            EffectList effects = Ping();
            if (effects == null || effects.m_effectPrefabs == null)
                return;
            foreach (EffectList.EffectData data in effects.m_effectPrefabs)
            {
                if (data != null && data.m_enabled && data.m_prefab != null)
                    Object.Destroy(LocalCopy(data.m_prefab, position), Life);
            }
        }

        private static GameObject LocalCopy(GameObject prefab, Vector3 position)
        {
            bool was = ZNetView.m_forceDisableInit;
            ZNetView.m_forceDisableInit = true;
            try
            {
                return Object.Instantiate(prefab, position, Quaternion.identity);
            }
            finally
            {
                ZNetView.m_forceDisableInit = was;
            }
        }

        /// <summary>The Wishbone's near ping, found once the item database is up.</summary>
        private static EffectList Ping()
        {
            if (ping != null || ObjectDB.instance == null)
                return ping;
            GameObject wishbone = ObjectDB.instance.GetItemPrefab("Wishbone");
            ItemDrop item = wishbone != null ? wishbone.GetComponent<ItemDrop>() : null;
            SE_Finder finder = item != null ? item.m_itemData.m_shared.m_equipStatusEffect as SE_Finder : null;
            ping = finder != null ? finder.m_pingEffectNear : null;
            return ping;
        }
    }
}
