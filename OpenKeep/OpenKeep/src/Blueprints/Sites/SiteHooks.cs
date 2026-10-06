using System;
using System.Collections.Generic;
using UnityEngine;

namespace OpenKeep.Blueprints.Sites
{
    /// <summary>
    /// The seams between the blueprint code and the site and planner modules, so each module registers itself instead of
    /// the shared files naming it: per-frame and OnGUI callbacks (run by <see cref="BlueprintRunner"/>), the Site planner
    /// entry's click (routed by <see cref="BlueprintTool"/>) and a hook when a site marker comes to life (RPC
    /// registration). Every callback runs guarded.
    /// </summary>
    public static class SiteHooks
    {
        private static readonly List<(string Name, Action Run)> updates = new List<(string, Action)>();
        private static readonly List<(string Name, Action Run)> guis = new List<(string, Action)>();
        private static readonly List<Func<bool>> guiWanted = new List<Func<bool>>();

        /// <summary>The Site planner entry was clicked by the local player (set by the planner module).</summary>
        public static Action<Player> PlannerClick = player => { };

        /// <summary>The Copy entry was clicked by the local player (set by the copy module).</summary>
        public static Action<Player> CopyClick = player => { };

        /// <summary>A site marker with a valid ZDO woke on this machine (register RPCs here).</summary>
        public static event Action<SiteMarker> MarkerCreated;

        public static void OnUpdate(string name, Action run) => updates.Add((name, run));

        /// <summary>An OnGUI callback and when it has something to draw; the runner draws only while one has.</summary>
        public static void OnGui(string name, Action run, Func<bool> wanted)
        {
            guis.Add((name, run));
            guiWanted.Add(wanted);
        }

        /// <summary>A registered OnGUI callback has something to draw now.</summary>
        internal static bool GuiWanted
        {
            get
            {
                foreach (Func<bool> wanted in guiWanted)
                {
                    if (wanted())
                        return true;
                }
                return false;
            }
        }

        internal static void Update()
        {
            foreach ((string name, Action run) in updates)
                BlueprintSafe.Run(name, run);
        }

        internal static void Gui()
        {
            foreach ((string name, Action run) in guis)
                BlueprintSafe.Run(name, run);
        }

        internal static void RaiseMarkerCreated(SiteMarker marker)
        {
            if (MarkerCreated != null)
                BlueprintSafe.Run("OpenKeep site marker", () => MarkerCreated(marker));
        }
    }
}
