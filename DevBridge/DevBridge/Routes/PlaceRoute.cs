using System;
using System.Collections.Generic;
using System.Linq;
using DevBridge.Server;
using DevBridge.Stage;
using UnityEngine;

namespace DevBridge.Routes
{
    /// <summary>
    /// /place, /lineup, /placed, /dress and /clear: bundle assets and still copies of the game's own prefabs set out in
    /// a row in front of the player, on this machine only.
    /// </summary>
    internal static class PlaceRoute
    {
        private const string RowArgs = "&distance=4&new=1&side=right|left|both&gap=0.5(or spacing=)&height=0&sit=1&yaw=0&scale=1";

        internal static void Register(Router router)
        {
            router.Add("/place",
                "/place?asset=<prefab>&bundle=<name>&dress=Creature|Piece|<game prefab>|none&child=&only=&plain=0.1|off\n" +
                "       " + RowArgs + "&at=x,y,z&on=<game prefab>&bone=<bone>&gear=1|0|<items>\n" +
                "                       a still, local copy of a bundle prefab (no AI, physics, colliders or network): the first starts a\n" +
                "                       row `distance` m in front of the player facing them, later ones go beside it (gap between meshes,\n" +
                "                       sit=1 stands the lowest point on the ground); dress puts its placeholder materials into a game\n" +
                "                       material; on= has a game creature wear it (a kit by bone names, or whole on bone=)",
                Place);
            router.Add("/lineup",
                "/lineup?prefabs=Skeleton,Draugr,AxeBronze&gear=1|0|<items>" + RowArgs + "\n" +
                "                       still, local copies of the game's own prefabs beside the row, alternating sides by default;\n" +
                "                       gear=1 shows what a creature carries, gear=Battleaxe,ShieldWood puts those game items on it",
                Lineup);
            router.Add("/placed", "/placed?id=N           everything placed: size, triangles, materials and shaders, dress", List);
            router.Add("/dress",
                "/dress?id=N&dress=Creature|Piece|<game prefab>|none&child=&only=<material text>&plain=0.1|off\n" +
                "                       dress a placed asset again (from its original placeholders); only= limits it to some materials",
                Redress);
            router.Add("/clear", "/clear?id=1,2|kind=asset|game|effect|sound|bundle=<name>   remove placed things (all by default)", Clear);
        }

        private static void Place(BridgeRequest request)
        {
            var spec = new PlaceSpec
            {
                Asset = request.Require("asset"), Bundle = request.Get("bundle"), On = request.Get("on"), Bone = request.Get("bone"),
                Gear = request.Get("gear"), Scale = request.Float("scale", 1f),
            };
            var notes = new Dictionary<string, object>();
            Placement placement = Stager.Asset(spec, Vector3.zero, Quaternion.identity, notes);
            try
            {
                if (request.Has("dress")) Dress.Add(placement, Dress.Spec(request, request.Require("dress")));
                notes["row"] = Arrange(placement, request, "right");
            }
            catch
            {
                Placements.Remove(placement);
                throw;
            }
            request.Json(Reply(placement, notes));
        }

        private static void Lineup(BridgeRequest request)
        {
            List<GameObject> prefabs = request.Require("prefabs").Split(',').Select(s => s.Trim()).Where(s => s.Length > 0)
                .Select(GamePrefabs.Require).ToList();
            string gear = request.Get("gear");
            var placed = new List<Dictionary<string, object>>();
            foreach (GameObject prefab in prefabs)
            {
                var notes = new Dictionary<string, object>();
                Placement placement = Stager.Game(prefab, request.Float("scale", 1f), gear, notes);
                notes["row"] = Arrange(placement, request, "both");
                placed.Add(Reply(placement, notes));
            }
            request.Json(placed);
        }

        /// <summary>Into the row (begun anew with new=1 or when empty), or at an exact at=x,y,z outside it.</summary>
        private static string Arrange(Placement placement, BridgeRequest request, string side)
        {
            Slot slot = Slot.From(request, side);
            if (request.Has("at"))
            {
                Row.PutAt(placement.Root, Fmt.ParseV3(request.Get("at"), "at"), slot);
                return "at";
            }
            if (!Row.Started || request.Flag("new")) Row.Begin(request.Float("distance", 4f));
            return Row.Put(placement.Root, slot);
        }

        private static Dictionary<string, object> Reply(Placement placement, Dictionary<string, object> notes)
        {
            Dictionary<string, object> info = Placements.Describe(placement);
            foreach (KeyValuePair<string, object> note in notes) info[note.Key] = note.Value;
            if (!Row.Started || !(notes["row"] is string place)) return info;
            Dictionary<string, object> row = Row.Describe();
            row["place"] = place;
            info["row"] = row;
            return info;
        }

        private static void List(BridgeRequest request)
        {
            if (request.Has("id")) request.Json(Placements.Describe(Placements.Get(request.Int("id", 0))));
            else request.Json(Placements.All.Select(Placements.Describe).ToList());
        }

        private static void Redress(BridgeRequest request)
        {
            Placement placement = Placements.Pick(request, p => p.Kind == Placement.Asset, "a bundle asset");
            Dress.Add(placement, Dress.Spec(request, request.Get("dress") ?? request.Require("with")));
            request.Json(Placements.Describe(placement));
        }

        private static void Clear(BridgeRequest request)
        {
            string kind = request.Get("kind"), bundle = request.Get("bundle");
            HashSet<int> ids = request.Has("id") ? new HashSet<int>(Placements.Many(request.Get("id")).Select(p => p.Id)) : null;
            int removed = Placements.Clear(p => (ids == null || ids.Contains(p.Id)) &&
                (kind == null || string.Equals(p.Kind, kind, StringComparison.OrdinalIgnoreCase)) &&
                (bundle == null || string.Equals(p.Bundle, bundle, StringComparison.OrdinalIgnoreCase)));
            request.Json(new Dictionary<string, object> { ["removed"] = removed, ["left"] = Placements.All.Count });
        }
    }
}
