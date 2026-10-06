using System.Collections.Generic;
using System.Linq;
using OpenKeep.Core;
using UnityEngine;

namespace OpenKeep.Blueprints
{
    /// <summary>
    /// The Blueprints tab's lines beside the crosshair, one block per tool (a blueprint, Fix ground) so each sets and
    /// clears only its own: what is selected and its size, the floor height, the earth to cut and fill with its stone,
    /// the materials, then in red why it cannot be done there, or what a click does; a running build's progress.
    /// Drawn with the game's text look (white with a shadow), only while a block is set and no game window is open.
    /// </summary>
    public static class BlueprintHud
    {
        public const string BlueprintBlock = "blueprint";
        public const string FixBlock = "fix";
        public const string Red = "#ff5a4a";
        public const string Grey = "#c8c8c8";
        private const float Offset = 40f;
        private const float Width = 600f;

        private static readonly Dictionary<string, List<string>> blocks = new Dictionary<string, List<string>>();
        private static GUIStyle style;
        private static GUIStyle shadow;

        public static void Show(Blueprint bp, SitePlan plan, bool pinned, bool outlineOnly)
        {
            List<string> lines = new List<string>
            {
                "<b>" + BlueprintWords.Format(BlueprintWords.Title, bp.Name, bp.Pieces.Count,
                    BlueprintWords.Metres(bp.PieceBounds.width), BlueprintWords.Metres(bp.PieceBounds.height)) + "</b>",
            };
            bool current = plan != null && plan.Blueprint == bp;
            if (current)
                AddPlan(lines, plan);
            if (outlineOnly)
                lines.Add(Coloured(Language.Localize(BlueprintWords.Outline), Grey));
            lines.Add(current && plan.Problem != null ? Coloured(plan.Problem, Red)
                : Language.Localize(pinned ? BlueprintWords.Pinned : BlueprintWords.ClickToPin));
            Set(BlueprintBlock, lines);
        }

        private static void AddPlan(List<string> lines, SitePlan plan)
        {
            string floor = BlueprintWords.Metres(plan.Frame.Ground);
            lines.Add(plan.WaterMoved
                ? BlueprintWords.Format(BlueprintWords.GroundWater, floor, BlueprintWords.Metres(WaterRule.Sea))
                : BlueprintWords.Format(BlueprintWords.Ground, floor));
            AddGround(lines, plan.Work, plan.Bill);
        }

        /// <summary>The earth line (with its stone) and the materials line of a bill.</summary>
        public static void AddGround(List<string> lines, GroundWork w, MaterialBill bill)
        {
            if (w.Cut > 0.5f || w.Fill > 0.5f)
                lines.Add(BlueprintWords.Format(BlueprintWords.Earth, w.Cut.ToString("0"), BlueprintWords.Metres(w.MaxCut), bill.GroundRemoved,
                    w.Fill.ToString("0"), BlueprintWords.Metres(w.MaxFill), bill.GroundNeeded));
            if (w.Limited > 0)
                lines.Add(Coloured(BlueprintWords.Format(BlueprintWords.Limited, w.Limited), Grey));
            string summary = bill.Summary();
            if (bill.Free)
                lines.Add(Language.Localize(BlueprintWords.Free));
            else if (summary != null || bill.StoneBack > 0)
                lines.Add(BlueprintWords.Format(BlueprintWords.Materials, summary ?? "-") +
                    (bill.StoneBack > 0 ? "  " + BlueprintWords.Format(BlueprintWords.StoneBack, bill.StoneBack) : ""));
        }

        /// <summary>No blueprint to show: the selected file unreadable, or no ground under the crosshair.</summary>
        public static void ShowEmpty(string error, bool noBlueprint)
        {
            string text = error != null ? BlueprintWords.Format(BlueprintWords.Unreadable, error)
                : Language.Localize(noBlueprint ? BlueprintWords.NoBlueprints : BlueprintWords.Aim);
            Set(BlueprintBlock, new List<string> { Coloured(text, error != null ? Red : Grey) });
        }

        /// <summary>Sets a tool's block of lines; null or empty removes it.</summary>
        public static void Set(string block, List<string> lines)
        {
            if (lines == null || lines.Count == 0)
                blocks.Remove(block);
            else
                blocks[block] = lines;
        }

        public static void Clear(string block) => blocks.Remove(block);

        /// <summary>There is something to draw: a block of lines or a running build (<see cref="BlueprintGui"/> runs only then).</summary>
        public static bool Wanted => blocks.Count > 0 || BuildJob.Busy;

        /// <summary>OnGUI: the blocks (and a running build's progress) below and right of the screen's centre.</summary>
        public static void Draw()
        {
            string progress = BuildJob.Progress;
            if ((blocks.Count == 0 && progress == null) || Hud.instance == null || InventoryGui.IsVisible() || Minimap.IsOpen() || global::Menu.IsVisible())
                return;
            EnsureStyles();
            float y = Screen.height * 0.5f + Offset;
            float x = Screen.width * 0.5f + Offset;
            if (progress != null)
                y += Line(progress, x, y);
            foreach (string line in blocks.Values.SelectMany(b => b))
                y += Line(line, x, y);
        }

        private static float Line(string text, float x, float y)
        {
            float height = style.CalcHeight(new GUIContent(text), Width);
            GUI.Label(new Rect(x + 1f, y + 1f, Width, height), StripColours(text), shadow);
            GUI.Label(new Rect(x, y, Width, height), text, style);
            return height + 2f;
        }

        private static void EnsureStyles()
        {
            if (style != null)
                return;
            style = new GUIStyle(GUI.skin.label) { fontSize = 18, richText = true, wordWrap = true };
            style.normal.textColor = Color.white;
            shadow = new GUIStyle(style);
            shadow.normal.textColor = new Color(0f, 0f, 0f, 0.8f);
        }

        private static string StripColours(string text) => System.Text.RegularExpressions.Regex.Replace(text, "<color=[^>]*>|</color>", "");

        public static string Coloured(string text, string colour) => "<color=" + colour + ">" + text + "</color>";
    }
}
