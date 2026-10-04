using System;
using System.Collections.Generic;
using UnityEngine;

namespace DevBridge.Overlay
{
    /// <summary>One kind of thing /overlay draws: whether it is on, its pool of lines, and what its last redraw found.</summary>
    internal sealed class Category
    {
        internal readonly string Name;
        internal readonly LinePool Lines;
        private readonly Action<OverlayArea, Category> draw;
        private readonly Dictionary<string, object> stats = new Dictionary<string, object>();
        private string error;

        internal bool On;

        internal Category(string name, Action<OverlayArea, Category> draw)
        {
            (Name, this.draw) = (name, draw);
            Lines = new LinePool(name);
        }

        /// <summary>Draws the category again; a failure is kept for the reply and logged once, and the others go on.</summary>
        internal void Redraw(OverlayArea area, Transform root, int max)
        {
            stats.Clear();
            Lines.Begin(root, max);
            try
            {
                draw(area, this);
                error = null;
            }
            catch (Exception failure)
            {
                string message = failure.GetType().Name + ": " + failure.Message;
                if (message != error) Debug.LogWarning($"[DevBridge] overlay {Name}: {message}\n{failure.StackTrace}");
                error = message;
            }
            Lines.End();
        }

        internal void Count(string key, int by = 1) => stats[key] = (stats.TryGetValue(key, out object count) ? (int)count : 0) + by;

        internal void Note(string key, object value) => stats[key] = value;

        internal void Off()
        {
            On = false;
            Lines.Clear();
            stats.Clear();
            error = null;
        }

        internal Dictionary<string, object> Report()
        {
            var report = new Dictionary<string, object>(stats) { ["lines"] = Lines.Used };
            if (Lines.Capped) report["capped"] = $"at {Lines.Max} lines, the rest not drawn: raise max= or lower radius=";
            if (error != null) report["error"] = error;
            return report;
        }
    }
}
