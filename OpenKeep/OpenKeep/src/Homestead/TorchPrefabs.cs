using System.Collections.Generic;
using UnityEngine;

namespace OpenKeep.Homestead
{
    /// <summary>
    /// The fires Torch Pieces names, as prefab hashes to compare with a fire's ZDO prefab. The setting is read on every
    /// call; the text is split again only when it or the scene changes. A name is looked up in the scene's prefabs
    /// (exact, then ignoring case, as Build On Wood does) so the hash is the real prefab's; a name without a prefab or
    /// without a <c>Fireplace</c> is warned about once and still kept as written.
    /// </summary>
    public static class TorchPrefabs
    {
        private static string parsedText;
        private static ZNetScene parsedScene;
        private static HashSet<int> listed = new HashSet<int>();

        public static bool IsListed(int prefabHash) => Current().Contains(prefabHash);

        private static HashSet<int> Current()
        {
            string text = TorchSettings.TorchPieces.Value ?? "";
            ZNetScene scene = ZNetScene.instance;
            if (text == parsedText && scene == parsedScene)
                return listed;
            parsedText = text;
            parsedScene = scene;
            listed = Parse(text, scene);
            return listed;
        }

        private static HashSet<int> Parse(string text, ZNetScene scene)
        {
            HashSet<int> hashes = new HashSet<int>();
            foreach (string part in text.Split(','))
            {
                string name = part.Trim();
                if (name.Length > 0)
                    hashes.Add(Resolve(name, scene).GetStableHashCode());
            }
            return hashes;
        }

        /// <summary>The prefab's own name for a listed name; the name as written before the scene exists or when unknown.</summary>
        private static string Resolve(string name, ZNetScene scene)
        {
            if (scene == null)
                return name;
            GameObject prefab = FirePrefabs.Find(name);
            if (prefab == null)
                Plugin.Log.LogWarning($"OpenKeep: Torch Pieces names '{name}', which is not a prefab.");
            else if (prefab.GetComponent<Fireplace>() == null)
                Plugin.Log.LogWarning($"OpenKeep: Torch Pieces names '{name}', which is not a fire.");
            return prefab != null ? prefab.name : name;
        }
    }
}
