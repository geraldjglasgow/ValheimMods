using System.Collections.Generic;
using UnityEngine;

namespace OpenKeep.Blueprints.Sites
{
    /// <summary>
    /// Hands a construction site's store back: every material dropped beside its post in full stacks, as networked items
    /// made on the machine that owns the site (so they are there for everyone). Items this game does not know are left
    /// out and logged.
    /// </summary>
    public static class SiteStore
    {
        /// <summary>Drops the store's items around the point; returns how many items were dropped.</summary>
        public static int Drop(Vector3 at, Dictionary<string, int> store)
        {
            int dropped = 0;
            foreach (KeyValuePair<string, int> item in store)
            {
                GameObject prefab = ObjectDB.instance != null ? ObjectDB.instance.GetItemPrefab(item.Key) : null;
                if (prefab == null || prefab.GetComponent<ItemDrop>() == null)
                {
                    Plugin.Log.LogWarning($"OpenKeep: a site's {item.Value} {item.Key} cannot be handed back: this game has no such item");
                    continue;
                }
                dropped += DropStacks(prefab, item.Value, at);
            }
            return dropped;
        }

        private static int DropStacks(GameObject prefab, int amount, Vector3 at)
        {
            int stack = Mathf.Max(1, prefab.GetComponent<ItemDrop>().m_itemData.m_shared.m_maxStackSize);
            int dropped = 0;
            for (int left = Mathf.Min(amount, 100000); left > 0; left -= stack)
            {
                int n = Mathf.Min(stack, left);
                Vector2 spread = Random.insideUnitCircle * 0.8f;
                Vector3 position = at + new Vector3(spread.x, 0.6f, spread.y);
                ItemDrop drop = Object.Instantiate(prefab, position, Quaternion.Euler(0f, Random.Range(0f, 360f), 0f)).GetComponent<ItemDrop>();
                drop.SetStack(n);
                dropped += n;
            }
            return dropped;
        }
    }
}
