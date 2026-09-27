using System.Collections.Generic;
using EarthWright.Core;
using UnityEngine;

namespace EarthWright.Terrain
{
    /// <summary>
    /// Sender side: runs the hooks and guards, finds every terrain compiler an edit touches (creating missing ones, as
    /// the game does for its own terrain ops) and sends each the edit through the compiler's own network view, which
    /// delivers it to the compiler's owner. A privileged edit goes through the server instead (<see cref="ServerRelay"/>).
    /// </summary>
    public static class Dispatcher
    {
        /// <summary>Checks and sends an edit. False when a guard refused it (the reason is shown unless quiet).</summary>
        public static bool Submit(TerrainEdit edit, bool quiet = false)
        {
            if (edit == null)
                return false;
            EditEvents.RaiseBuilding(edit);
            string reason = EditGuards.CheckSender(edit);
            if (!string.IsNullOrEmpty(reason))
            {
                if (!quiet)
                    Messages.Center(reason);
                return false;
            }
            SendChecked(edit);
            return true;
        }

        /// <summary>
        /// Runs the building hook and the sender guards without sending: null when the edit may be sent, else the reason.
        /// For work that charges between the check and the send (special entries): Check, charge, then <see cref="SendChecked"/>.
        /// </summary>
        public static string Check(TerrainEdit edit)
        {
            if (edit == null)
                return "";
            EditEvents.RaiseBuilding(edit);
            return EditGuards.CheckSender(edit);
        }

        /// <summary>Sends an edit whose building hook and sender guards already ran (the placement hook checks before the game charges).</summary>
        public static void SendChecked(TerrainEdit edit)
        {
            EditEvents.RaiseBeforeSend(edit);
            Route(edit);
            EditEvents.RaiseSent(edit);
        }

        private static void Route(TerrainEdit edit)
        {
            List<TerrainComp> comps = Compilers(edit);
            if (GeneralSettings.DebugLog.Value)
                Plugin.Log.LogInfo($"Edit {edit.Source} ({edit.Kind}, {edit.Flags}) to {comps.Count} terrain compilers");
            if (edit.Has(EditFlags.Privileged))
            {
                ServerRelay.Send(edit, comps);
                return;
            }
            foreach (TerrainComp comp in comps)
            {
                TerrainEdit part = PartFor(comp, edit);
                if (part != null)
                    comp.m_nview.InvokeRPC(OwnerHandler.RpcName, EditWire.Write(part));
            }
        }

        /// <summary>The compilers of every heightmap the edit touches, created where missing.</summary>
        public static List<TerrainComp> Compilers(TerrainEdit edit)
        {
            List<TerrainComp> comps = new List<TerrainComp>();
            foreach (Heightmap map in Maps(edit))
            {
                TerrainComp comp = map.GetAndCreateTerrainCompiler();
                if (comp != null && comp.m_nview != null && comp.m_nview.IsValid())
                    comps.Add(comp);
            }
            return comps;
        }

        /// <summary>The loaded heightmaps an edit touches: a restore's own, the ones a target vertex lies on, or those under a stroke.</summary>
        private static List<Heightmap> Maps(TerrainEdit edit)
        {
            List<Heightmap> maps = new List<Heightmap>();
            if (edit.Kind == EditKind.Vertices && edit.Vertices.Mode == VertexMode.Restore)
            {
                Heightmap map = Heightmap.FindHeightmap(edit.Vertices.CompPosition);
                if (map != null)
                    maps.Add(map);
            }
            else if (edit.Kind == EditKind.Vertices)
            {
                TargetMaps(edit.Vertices, maps);
            }
            else
            {
                edit.GetArea(out Vector3 center, out float radius);
                Heightmap.FindHeightmap(center, radius, maps);
            }
            return maps;
        }

        /// <summary>
        /// Only the heightmaps a target vertex lies on (a vertex on a shared edge counts for both), so a long diagonal
        /// ramp does not create empty compilers in zones its bounding square merely covers.
        /// </summary>
        private static void TargetMaps(VertexSet set, List<Heightmap> maps)
        {
            List<Heightmap> found = new List<Heightmap>();
            foreach (TargetVertex v in set.Targets)
            {
                found.Clear();
                Heightmap.FindHeightmap(new Vector3(v.X, 0f, v.Z), 0f, found);
                foreach (Heightmap map in found)
                {
                    if (!maps.Contains(map))
                        maps.Add(map);
                }
            }
        }

        /// <summary>The edit as it goes to one compiler: target vertices outside its heightmap are left out; null if none remain.</summary>
        public static TerrainEdit PartFor(TerrainComp comp, TerrainEdit edit)
        {
            if (edit.Kind != EditKind.Vertices || edit.Vertices.Mode != VertexMode.Targets)
                return edit;
            VertexSet part = new VertexSet { Mode = VertexMode.Targets };
            foreach (TargetVertex v in edit.Vertices.Targets)
            {
                if (comp.m_hmap.IsPointInside(new Vector3(v.X, 0f, v.Z)))
                    part.Targets.Add(v);
            }
            if (part.Targets.Count == 0)
                return null;
            return new TerrainEdit
            {
                Kind = EditKind.Vertices, Flags = edit.Flags, SenderPlayer = edit.SenderPlayer,
                SenderPeer = edit.SenderPeer, Source = edit.Source, Vertices = part,
            };
        }
    }
}
