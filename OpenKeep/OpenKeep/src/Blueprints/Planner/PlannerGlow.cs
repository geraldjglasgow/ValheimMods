using System;
using System.Collections.Generic;
using OpenKeep.Blueprints.Sites;
using UnityEngine;

namespace OpenKeep.Blueprints.Planner
{
    /// <summary>
    /// The planner's three glows on the site ghosts (<see cref="SiteGhost.SetGlow"/>), one per key: the piece under
    /// the crosshair (faint), the selection (yellow) and the queue row under the mouse (warm orange). A glow is set
    /// again only when its site, the site's ghost or the caller's stamp changed, so asking every frame costs nothing,
    /// and it is cleared from the ghost it was set on when it moves to another site or goes.
    /// </summary>
    public static class PlannerGlow
    {
        public const string HoverKey = "planner.hover";
        public const string SelectionKey = "planner.selection";
        public const string PanelKey = "planner.panel";

        public static readonly Color Hover = new Color(0.75f, 0.9f, 1f, 0.35f);
        public static readonly Color Selection = new Color(1f, 0.85f, 0.15f, 0.9f);
        public static readonly Color Panel = new Color(1f, 0.5f, 0.12f, 0.8f);

        /// <summary>Where a key's glow was last set: the site, its ghost then, and the caller's stamp.</summary>
        private sealed class Shown
        {
            public SiteMarker Site;
            public SiteGhost Ghost;
            public object Stamp;
        }

        private static readonly Dictionary<string, Shown> shown = new Dictionary<string, Shown>();

        /// <summary>The site's pieces glow under the key; <paramref name="pieces"/> is asked only when the glow is set again.</summary>
        public static void Show(string key, SiteMarker site, object stamp, Func<ICollection<int>> pieces, Color colour)
        {
            SiteGhost ghost = site != null ? SiteGhost.For(site) : null;
            shown.TryGetValue(key, out Shown now);
            if (now != null && now.Ghost == ghost && now.Site == site && Equals(now.Stamp, stamp))
                return;
            if (now != null && now.Ghost != ghost)
                Hide(key);
            if (ghost == null)
                return;
            ghost.SetGlow(key, pieces(), colour);
            shown[key] = new Shown { Site = site, Ghost = ghost, Stamp = stamp };
        }

        public static void Hide(string key)
        {
            if (!shown.TryGetValue(key, out Shown now))
                return;
            shown.Remove(key);
            // A ghost whose site is gone went with it: nothing left to clear.
            if (now.Site != null)
                BlueprintSafe.Run("OpenKeep site planner glow", () => now.Ghost.ClearGlow(key));
        }

        public static void HideAll()
        {
            if (shown.Count == 0)
                return;
            foreach (string key in new List<string>(shown.Keys))
                Hide(key);
        }
    }
}
