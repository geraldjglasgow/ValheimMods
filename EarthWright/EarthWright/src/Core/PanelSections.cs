using System;
using System.Collections.Generic;
using System.Linq;

namespace EarthWright.Core
{
    /// <summary>
    /// Sections of the EarthWright panel (the typed-value window drawn by the Preview module). A module adds a section
    /// with an IMGUI draw callback, called inside the panel's vertical layout; admin sections are only drawn for admins.
    /// </summary>
    public static class PanelSections
    {
        public sealed class Section
        {
            public int Order;
            public string Title;
            public bool AdminOnly;
            public Action Draw;
        }

        private static readonly List<Section> sections = new List<Section>();

        public static void Add(int order, string title, Action draw, bool adminOnly = false)
        {
            sections.Add(new Section { Order = order, Title = title, Draw = draw, AdminOnly = adminOnly });
        }

        /// <summary>The sections this player may see, in order.</summary>
        public static List<Section> Visible() => sections.Where(s => !s.AdminOnly || Side.LocalIsAdmin).OrderBy(s => s.Order).ToList();
    }
}
