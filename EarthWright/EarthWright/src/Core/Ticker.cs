using System;
using System.Collections.Generic;

namespace EarthWright.Core
{
    /// <summary>
    /// Per-frame callbacks driven by the plugin's own Update and OnGUI, so modules need no MonoBehaviour of their own.
    /// Each callback runs guarded: an exception is logged under EarthWright and the callback is skipped for 5 seconds.
    /// </summary>
    public static class Ticker
    {
        private sealed class Entry
        {
            public string Name;
            public Action Run;
            public float MutedUntil;
        }

        private static readonly List<Entry> updates = new List<Entry>();
        private static readonly List<Entry> guis = new List<Entry>();

        /// <summary>Runs every frame from the plugin's Update.</summary>
        public static void OnUpdate(string name, Action run) => updates.Add(new Entry { Name = name, Run = run });

        /// <summary>Runs from the plugin's OnGUI (IMGUI drawing and events).</summary>
        public static void OnGui(string name, Action run) => guis.Add(new Entry { Name = name, Run = run });

        internal static void Tick() => RunAll(updates);

        internal static void Gui() => RunAll(guis);

        private static void RunAll(List<Entry> entries)
        {
            float now = UnityEngine.Time.realtimeSinceStartup;
            foreach (Entry entry in entries)
            {
                if (now < entry.MutedUntil)
                    continue;
                try
                {
                    entry.Run();
                }
                catch (Exception e)
                {
                    entry.MutedUntil = now + 5f;
                    Plugin.Log.LogError($"{entry.Name}: {e}");
                }
            }
        }
    }
}
