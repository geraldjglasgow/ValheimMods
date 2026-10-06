using System.Collections.Generic;
using EarthWright.Core;
using UnityEngine;

namespace EarthWright.Terrain
{
    /// <summary>
    /// Sender side: runs the hooks and guards, finds every terrain compiler an edit touches (creating a missing one where
    /// the edit changes that heightmap, as the game does for its own terrain ops) and sends each the edit through the
    /// compiler's own network view, which delivers it to the compiler's owner. A privileged edit goes through the server
    /// instead (<see cref="ServerRelay"/>). Every part carries a request id the receiver answers (<see cref="EditAnswers"/>);
    /// an edit is never sent to an unowned compiler (the game would hand it to everybody and nobody would apply it): such
    /// a compiler is claimed first, as the game does with one it creates.
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
            try
            {
                List<TerrainComp> comps = Compilers(edit);
                EditEvents.RaiseBeforeSend(edit, comps);
                Route(edit, comps);
                EditEvents.RaiseSent(edit);
            }
            finally
            {
                TargetZones.Forget();
            }
        }

        private static void Route(TerrainEdit edit, List<TerrainComp> comps)
        {
            if (GeneralSettings.DebugLog.Value)
                Plugin.Log.LogInfo($"Edit {edit.Source} ({edit.Kind}, {edit.Flags}) to {comps.Count} terrain compilers");
            foreach (TerrainComp comp in comps)
            {
                TerrainEdit part = PartFor(comp, edit);
                if (part != null)
                    SendPart(comp, part, 0);
            }
        }

        /// <summary>
        /// Sends one compiler its part with a new request id: to the compiler's owner, or through the server for a
        /// privileged edit. An unowned compiler is claimed first. Also used to send a part again after a retry answer.
        /// </summary>
        internal static void SendPart(TerrainComp comp, TerrainEdit part, int attempts)
        {
            ZNetView view = comp.m_nview;
            if (!view.HasOwner())
                view.ClaimOwnership();
            EditAnswers.Track(comp, part, attempts);
            if (part.Has(EditFlags.Privileged))
                ServerRelay.Send(comp, part);
            else
                view.InvokeRPC(OwnerHandler.RpcName, EditWire.Write(part));
        }

        /// <summary>
        /// The compilers of every heightmap the edit touches. A missing one is created only where the edit would change
        /// something (planned on this machine's copy of that ground, <see cref="Engine.WouldChange"/>), so a stroke near a
        /// zone edge leaves no empty compiler on the neighbouring heightmap. Found once per send and handed to the hooks.
        /// </summary>
        public static List<TerrainComp> Compilers(TerrainEdit edit)
        {
            List<TerrainComp> comps = new List<TerrainComp>();
            foreach (Heightmap map in Maps(edit))
            {
                TerrainComp comp = CompilerOf(map, edit);
                if (comp != null && comp.m_nview != null && comp.m_nview.IsValid())
                    comps.Add(comp);
            }
            return comps;
        }

        /// <summary>The heightmap's compiler; a missing one is created when the edit changes this heightmap (a restore always).</summary>
        private static TerrainComp CompilerOf(Heightmap map, TerrainEdit edit)
        {
            TerrainComp comp = TerrainComp.FindTerrainCompiler(map.transform.position);
            if (comp != null)
                return comp;
            bool restore = edit.Kind == EditKind.Vertices && edit.Vertices.Mode == VertexMode.Restore;
            return restore || Engine.WouldChange(map, edit) ? map.GetAndCreateTerrainCompiler() : null;
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
                StrokeMaps(edit.Stroke, maps);
            }
            return maps;
        }

        /// <summary>
        /// The heightmaps the stroke's own reach (a circle holding every vertex and paint cell it may touch) gets into. The
        /// gentle-slopes ring is left out on purpose: the owner relaxes only a compiler the stroke itself changed, and
        /// only within it, so a heightmap the reach misses is never changed.
        /// </summary>
        private static void StrokeMaps(BrushStroke stroke, List<Heightmap> maps)
        {
            float reach = stroke.Reach;
            Heightmap.FindHeightmap(stroke.Center, reach, maps);
            for (int i = maps.Count - 1; i >= 0; i--)
            {
                if (!Reaches(maps[i], stroke.Center, reach))
                    maps.RemoveAt(i);
            }
        }

        /// <summary>The circle (XZ) reaches into the heightmap's square (the game's own test is square against square).</summary>
        private static bool Reaches(Heightmap map, Vector3 center, float radius)
        {
            float half = map.m_width * map.m_scale * 0.5f;
            Vector3 origin = map.transform.position;
            float dx = Mathf.Max(Mathf.Abs(center.x - origin.x) - half, 0f);
            float dz = Mathf.Max(Mathf.Abs(center.z - origin.z) - half, 0f);
            return dx * dx + dz * dz <= radius * radius;
        }

        /// <summary>
        /// Only the heightmaps a target vertex lies on (a vertex on a shared edge counts for both), so a long diagonal
        /// ramp does not create empty compilers in zones its bounding square merely covers.
        /// </summary>
        private static void TargetMaps(VertexSet set, List<Heightmap> maps) => TargetZones.Maps(set, maps);

        /// <summary>The edit as it goes to one compiler: target vertices outside its heightmap are left out; null if none remain.</summary>
        public static TerrainEdit PartFor(TerrainComp comp, TerrainEdit edit)
        {
            if (edit.Kind != EditKind.Vertices || edit.Vertices.Mode != VertexMode.Targets)
                return edit;
            VertexSet part = TargetZones.PartOf(comp.m_hmap, edit.Vertices);
            if (part == null)
                return null;
            return new TerrainEdit
            {
                Kind = EditKind.Vertices, Flags = edit.Flags, SenderPlayer = edit.SenderPlayer,
                SenderPeer = edit.SenderPeer, Source = edit.Source, Vertices = part,
            };
        }
    }
}
