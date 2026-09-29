using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using DevBridge.Server;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DevBridge.Ui
{
    /// <summary>
    /// Slash-separated paths to GameObjects, inactive ones included, across every loaded scene and DontDestroyOnLoad.
    /// A name shared by siblings takes an index: Slot[2] is the third sibling named Slot.
    /// </summary>
    internal static class ScenePaths
    {
        private static readonly Regex Indexed = new Regex(@"^(.*)\[(\d+)\]$");

        internal static IEnumerable<Transform> Roots()
        {
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene scene = SceneManager.GetSceneAt(i);
                if (!scene.isLoaded) continue;
                foreach (GameObject root in scene.GetRootGameObjects()) yield return root.transform;
            }
            Scene persistent = DevBridgePlugin.Instance.gameObject.scene;
            if (persistent.name != "DontDestroyOnLoad") yield break;
            foreach (GameObject root in persistent.GetRootGameObjects()) yield return root.transform;
        }

        internal static IEnumerable<Transform> Children(Transform parent)
        {
            for (int i = 0; i < parent.childCount; i++) yield return parent.GetChild(i);
        }

        internal static GameObject Find(string path)
        {
            string[] parts = path.Trim().Trim('/').Split('/');
            Transform current = Pick(Roots(), parts[0]);
            for (int i = 1; current && i < parts.Length; i++) current = Pick(Children(current), parts[i]);
            return current ? current.gameObject : null;
        }

        internal static GameObject Require(string path) =>
            Find(path) ?? throw new BridgeException($"nothing at path {path} (list the tree with /ui or search with /find)");

        private static Transform Pick(IEnumerable<Transform> candidates, string part)
        {
            Match match = Indexed.Match(part);
            string name = match.Success ? match.Groups[1].Value : part;
            int index = match.Success ? int.Parse(match.Groups[2].Value) : 0;
            return candidates.Where(t => t.name == name).Skip(index).FirstOrDefault();
        }

        internal static string PathOf(Transform transform)
        {
            var parts = new List<string>();
            for (Transform t = transform; t; t = t.parent) parts.Add(Label(t));
            parts.Reverse();
            return string.Join("/", parts);
        }

        private static string Label(Transform t)
        {
            IEnumerable<Transform> siblings = t.parent ? Children(t.parent) : Roots();
            int index = siblings.TakeWhile(s => s != t).Count(s => s.name == t.name);
            return index == 0 ? t.name : $"{t.name}[{index}]";
        }
    }
}
