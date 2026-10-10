using System.Collections.Generic;
using DevBridge.Server;
using DevBridge.Studio;
using UnityEngine;

namespace DevBridge.Routes
{
    /// <summary>
    /// /studio, the page, and the /studio/ calls it makes (also usable from curl): pick a networked object near the
    /// player, list its transforms, move, rotate and scale them, attach effects and play them. All on this machine only:
    /// nothing is saved or sent to other players.
    /// </summary>
    internal static class StudioRoute
    {
        private const string Pad = "\n                       ";

        internal static void Register(Router router)
        {
            router.Add("/studio", "/studio                a page for your browser (or the Open key, F3): pick an object near you, select its" + Pad +
                "parts, move, rotate and scale them, attach effects and play them; or find items by name, kind or biome" + Pad +
                "and turn them in a viewer; this machine only",
                _ => throw new BridgeException("/studio is a page: open it in a browser"));
            HttpBridge.ServeDirect("/studio", StudioPage.Answer);
            RegisterReads(router);
            RegisterEdits(router);
            StudioItemsRoute.Register(router);
        }

        private static void RegisterReads(Router router)
        {
            router.Add("/studio/targets", "/studio/targets?radius=30&filter=<prefab text>" + Pad +
                "the object under the crosshair and the networked objects near the player, as target= ids", Targets);
            router.Add("/studio/tree", "/studio/tree?target=<zdo id>|hover" + Pad +
                "its transforms by path (child indexes, \"\" the root): local pose, position in the root's space, what each holds", Tree);
            router.Add("/studio/sources", "/studio/sources?filter=text" + Pad +
                "effects to attach: workshop bundles' (bundle:<name>/<prefab>, opened in the Workshop tab), mods' effect templates" + Pad +
                "(template:<scene path>) and the game's (game:<prefab>)", Sources);
        }

        private static void RegisterEdits(Router router)
        {
            router.Add("/studio/set", "/studio/set?target=&path=0/1&position=x,y,z&rotation=x,y,z&scale=x,y,z" + Pad +
                "set a node's local pose (any of the three)", Set);
            router.Add("/studio/reset", "/studio/reset?target=&path=   put back every node moved at or under path (the whole target without)", Reset);
            router.Add("/studio/play", "/studio/play?target=&path=&state=<animator state>" + Pad +
                "play a node's particles, animators, animations and sounds from the start", Play);
            router.Add("/studio/attach", "/studio/attach?target=&path=&source=<from /studio/sources>&position=x,y,z" + Pad +
                "hang a local copy of an effect on a node, kept until /studio/detach?target=&path= removes it", Attach);
            router.Add("/studio/detach", "/studio/detach?target=&path=   remove an attached effect", Detach);
        }

        private static void Targets(BridgeRequest request) =>
            request.Json(StudioTargets.List(request.Float("radius", 30f), request.Get("filter")));

        private static void Tree(BridgeRequest request)
        {
            GameObject root = StudioTargets.Resolve(request.Require("target"));
            request.Json(new Dictionary<string, object>
            {
                ["target"] = request.Get("target"),
                ["prefab"] = Utils.GetPrefabName(root),
                ["nodes"] = StudioNodes.Tree(root),
            });
        }

        private static void Sources(BridgeRequest request) => request.Json(StudioSources.List(request.Get("filter")));

        private static void Set(BridgeRequest request)
        {
            GameObject root = StudioTargets.Resolve(request.Require("target"));
            string path = request.Get("path", "");
            Transform node = StudioNodes.At(root, path);
            StudioMoves.Set(node, Vector(request, "position"), Vector(request, "rotation"), Vector(request, "scale"));
            request.Json(StudioNodes.Describe(root.transform, node, path));
        }

        private static void Reset(BridgeRequest request)
        {
            GameObject root = StudioTargets.Resolve(request.Require("target"));
            int count = StudioMoves.Reset(StudioNodes.At(root, request.Get("path", "")));
            request.Json(new Dictionary<string, object> { ["reset"] = count });
        }

        private static void Play(BridgeRequest request)
        {
            GameObject root = StudioTargets.Resolve(request.Require("target"));
            Transform node = StudioNodes.At(root, request.Get("path", ""));
            request.Json(new Dictionary<string, object> { ["played"] = StudioPlayer.Play(node, request.Get("state")) });
        }

        private static void Attach(BridgeRequest request)
        {
            GameObject root = StudioTargets.Resolve(request.Require("target"));
            string path = request.Get("path", "");
            Transform parent = StudioNodes.At(root, path);
            Transform copy = StudioAttach.Attach(parent, request.Require("source"), Vector(request, "position") ?? Vector3.zero);
            string child = (path.Length == 0 ? "" : path + "/") + copy.GetSiblingIndex();
            request.Json(StudioNodes.Describe(root.transform, copy, child));
        }

        private static void Detach(BridgeRequest request)
        {
            GameObject root = StudioTargets.Resolve(request.Require("target"));
            StudioAttach.Detach(StudioNodes.At(root, request.Require("path")));
            request.Json(new Dictionary<string, object> { ["removed"] = request.Get("path") });
        }

        private static Vector3? Vector(BridgeRequest request, string name) =>
            request.Has(name) ? Fmt.ParseV3(request.Get(name), name) : (Vector3?)null;
    }
}
