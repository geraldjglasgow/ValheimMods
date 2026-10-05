using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace EliteCrafting.Items
{
    /// <summary>
    /// How the runes look (prefabs.md sections 4 and 6): a tint per rune and tinted icons. The tint lookups are for
    /// every area (Display opts runes into the ground glow in their tint); the drawing itself runs on clients only and
    /// is skipped on a dedicated server, which has no graphics device.
    /// </summary>
    public static class StoneVisuals
    {
        private static readonly Dictionary<string, AppliedLook> Applied = new Dictionary<string, AppliedLook>();

        /// <summary>No graphics device: a dedicated server. The game's own check (ZNet.IsDedicated is always false in this build).</summary>
        public static bool Headless { get; } = SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null;

        /// <summary>The tint of a rune id; white when the id is unknown.</summary>
        public static Color Tint(string? stoneId) => StoneTints.TryById(stoneId, out Color tint) ? tint : Color.white;

        /// <summary>Whether the rune id has a tint of its own.</summary>
        public static bool HasTint(string? stoneId) => StoneTints.TryById(stoneId, out _);

        /// <summary>The tint by prefab name (<c>ECF_Shaping</c>), white when none. For code that holds an item, not an id.</summary>
        public static Color TintOfPrefab(string? prefabName) =>
            StoneTints.TryByPrefab(prefabName, out Color tint) ? tint : Color.white;

        /// <summary>
        /// Draws one prefab in its current tint. Clients only; a no-op when headless, and for a rune tablet, whose
        /// colour is painted into it (the tint still colours its ground glow, read through <see cref="TintOfPrefab"/>).
        /// </summary>
        internal static void Apply(StoneEntry entry)
        {
            if (Headless || entry.WearsTablet)
            {
                return;
            }
            entry.Look ??= new StoneLook(entry.Prefab);
            Color? tint = StoneTints.TryByPrefab(entry.PrefabName, out Color t) ? t : (Color?)null;
            AppliedLook? last = Applied.TryGetValue(entry.PrefabName, out AppliedLook found) ? found : null;
            if (last != null && last.Tint == tint)
            {
                return;
            }
            entry.Look.Tint(tint);
            Applied[entry.PrefabName] = new AppliedLook(tint, IconFor(entry, tint, last));
        }

        private static Sprite? IconFor(StoneEntry entry, Color? tint, AppliedLook? last)
        {
            StoneIcons.Release(last?.Icon);
            Sprite? icon = tint.HasValue ? StoneIcons.Tinted(entry.BaseIcon, tint.Value, entry.PrefabName) : null;
            Sprite? shown = icon ?? entry.BaseIcon;
            entry.Shared.m_icons = shown != null ? new[] { shown } : System.Array.Empty<Sprite>();
            return icon;
        }

        private sealed class AppliedLook
        {
            public AppliedLook(Color? tint, Sprite? icon)
            {
                Tint = tint;
                Icon = icon;
            }

            public Color? Tint { get; }

            /// <summary>The generated icon (null when the base icon is shown), released on the next change.</summary>
            public Sprite? Icon { get; }
        }
    }
}
