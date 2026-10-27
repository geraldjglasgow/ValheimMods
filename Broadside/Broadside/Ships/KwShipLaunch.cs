using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// Launching: a blueprint becomes a ship (<see cref="KwShipPrefab"/>) on the water, where a saved ship's hammer entry
    /// set its ghost (<see cref="KwBpPlace"/>). The ship's ZDO carries its blueprint in its own frame
    /// (<see cref="KwShipData"/>), so every peer builds it the same; it is a networked object like the game's own ships,
    /// which the game moves, syncs and saves. Runs on the building player's game.
    /// </summary>
    public static class KwShipLaunch
    {
        /// <summary>A ship from a blueprint whose frame stands at origin, turned so; null when the game has no ship prefab.</summary>
        public static GameObject? Spawn(KwBlueprint blueprint, Vector3 origin, Quaternion turn)
        {
            GameObject? prefab = ZNetScene.instance != null ? ZNetScene.instance.GetPrefab(KwShipPrefab.Name) : null;
            if (prefab == null)
                return null;
            KwBpHull hull = KwBpHull.Measure(blueprint);
            KwBlueprint own = KwShipData.Rebased(blueprint, hull.Origin);
            GameObject ship = Object.Instantiate(prefab, origin + turn * hull.Origin, turn);
            ship.GetComponent<ZNetView>().GetZDO().Set(KwShipBody.Key, KwShipData.Pack(own));
            KwShipKeys.Launched(ship.GetComponent<ZNetView>().GetZDO(), blueprint.Name);
            return ship;
        }
    }
}
