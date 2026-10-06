using System.Collections.Generic;
using EarthWright.Core;
using HarmonyLib;
using UnityEngine;

namespace EarthWright.Terrain
{
    /// <summary>
    /// Owner side: paces the saves of the terrain compilers this machine owns. A save compresses the whole compiler into
    /// its ZDO, which the game then sends to every peer nearby, and a held click applies an edit up to twenty times a
    /// second; so each compiler saves at most once every <see cref="Interval"/> seconds, the edits in between going out
    /// together with the next save as one operation over their combined area (other clients then refresh the grass of
    /// that area only). The owner's own heightmap still rebuilds at every edit, since the arrays are written at once.
    /// A pending save is always written while this machine holds the compiler: when the interval is up, before the
    /// compiler is unloaded or destroyed, before the world is saved and before the session shuts down. Only the server
    /// hands out compilers: a client whose compiler was let go with a save pending waits until it gets it back (the
    /// server, which owns every unowned object, takes it back itself when nobody wrote it since). Should another
    /// machine have taken it, the local copy reloads what was last saved, so this machine shows the ground everyone
    /// else sees, and the edits applied here since then are sent to the new owner, who applies them to its own copy.
    /// </summary>
    public static class SaveThrottle
    {
        public const float Interval = 0.3f;

        private sealed class Entry
        {
            /// <summary>The compiler's ZDO and its id: the game pools ZDO objects, so a destroyed one may come back as another.</summary>
            public ZDO Zdo;
            public ZDOID Uid;
            public bool Pending;
            public float NextSave;
            public Vector3 Center;
            public float Radius;

            /// <summary>The edits applied since the last save, sent on to the new owner if the save is lost.</summary>
            public readonly List<TerrainEdit> Unsaved = new List<TerrainEdit>();
        }

        private static readonly Dictionary<TerrainComp, Entry> entries = new Dictionary<TerrainComp, Entry>();
        private static readonly List<TerrainComp> scratch = new List<TerrainComp>();

        internal static void Initialize() => Ticker.OnUpdate("EarthWright terrain saves", Tick);

        /// <summary>An edit changed the compiler over this area: saves it now, or when its interval is up.</summary>
        public static void Request(TerrainComp comp, TerrainEdit edit, Vector3 center, float radius)
        {
            if (!entries.TryGetValue(comp, out Entry entry))
            {
                entry = new Entry();
                entries[comp] = entry;
            }
            entry.Zdo = comp.m_nview.GetZDO();
            entry.Uid = entry.Zdo.m_uid;
            Grow(entry, center, radius);
            entry.Pending = true;
            entry.Unsaved.Add(edit);
            if (Time.time >= entry.NextSave)
                Save(comp, entry);
        }

        /// <summary>The smallest circle (XZ) holding the pending area and the new one.</summary>
        private static void Grow(Entry entry, Vector3 center, float radius)
        {
            Vector3 offset = center - entry.Center;
            offset.y = 0f;
            float distance = offset.magnitude;
            if (entry.Pending && distance + radius <= entry.Radius)
                return;
            if (!entry.Pending || distance + entry.Radius <= radius)
            {
                entry.Center = center;
                entry.Radius = radius;
                return;
            }
            float grown = (distance + entry.Radius + radius) * 0.5f;
            entry.Center += offset * ((grown - entry.Radius) / distance);
            entry.Radius = grown;
        }

        /// <summary>Writes the compiler to its ZDO as one operation over the area of the edits since the last save.</summary>
        private static void Save(TerrainComp comp, Entry entry)
        {
            comp.m_operations++;
            comp.m_lastOpPoint = entry.Center;
            comp.m_lastOpRadius = entry.Radius;
            comp.Save();
            entry.Pending = false;
            entry.Unsaved.Clear();
            entry.NextSave = Time.time + Interval;
        }

        private static void Tick()
        {
            if (entries.Count == 0)
                return;
            scratch.Clear();
            scratch.AddRange(entries.Keys);
            foreach (TerrainComp comp in scratch)
            {
                if (entries.TryGetValue(comp, out Entry entry))
                    TickOne(comp, entry);
            }
        }

        private static void TickOne(TerrainComp comp, Entry entry)
        {
            if (comp == null || (!entry.Pending && Time.time >= entry.NextSave))
            {
                entries.Remove(comp);
                return;
            }
            // A view without its ZDO is being unloaded: the destroy hook writes the save.
            if (!entry.Pending || Time.time < entry.NextSave || !comp.m_nview.IsValid())
                return;
            ZDO zdo = comp.m_nview.GetZDO();
            if (comp.m_nview.IsOwner() || Reclaim(zdo, comp))
                Save(comp, entry);
            else if (zdo.HasOwner())
                Lost(comp, entry);
        }

        /// <summary>
        /// The compiler has no owner and nobody wrote it since this machine's last save, so this copy is the newest. Only
        /// the server takes it back for the pending save (it hands out every object; a client claiming it could race the
        /// server giving it to a third machine in the same tick). A client waits until the server gives it back.
        /// </summary>
        private static bool Reclaim(ZDO zdo, TerrainComp comp)
        {
            if (!Side.IsServer || zdo.HasOwner() || zdo.DataRevision != comp.m_lastDataRevision)
                return false;
            zdo.SetOwner(ZDOMan.GetSessionID());
            return true;
        }

        /// <summary>
        /// Another machine took the compiler with edits unsaved: reload what was last saved, as everyone else sees it, and
        /// send the unsaved edits to the new owner, who applies them to its copy (no answer is wanted: their senders were
        /// already answered).
        /// </summary>
        private static void Lost(TerrainComp comp, Entry entry)
        {
            entries.Remove(comp);
            comp.m_lastDataRevision = uint.MaxValue;
            if (GeneralSettings.DebugLog.Value)
                Plugin.Log.LogInfo($"Terrain compiler at {comp.transform.position} changed owner before {entry.Unsaved.Count} edits were saved; reloading and handing them on");
            foreach (TerrainEdit edit in entry.Unsaved)
            {
                edit.RequestId = 0;
                comp.m_nview.InvokeRPC(OwnerHandler.RpcName, EditWire.Write(edit));
            }
            entry.Unsaved.Clear();
        }

        /// <summary>The compiler is going away (unloaded or destroyed): writes its pending save first.</summary>
        internal static void OnDestroy(TerrainComp comp)
        {
            if (!entries.TryGetValue(comp, out Entry entry))
                return;
            entries.Remove(comp);
            Flush(comp, entry);
        }

        /// <summary>Writes every pending save now (the world is about to be saved, or the session ends).</summary>
        internal static void FlushAll()
        {
            scratch.Clear();
            scratch.AddRange(entries.Keys);
            foreach (TerrainComp comp in scratch)
            {
                if (entries.TryGetValue(comp, out Entry entry))
                    Flush(comp, entry);
            }
        }

        /// <summary>The session ends: every pending save is written and nothing is kept for the next one.</summary>
        internal static void Shutdown()
        {
            FlushAll();
            entries.Clear();
        }

        /// <summary>
        /// Writes a pending save while this machine owns the ZDO (or may take it back), also when the compiler's network view
        /// already let go of it (the game resets the view just before it destroys an unloaded object): the ZDO is lent
        /// back for the save.
        /// </summary>
        private static void Flush(TerrainComp comp, Entry entry)
        {
            ZNetView view = comp.m_nview;
            ZDO zdo = entry.Zdo;
            if (!entry.Pending || ReferenceEquals(view, null) || zdo == null || zdo.m_uid != entry.Uid || ZDOMan.instance == null
                || ZDOMan.instance.GetZDO(entry.Uid) != zdo)
                return;
            if (!zdo.IsOwner() && !Reclaim(zdo, comp))
            {
                if (zdo.HasOwner() && comp.m_nview.IsValid())
                    Lost(comp, entry);
                return;
            }
            ZDO held = view.m_zdo;
            view.m_zdo = zdo;
            try
            {
                Save(comp, entry);
            }
            finally
            {
                view.m_zdo = held;
            }
        }
    }

    /// <summary>An unloaded or destroyed compiler writes its pending save before it goes (never throws into the game).</summary>
    [HarmonyPatch(typeof(TerrainComp), nameof(TerrainComp.OnDestroy))]
    public static class TerrainCompDestroyPatch
    {
        [HarmonyPrefix]
        public static void Prefix(TerrainComp __instance) => Safe.Run("EarthWright terrain save (unload)", () => SaveThrottle.OnDestroy(__instance));
    }

    /// <summary>The world save holds every pending terrain save (host and dedicated server).</summary>
    [HarmonyPatch(typeof(ZNet), nameof(ZNet.SaveWorld))]
    public static class WorldSaveFlushPatch
    {
        [HarmonyPrefix]
        public static void Prefix() => Safe.Run("EarthWright terrain save (world save)", SaveThrottle.FlushAll);
    }

    /// <summary>Logout and shutdown: pending terrain saves are written while the objects still hold their ZDOs.</summary>
    [HarmonyPatch(typeof(ZNetScene), nameof(ZNetScene.Shutdown))]
    public static class SceneShutdownFlushPatch
    {
        [HarmonyPrefix]
        public static void Prefix() => Safe.Run("EarthWright terrain save (shutdown)", SaveThrottle.Shutdown);
    }
}
