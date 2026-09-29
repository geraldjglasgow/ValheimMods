using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace DevBridge.Ui
{
    /// <summary>Indented text dumps of the hierarchy (/ui) and path lists of matching objects (/find).</summary>
    internal sealed class UiTree
    {
        private const int MaxLines = 1500;

        private readonly StringBuilder text = new StringBuilder();
        private readonly int maxDepth;
        private readonly bool all;
        private int lines;

        private UiTree(int maxDepth, bool all)
        {
            this.maxDepth = maxDepth;
            this.all = all;
        }

        internal static string Dump(IEnumerable<Transform> roots, int depth, bool all)
        {
            var tree = new UiTree(depth, all);
            foreach (Transform root in roots)
            {
                tree.text.AppendLine("== " + ScenePaths.PathOf(root));
                tree.Write(root, 0);
            }
            if (tree.lines >= MaxLines) tree.text.AppendLine($"... cut at {MaxLines} lines: give a deeper path= or a smaller depth=");
            return tree.text.ToString();
        }

        private void Write(Transform t, int depth)
        {
            if (lines++ >= MaxLines) return;
            text.Append(' ', depth * 2).AppendLine(UiDescribe.Line(t));
            List<Transform> shown = ScenePaths.Children(t).Where(c => all || c.gameObject.activeSelf).ToList();
            int hidden = t.childCount - shown.Count;
            if (depth >= maxDepth && shown.Count > 0)
                text.Append(' ', depth * 2 + 2).AppendLine($"... {shown.Count} children");
            else
                foreach (Transform child in shown) Write(child, depth + 1);
            if (hidden > 0) text.Append(' ', depth * 2 + 2).AppendLine($"({hidden} inactive, add all=1)");
        }

        /// <summary>Root canvases that are showing, the default roots of /ui.</summary>
        internal static IEnumerable<Transform> ActiveCanvases() =>
            UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None)
                .Where(c => c.isRootCanvas && c.isActiveAndEnabled)
                .Select(c => c.transform)
                .OrderBy(ScenePaths.PathOf);

        internal static string Find(Func<Transform, bool> match, bool uiOnly, bool all, int limit)
        {
            var found = new StringBuilder();
            int count = 0;
            foreach (Transform root in ScenePaths.Roots())
            {
                foreach (Transform t in root.GetComponentsInChildren<Transform>(all))
                {
                    if (uiOnly && !(t is RectTransform) || !match(t)) continue;
                    if (++count <= limit) found.AppendLine(ScenePaths.PathOf(t) + "  " + UiDescribe.Line(t));
                }
            }
            if (count > limit) found.AppendLine($"... {count - limit} more, raise limit=");
            return count == 0 ? "nothing matched" : found.ToString();
        }
    }
}
