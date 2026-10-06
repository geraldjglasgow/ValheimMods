using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DevBridge.Events;
using DevBridge.Stage;
using UnityEngine;

namespace DevBridge.Swap
{
    /// <summary>A bundle file watched for rebuilds: where it is, the write time last seen, and the one loaded.</summary>
    internal sealed class Watch
    {
        internal string Bundle;
        internal string Path;
        internal DateTime Seen;
        internal DateTime Loaded;
    }

    /// <summary>
    /// Bundle files watched for the workshop's rebuilds (watch=1). A file whose write time changed and then held still
    /// for a second (the build has finished writing it) is reloaded as /bundle?reload= does it, which takes the swaps
    /// from it off before the old bundle unloads and puts them on again from the new one, and puts the stage's assets back.
    /// </summary>
    internal static class Watches
    {
        private static readonly Dictionary<string, Watch> Active = new Dictionary<string, Watch>(StringComparer.OrdinalIgnoreCase);

        internal static IEnumerable<string> Names => Active.Keys.OrderBy(n => n).ToList();

        internal static bool On(string bundle) => Active.ContainsKey(bundle);

        internal static bool Any => Active.Count > 0;

        internal static void Start(LoadedBundle loaded)
        {
            Active[loaded.Name] = new Watch { Bundle = loaded.Name, Path = loaded.Path, Seen = loaded.FileTime, Loaded = loaded.FileTime };
            SwapKeeper.Ensure();
        }

        /// <summary>Stops watching that bundle, or every bundle when none is named; returns the names no longer watched.</summary>
        internal static List<string> Stop(string bundle)
        {
            List<string> stopped = Active.Keys.Where(k => bundle == null || string.Equals(k, bundle, StringComparison.OrdinalIgnoreCase)).ToList();
            foreach (string name in stopped) Active.Remove(name);
            return stopped;
        }

        internal static void Tick()
        {
            foreach (Watch watch in Active.Values.ToList()) Check(watch);
        }

        private static void Check(Watch watch)
        {
            LoadedBundle loaded = Bundles.All.FirstOrDefault(b => string.Equals(b.Name, watch.Bundle, StringComparison.OrdinalIgnoreCase));
            if (loaded != null) watch.Path = loaded.Path;
            if (!File.Exists(watch.Path)) return;
            DateTime written = File.GetLastWriteTime(watch.Path);
            DateTime current = loaded?.FileTime ?? watch.Loaded;
            bool settled = written == watch.Seen;
            watch.Seen = written;
            if (written == current || !settled) return;
            watch.Loaded = written;
            Reload(watch, loaded);
        }

        /// <summary>The rebuilt file loaded again (or loaded anew after a build that failed to load), its swaps put back on.</summary>
        private static void Reload(Watch watch, LoadedBundle loaded)
        {
            try
            {
                Dictionary<string, object> result = loaded != null ? Reloads.Reload(loaded) : Swaps.Restore(Bundles.Load(watch.Path), new Dictionary<string, object>());
                result["action"] = "rebuilt";
                result["bundle"] = watch.Bundle;
                EventLog.Add("swap", result);
                Debug.Log($"[DevBridge] swap: {watch.Bundle} was rebuilt; reloaded and its swaps applied again");
            }
            catch (Exception error)
            {
                if (error is IOException) watch.Loaded = default; // the build still had the file open: try again next second
                EventLog.Add("swap", new Dictionary<string, object> { ["action"] = "rebuilt", ["bundle"] = watch.Bundle, ["error"] = error.Message });
                Debug.LogWarning($"[DevBridge] swap: reloading the rebuilt {watch.Bundle} failed: {error.Message}");
            }
        }
    }

    /// <summary>
    /// Once a second, on the main thread: copies of swapped prefabs spawned since take the swap (a starred creature's
    /// own body material included; the copies are caught as they are made, see NewCopies), and watched bundle files the
    /// workshop rebuilt are reloaded. With no swap and no watch left it does nothing but one test per frame.
    /// </summary>
    internal sealed class SwapKeeper : MonoBehaviour
    {
        private float next;

        internal static void Ensure()
        {
            GameObject host = DevBridgePlugin.Instance.gameObject;
            if (!host.GetComponent<SwapKeeper>()) host.AddComponent<SwapKeeper>();
        }

        private void Update()
        {
            if (Time.unscaledTime < next || (!Swaps.Any && !Watches.Any)) return;
            next = Time.unscaledTime + 1f;
            Swaps.Sweep();
            Watches.Tick();
        }
    }
}
