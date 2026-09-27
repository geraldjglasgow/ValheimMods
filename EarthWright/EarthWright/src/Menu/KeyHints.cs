using System.Collections.Generic;
using System.Linq;
using EarthWright.Actions;

namespace EarthWright.Menu
{
    /// <summary>A key a menu entry's description can list.</summary>
    public enum HintKind
    {
        /// <summary>Modifier + wheel or the increase/decrease keys: the brush size or the selected value.</summary>
        Adjust,
        Next,
        Shape,
        Rotate,
        Style,
        Lock,
        Mode,
        Hard,
        Paint,
        Grid,
        Undo,
        Reset,
        /// <summary>The ramp profile key (the Paths module's own setting).</summary>
        Profile,
        Back,
        QuickRamp,
        Carve,
        CarvePaved,
        /// <summary>The aim-at-the-edge toggle.</summary>
        Edge,
        /// <summary>Reset around the player (the reset key is <see cref="Reset"/>).</summary>
        ResetAround,
    }

    /// <summary>
    /// The line of key hints that ends a menu entry's description: each hint is the key's current binding in yellow
    /// followed by a $word saying what it does, so the words follow the game's language and the keys follow the
    /// configuration. Hints the brush does not honour for the entry are left out (<see cref="HintRules"/>). The line is
    /// rebuilt whenever a setting changes (see <see cref="MenuRefresh"/>).
    /// </summary>
    public static class KeyHints
    {
        private static readonly Dictionary<HintKind, (KeyRef Key, string Word)> simple = new Dictionary<HintKind, (KeyRef, string)>
        {
            { HintKind.Next, (KeyNames.NextValue, "$ew_menu_hint_next") },
            { HintKind.Shape, (KeyNames.CycleShape, "$ew_menu_hint_shape") },
            { HintKind.Style, (KeyNames.CycleStyle, "$ew_menu_hint_style") },
            { HintKind.Lock, (KeyNames.LockTarget, "$ew_menu_hint_lock") },
            { HintKind.Mode, (KeyNames.CycleTarget, "$ew_menu_hint_mode") },
            { HintKind.Hard, (KeyNames.HardLevel, "$ew_menu_hint_hard") },
            { HintKind.Paint, (KeyNames.CyclePaint, "$ew_menu_hint_paint") },
            { HintKind.Grid, (KeyNames.GridMode, "$ew_menu_hint_grid") },
            { HintKind.Edge, (KeyNames.AimAtEdge, "$ew_menu_hint_edge") },
            { HintKind.ResetAround, (KeyNames.ResetAround, "$ew_menu_hint_resetaround") },
            { HintKind.Reset, (KeyNames.ResetArea, "$ew_menu_hint_reset") },
            { HintKind.Profile, (KeyNames.RampProfile, "$ew_menu_hint_profile") },
            { HintKind.Back, (KeyNames.RemovePoint, "$ew_menu_hint_back") },
            { HintKind.QuickRamp, (KeyNames.QuickRamp, "$ew_menu_hint_quickramp") },
            { HintKind.Carve, (KeyNames.CarveRoad, "$ew_menu_hint_carve") },
            { HintKind.CarvePaved, (KeyNames.CarvePaved, "$ew_menu_hint_carvepaved") },
        };

        /// <summary>
        /// Every key the brush honours with a plain brush entry (clear, uproot, groundbreaker and custom entries too); the
        /// hints that do not apply to an entry are dropped by <see cref="HintRules"/>.
        /// </summary>
        public static readonly HintKind[] Brush =
        {
            HintKind.Adjust, HintKind.Next, HintKind.Shape, HintKind.Rotate, HintKind.Style, HintKind.Lock, HintKind.Mode,
            HintKind.Hard, HintKind.Paint, HintKind.Grid, HintKind.Edge, HintKind.Reset, HintKind.ResetAround, HintKind.Undo,
        };

        /// <summary>The keys an entry's description lists: a ramp's or road's own keys, every brush key for the rest.</summary>
        public static IEnumerable<HintKind> For(ToolAction action, IEnumerable<HintKind> pathKeys)
        {
            if (action == null || !action.IsPathTool)
                return Brush;
            return (pathKeys ?? Enumerable.Empty<HintKind>()).Concat(new[] { HintKind.Lock, HintKind.Undo }).Distinct();
        }

        /// <summary>"$ew_menu_keys" and the hints that apply to the action, joined by commas; empty when none is bound.</summary>
        public static string Line(IEnumerable<HintKind> kinds, ToolAction action)
        {
            List<string> parts = kinds.Where(k => HintRules.Applies(k, action)).Select(k => Part(k, action)).Where(p => p != null).ToList();
            return parts.Count == 0 ? "" : "$ew_menu_keys " + string.Join(", ", parts);
        }

        private static string Part(HintKind kind, ToolAction action)
        {
            if (kind == HintKind.Adjust)
                return Adjust(HintRules.AdjustWord(action));
            if (kind == HintKind.Rotate)
                return Pair(KeyNames.RotateLeft, KeyNames.RotateRight, "$ew_menu_hint_rotate", " / ");
            if (kind == HintKind.Undo)
                return Pair(KeyNames.Undo, KeyNames.Redo, "$ew_menu_hint_undo", " / ");
            if (!simple.TryGetValue(kind, out var hint))
                return null;
            string key = KeyText.Of(hint.Key);
            return key == null ? null : Yellow(key) + " " + hint.Word;
        }

        /// <summary>"Alt+Wheel / [ ] size": the scroll modifier and the increase/decrease keys, then what they change.</summary>
        private static string Adjust(string word)
        {
            string modifier = KeyText.Of(KeyNames.AdjustModifier);
            string keys = Keys(KeyNames.DecreaseValue, KeyNames.IncreaseValue, " ");
            List<string> ways = new List<string>();
            if (modifier != null)
                ways.Add(Yellow(modifier + "+$ew_menu_key_wheel"));
            if (keys != null)
                ways.Add(Yellow(keys));
            return ways.Count == 0 ? null : string.Join(" / ", ways) + " " + word;
        }

        private static string Pair(KeyRef first, KeyRef second, string word, string separator)
        {
            string keys = Keys(first, second, separator);
            return keys == null ? null : Yellow(keys) + " " + word;
        }

        /// <summary>Two keys joined by the separator ("[ ]", "Ctrl+Z / Ctrl+Y"), or the one that is bound, or null.</summary>
        private static string Keys(KeyRef first, KeyRef second, string separator)
        {
            string a = KeyText.Of(first);
            string b = KeyText.Of(second);
            if (a == null || b == null)
                return a ?? b;
            return a + separator + b;
        }

        private static string Yellow(string text) => "<color=yellow>" + text + "</color>";
    }
}
