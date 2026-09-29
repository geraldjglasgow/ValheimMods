using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using DevBridge.Server;
using UnityEngine;

namespace DevBridge.Stage
{
    /// <summary>Everything on the stage, by id. Entries whose object is gone (a finished effect, a logout) drop out.</summary>
    internal static class Placements
    {
        private static readonly List<Placement> Entries = new List<Placement>();
        private static int nextId = 1;

        internal static IReadOnlyList<Placement> All
        {
            get
            {
                Entries.RemoveAll(p => !p.Alive);
                return Entries;
            }
        }

        internal static Placement Add(string kind, string name, string bundle, GameObject root)
        {
            var placement = new Placement { Id = nextId++, Kind = kind, Name = name, Bundle = bundle, Root = root };
            placement.KeepOriginals(root.GetComponentsInChildren<Renderer>(true));
            Entries.Add(placement);
            return placement;
        }

        /// <summary>Back on the list (a placement made again after its bundle was reloaded keeps its entry and id).</summary>
        internal static void Keep(Placement placement)
        {
            if (!Entries.Contains(placement)) Entries.Add(placement);
        }

        internal static Placement Get(int id) =>
            All.FirstOrDefault(p => p.Id == id) ?? throw new BridgeException($"nothing placed with id {id} (list them with /placed)");

        /// <summary>By id=, or the newest placement that has what `has` asks for.</summary>
        internal static Placement Pick(BridgeRequest request, Func<Placement, bool> has, string what)
        {
            if (request.Has("id")) return Get(request.Int("id", 0));
            return All.LastOrDefault(has) ?? throw new BridgeException($"nothing placed has {what}; give id= (list them with /placed)");
        }

        /// <summary>The ids in "3,5,8".</summary>
        internal static List<Placement> Many(string ids) =>
            ids.Split(',').Select(s => s.Trim()).Where(s => s.Length > 0)
                .Select(s => int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out int id) ? Get(id) : throw new BridgeException($"'{s}' is not an id"))
                .ToList();

        internal static void Remove(Placement placement)
        {
            placement.Destroy();
            Entries.Remove(placement);
            if (Entries.Count == 0) Row.Reset();
        }

        internal static int Clear(Func<Placement, bool> which)
        {
            List<Placement> doomed = All.Where(which).ToList();
            foreach (Placement placement in doomed) Remove(placement);
            return doomed.Count;
        }

        internal static Dictionary<string, object> Describe(Placement placement)
        {
            Transform root = placement.Root.transform;
            var info = new Dictionary<string, object>
            {
                ["id"] = placement.Id, ["kind"] = placement.Kind, ["name"] = placement.Name, ["bundle"] = placement.Bundle,
                ["position"] = Fmt.V3(root.position), ["yaw"] = Fmt.R(root.eulerAngles.y), ["scale"] = Fmt.R(root.localScale.x),
                ["size"] = Fmt.V3(AssetInfo.Bounds(placement.Root).size), ["parts"] = AssetInfo.Parts(placement.Root),
                ["materials"] = AssetInfo.Materials(placement.Root),
            };
            if (placement.Spec?.On != null) info["on"] = placement.Spec.On + (placement.Spec.Bone != null ? " at " + placement.Spec.Bone : "");
            if (placement.Dresses.Count > 0)
                info["dress"] = placement.Dresses.Select(d => $"{d.With}{(d.Child != null ? "/" + d.Child : "")}{(d.Only != null ? " on " + d.Only : "")}{(d.Gloss.HasValue ? $" plain {d.Gloss:0.##}" : "")}").ToList();
            if (placement.SwapSpec != null) info["swap"] = placement.SwapSpec;
            return info;
        }

        /// <summary>The box round the placements' drawn meshes; false when there is nothing to frame.</summary>
        internal static bool Bounds(IEnumerable<Placement> placements, out Bounds box)
        {
            box = default;
            bool any = false;
            foreach (Placement placement in placements.Where(p => p.Alive && p.Kind != Placement.Sound))
            {
                Bounds one = AssetInfo.Bounds(placement.Root);
                if (!any) box = one;
                else box.Encapsulate(one);
                any = true;
            }
            return any;
        }
    }
}
