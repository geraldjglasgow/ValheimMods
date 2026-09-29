using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using DevBridge.Server;
using UnityEngine;

namespace DevBridge.World
{
    /// <summary>Finds networked objects: by ZDO id, under the crosshair, or nearest to a point.</summary>
    internal static class ZdoLookup
    {
        internal static ZDO ById(string id)
        {
            string[] parts = id.Split(':');
            if (parts.Length != 2 || !long.TryParse(parts[0], out long user) || !uint.TryParse(parts[1], out uint number))
                throw new BridgeException($"a ZDO id looks like 123456789:42, not {id}");
            ZDOMan manager = ZDOMan.instance ?? throw new BridgeException("no world loaded");
            return manager.GetZDO(new ZDOID(user, number)) ?? throw new BridgeException($"no ZDO {id} (unloaded or destroyed)");
        }

        internal static ZDO OfHover()
        {
            Player player = Player.m_localPlayer;
            GameObject hover = player ? player.GetHoverObject() : null;
            ZNetView view = hover ? hover.GetComponentInParent<ZNetView>() : null;
            if (!view || !view.IsValid()) throw new BridgeException("the crosshair is not on a networked object");
            return view.GetZDO();
        }

        /// <summary>Loaded networked objects within radius of the centre, nearest first; the local player is left out.</summary>
        internal static List<KeyValuePair<float, ZNetView>> Near(Vector3 centre, float radius, string filter)
        {
            ZNetScene scene = ZNetScene.instance;
            if (!scene) throw new BridgeException("no world loaded");
            ZNetView self = Player.m_localPlayer ? Player.m_localPlayer.m_nview : null;
            var found = new List<KeyValuePair<float, ZNetView>>();
            foreach (ZNetView view in scene.m_instances.Values)
            {
                if (!view || !view.IsValid() || view == self || !Matches(view, filter)) continue;
                float distance = Vector3.Distance(centre, view.transform.position);
                if (distance <= radius) found.Add(new KeyValuePair<float, ZNetView>(distance, view));
            }
            return found.OrderBy(pair => pair.Key).ToList();
        }

        internal static Vector3 Centre(string at)
        {
            if (at != null) return Fmt.ParseV3(at, "at");
            Player player = Player.m_localPlayer;
            return player ? player.transform.position : throw new BridgeException("no local player; give at=x,y,z");
        }

        private static bool Matches(ZNetView view, string filter) =>
            filter == null || Utils.GetPrefabName(view.gameObject).IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0;
    }
}
