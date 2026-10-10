using System.Collections.Generic;
using System.Linq;
using DevBridge.Server;
using DevBridge.World;
using UnityEngine;

namespace DevBridge.Studio
{
    /// <summary>
    /// The objects the studio edits: the networked objects loaded near the player, or the one under the crosshair, each
    /// addressed by its ZDO id (target=123456789:42, or target=hover).
    /// </summary>
    internal static class StudioTargets
    {
        private const int Most = 80;

        internal static GameObject Resolve(string target)
        {
            ZDO zdo = target == "hover" ? ZdoLookup.OfHover() : ZdoLookup.ById(target);
            ZNetView view = ZNetScene.instance ? ZNetScene.instance.FindInstance(zdo) : null;
            return view ? view.gameObject : throw new BridgeException($"{target} is not loaded on this machine");
        }

        /// <summary>The crosshair's object (or null) and the objects within radius, nearest first.</summary>
        internal static Dictionary<string, object> List(float radius, string filter)
        {
            List<Dictionary<string, object>> near = ZdoLookup.Near(ZdoLookup.Centre(null), radius, filter)
                .Take(Most).Select(pair => Line(pair.Value, pair.Key)).ToList();
            return new Dictionary<string, object> { ["hover"] = Hover(), ["nearby"] = near };
        }

        private static Dictionary<string, object> Hover()
        {
            try
            {
                ZNetView view = ZNetScene.instance.FindInstance(ZdoLookup.OfHover());
                return view ? Line(view, Vector3.Distance(ZdoLookup.Centre(null), view.transform.position)) : null;
            }
            catch (BridgeException)
            {
                return null;
            }
        }

        private static Dictionary<string, object> Line(ZNetView view, float distance) => new Dictionary<string, object>
        {
            ["target"] = view.GetZDO().m_uid.ToString(),
            ["prefab"] = Utils.GetPrefabName(view.gameObject),
            ["name"] = Name(view.gameObject),
            ["distance"] = Fmt.R(distance),
        };

        // What the game calls it: a piece's, creature's or item's name, localized; null for anything else.
        private static string Name(GameObject gameObject)
        {
            string token = gameObject.GetComponent<Piece>()?.m_name ?? gameObject.GetComponent<Character>()?.m_name
                ?? gameObject.GetComponent<ItemDrop>()?.m_itemData?.m_shared?.m_name;
            return token == null || Localization.instance == null ? token : Localization.instance.Localize(token);
        }
    }
}
