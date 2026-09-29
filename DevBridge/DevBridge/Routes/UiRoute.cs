using System;
using System.Linq;
using DevBridge.Server;
using DevBridge.Ui;
using UnityEngine;

namespace DevBridge.Routes
{
    /// <summary>/ui and /find: read the live GameObject hierarchy, UI added by mods included.</summary>
    internal static class UiRoute
    {
        internal static void Register(Router router)
        {
            router.Add("/ui",
                "/ui?path=<path>&depth=4&all=1\n" +
                "                       indented tree: name [components] \"text\" img:sprite @x,y wxh (screen pixels from the top-left);\n" +
                "                       without path, every visible root canvas to depth 3; all=1 includes inactive children",
                Tree);
            router.Add("/find",
                "/find?text=&name=&component=&scope=ui|all&all=1&limit=40\n" +
                "                       paths of objects whose text, name or component type contains the given words (all must match)",
                Find);
        }

        private static void Tree(BridgeRequest request)
        {
            string path = request.Get("path");
            var roots = path == null ? UiTree.ActiveCanvases().ToList() : new[] { ScenePaths.Require(path).transform }.ToList();
            request.Text(UiTree.Dump(roots, request.Int("depth", path == null ? 3 : 4), request.Flag("all")));
        }

        private static void Find(BridgeRequest request)
        {
            string text = request.Get("text"), name = request.Get("name"), component = request.Get("component");
            if (text == null && name == null && component == null) throw new BridgeException("give text=, name= or component=");
            Func<Transform, bool> match = t =>
                (name == null || Contains(t.name, name)) &&
                (component == null || t.GetComponents<Component>().Any(c => c && Contains(c.GetType().Name, component))) &&
                (text == null || Contains(UiDescribe.TextOf(t.gameObject), text));
            bool uiOnly = request.Get("scope", "ui") != "all";
            request.Text(UiTree.Find(match, uiOnly, request.Flag("all"), request.Int("limit", 40)));
        }

        private static bool Contains(string haystack, string needle) =>
            haystack != null && haystack.IndexOf(needle, StringComparison.OrdinalIgnoreCase) >= 0;
    }
}
