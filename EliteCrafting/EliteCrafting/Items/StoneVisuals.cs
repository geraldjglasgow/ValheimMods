using System.Collections.Generic;
using EliteCrafting.Core;
using EliteCrafting.Rules;
using UnityEngine;
using UnityEngine.Rendering;

namespace EliteCrafting.Items
{
    /// <summary>
    /// How the stones look (prefabs.md sections 4 and 6): tint per stone, scale per grade, tinted icons. The tint
    /// lookups are for every area (Display opts stones into the ground glow in their tint); the drawing itself runs
    /// on clients only and is skipped on a dedicated server, which has no graphics device.
    /// </summary>
    public static class StoneVisuals
    {
        private const float LesserScale = 0.85f;
        private const float GreaterScale = 1.15f;

        // A shard reads as a chip of its stone (SAL-15). Judgement call.
        private const float ShardScale = 0.55f;

        // A greater stone's icon is brighter than its lesser sibling's (prefabs.md section 6). Judgement call.
        private const float GreaterIconLift = 0.25f;

        private static readonly Dictionary<string, AppliedLook> Applied = new Dictionary<string, AppliedLook>();

        /// <summary>No graphics device: a dedicated server. The game's own check (ZNet.IsDedicated is always false in this build).</summary>
        public static bool Headless { get; } = SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null;

        /// <summary>The tint of a stone id; white when the stone has none (Honing, Tempering) or the id is unknown.</summary>
        public static Color Tint(string? stoneId) => StoneTints.TryById(stoneId, out Color tint) ? tint : Color.white;

        /// <summary>Whether the stone id has a tint of its own.</summary>
        public static bool HasTint(string? stoneId) => StoneTints.TryById(stoneId, out _);

        /// <summary>The tint by prefab name (<c>ECF_GrowthLesser</c>), white when none. For code that holds an item, not an id.</summary>
        public static Color TintOfPrefab(string? prefabName) =>
            StoneTints.TryByPrefab(prefabName, out Color tint) ? tint : Color.white;

        /// <summary>The model scale factor of a grade: lesser 0.85, greater 1.15, ungraded 1 (judgement call, PRF-3).</summary>
        public static float GradeScale(StoneGrade grade) =>
            grade == StoneGrade.Lesser ? LesserScale : grade == StoneGrade.Greater ? GreaterScale : 1f;

        /// <summary>Draws one prefab in its current tint and grade (a shard at its own scale). Clients only; a no-op when headless.</summary>
        internal static void Apply(StoneEntry entry, StoneDef? def)
        {
            if (Headless)
            {
                return;
            }
            entry.Look ??= new StoneLook(entry.Prefab);
            Color? tint = StoneTints.TryByPrefab(entry.PrefabName, out Color t) ? t : (Color?)null;
            StoneGrade grade = GradeOf(entry, def);
            AppliedLook? last = Applied.TryGetValue(entry.PrefabName, out AppliedLook found) ? found : null;
            if (last != null && last.Tint == tint && last.Grade == grade)
            {
                return;
            }
            entry.Look.Tint(tint);
            entry.Look.Scale(entry.IsShard ? ShardScale : GradeScale(grade));
            Sprite? icon = IconFor(entry, tint, grade, last);
            Applied[entry.PrefabName] = new AppliedLook(tint, grade, icon);
        }

        private static Sprite? IconFor(StoneEntry entry, Color? tint, StoneGrade grade, AppliedLook? last)
        {
            StoneIcons.Release(last?.Icon);
            Sprite? icon = null;
            if (tint.HasValue)
            {
                Color iconTint = grade == StoneGrade.Greater ? Color.Lerp(tint.Value, Color.white, GreaterIconLift) : tint.Value;
                icon = StoneIcons.Tinted(entry.BaseIcon, iconTint, entry.PrefabName);
            }
            Sprite? shown = icon ?? entry.BaseIcon;
            entry.Shared.m_icons = shown != null ? new[] { shown } : System.Array.Empty<Sprite>();
            return icon;
        }

        // The running definition's grade; a built-in stone without one takes it from its id.
        private static StoneGrade GradeOf(StoneEntry entry, StoneDef? def)
        {
            if (def != null)
            {
                return def.Grade;
            }
            string id = entry.BuiltInId ?? "";
            if (id.EndsWith("_lesser", System.StringComparison.Ordinal)) return StoneGrade.Lesser;
            if (id.EndsWith("_greater", System.StringComparison.Ordinal)) return StoneGrade.Greater;
            return StoneGrade.None;
        }

        private sealed class AppliedLook
        {
            public AppliedLook(Color? tint, StoneGrade grade, Sprite? icon)
            {
                Tint = tint;
                Grade = grade;
                Icon = icon;
            }

            public Color? Tint { get; }
            public StoneGrade Grade { get; }

            /// <summary>The generated icon (null when the base icon is shown), released on the next change.</summary>
            public Sprite? Icon { get; }
        }
    }
}
