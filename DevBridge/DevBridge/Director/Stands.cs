using DevBridge.Server;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace DevBridge.Director
{
    /// <summary>
    /// A set dressed with items: {"stand": spot, "item": "ECP_BoneSword"} hangs the item on the item stand nearest the
    /// spot (within "radius", default 1.5 m) as a player would, kept in the stand's ZDO, so it stays for later takes and
    /// every player sees it; "item": "" takes it down.
    /// </summary>
    internal static class Stands
    {
        internal static void Cue(JObject s, ShotFrame frame)
        {
            Vector3 at = Spot.Parse(s["stand"], "stand")?.Resolve(frame) ?? throw new BridgeException("stand needs a spot");
            ItemStand stand = Nearest(at, s.Value<float?>("radius") ?? 1.5f) ?? throw new BridgeException($"no item stand within reach of {at}");
            string item = s.Value<string>("item") ?? "";
            if (item.Length > 0 && !ObjectDB.instance.GetItemPrefab(item)) throw new BridgeException($"no item {item}");
            if (!stand.m_nview.IsOwner()) stand.m_nview.ClaimOwnership();
            if (item.Length == 0) stand.m_nview.GetZDO().Set(ZDOVars.s_item, 0);
            stand.SetVisualItem(item.Length == 0 ? 0 : item.GetStableHashCode(), 0, s.Value<int?>("quality") ?? 1, stand.GetOrientation());
        }

        private static ItemStand Nearest(Vector3 at, float radius)
        {
            ItemStand best = null;
            float bestDistance = radius;
            foreach (ItemStand stand in Object.FindObjectsByType<ItemStand>(FindObjectsSortMode.None))
            {
                float distance = Vector3.Distance(stand.transform.position, at);
                if (distance > bestDistance || !stand.m_nview || !stand.m_nview.IsValid()) continue;
                (best, bestDistance) = (stand, distance);
            }
            return best;
        }
    }
}
