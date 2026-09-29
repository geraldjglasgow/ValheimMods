using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace DevBridge.Stage
{
    /// <summary>Makes the stage's objects: bundle assets (alone or worn by a game creature) and still copies of the game's own.</summary>
    internal static class Stager
    {
        /// <summary>A bundle asset as a still copy, worn by a game creature when the spec says so; dressed by the caller.</summary>
        internal static Placement Asset(PlaceSpec spec, Vector3 position, Quaternion rotation, Dictionary<string, object> notes)
        {
            GameObject root = Make(spec, position, rotation, notes, out Renderer[] own);
            Placement placement = Placements.Add(Placement.Asset, spec.Asset, spec.Bundle, root);
            placement.KeepOriginals(own);
            placement.Spec = spec;
            return placement;
        }

        /// <summary>
        /// After its bundle is reloaded, an asset is made again at the same pivot from its spec, keeping its id and its
        /// dresses (a new version's size does not move it).
        /// </summary>
        internal static void Rebuild(Placement placement, Vector3 position, Quaternion rotation)
        {
            placement.Root = Make(placement.Spec, position, rotation, new Dictionary<string, object>(), out Renderer[] own);
            placement.KeepOriginals(own);
            Placements.Keep(placement);
            Dress.Apply(placement);
        }

        private static GameObject Make(PlaceSpec spec, Vector3 position, Quaternion rotation, Dictionary<string, object> notes, out Renderer[] own)
        {
            GameObject prefab = Bundles.Asset<GameObject>(spec.Asset, spec.Bundle, out LoadedBundle from);
            (spec.Asset, spec.Bundle) = (prefab.name, from.Name);
            GameObject asset = LocalCopy.Still(prefab, position, rotation);
            own = asset.GetComponentsInChildren<Renderer>(true);
            GameObject root = spec.On == null ? asset : Body(asset, spec, notes);
            root.transform.localScale *= spec.Scale;
            return root;
        }

        /// <summary>The game creature's copy wearing the asset; the asset goes if it cannot be worn.</summary>
        private static GameObject Body(GameObject asset, PlaceSpec spec, Dictionary<string, object> notes)
        {
            GameObject body = null;
            try
            {
                body = GameCopy(GamePrefabs.Require(spec.On), asset.transform.position, asset.transform.rotation, spec.Gear, notes);
                notes["worn"] = Wear.Hang(asset, body, spec.Bone);
                return body;
            }
            catch
            {
                Object.Destroy(asset);
                if (body) Object.Destroy(body);
                throw;
            }
        }

        /// <summary>A still copy of a game prefab, sized as the game sizes its creatures, carrying its gear when asked.</summary>
        internal static GameObject GameCopy(GameObject prefab, Vector3 position, Quaternion rotation, string gear, Dictionary<string, object> notes)
        {
            GameObject copy = LocalCopy.Still(prefab, position, rotation);
            GameSize(copy, prefab);
            if (copy.GetComponent<VisEquipment>()) notes["gear"] = Gear.Wear(copy, prefab, gear);
            return copy;
        }

        internal static Placement Game(GameObject prefab, float scale, string gear, Dictionary<string, object> notes)
        {
            GameObject copy = GameCopy(prefab, Vector3.zero, Quaternion.identity, gear, notes);
            copy.transform.localScale *= scale;
            return Placements.Add(Placement.Game, prefab.name, null, copy);
        }

        // Character.Awake grows every non-player creature by the world's enemy size (outside dungeons: the stage is where
        // the player is) and world level modifiers.
        private static void GameSize(GameObject copy, GameObject prefab)
        {
            Character character = prefab.GetComponent<Character>();
            if (!character || character is Player) return;
            float factor = 1f;
            bool inside = Player.m_localPlayer && Character.InInterior(Player.m_localPlayer.transform);
            if (global::Game.m_enemySpeedSize != 1f && !inside) factor *= global::Game.m_enemySpeedSize;
            if (global::Game.m_worldLevel > 0 && global::Game.instance) factor *= 1f + global::Game.m_worldLevel * global::Game.instance.m_worldLevelEnemyMoveSpeedMultiplier;
            copy.transform.localScale *= factor;
        }
    }
}
