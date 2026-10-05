using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace OpenKeep.Blueprints.Tab
{
    /// <summary>
    /// The folder panel's rows: copies of the build menu's own tag button (its look, hover, highlight and gamepad
    /// navigation) in its tag column, each with a small folder icon (the up icon on the first row), indented by its depth,
    /// and a <see cref="FolderTarget"/> that opens, takes drops and renames; the first row also carries the New folder
    /// button (<see cref="NewFolderBadge"/>). The game's own tag buttons are never touched: these rows are OpenKeep's,
    /// made once per menu, reused and hidden when not needed.
    /// </summary>
    public static class FolderRows
    {
        private const int RowTagId = 900000;
        private const float Indent = 14f;
        private const float IconSize = 20f;
        private const float Margin = 4f;

        private sealed class Row
        {
            public BuildUiTagButton Button;
            public FolderTarget Target;
            public Image Icon;
        }

        private static readonly List<Row> rows = new List<Row>();
        private static BuildUi menu;

        /// <summary>A new menu: rows are made again for it (the old ones went with the old menu).</summary>
        public static void Reset(BuildUi ui)
        {
            menu = ui;
            rows.Clear();
        }

        /// <summary>Shows these lines, in order, after everything else in the tag column; extra rows are hidden.</summary>
        public static void Show(List<FolderPanel.Line> lines)
        {
            for (int i = 0; i < lines.Count; i++)
            {
                Row row = RowAt(i);
                if (row == null)
                    return;
                Apply(row, lines[i]);
            }
            HideFrom(lines.Count);
        }

        public static void HideFrom(int first)
        {
            for (int i = first; i < rows.Count; i++)
            {
                if (rows[i].Button != null)
                    rows[i].Button.gameObject.SetActive(false);
            }
        }

        private static void Apply(Row row, FolderPanel.Line line)
        {
            row.Button.Setup(line.Text, RowTagId, false);
            row.Button.SetToggled(line.Current);
            row.Target.Set(line.Folder, line.Opens, renames: true);
            row.Icon.sprite = BlueprintIcons.Get(line.Up ? BlueprintIcons.FolderUp : BlueprintIcons.Folder);
            float left = Margin + line.Depth * Indent;
            row.Icon.rectTransform.anchoredPosition = new Vector2(left, 0f);
            foreach (TextMeshProUGUI text in row.Button.m_textMeshes)
                text.rectTransform.offsetMin = new Vector2(left + IconSize + Margin, text.rectTransform.offsetMin.y);
            row.Button.gameObject.SetActive(true);
            row.Button.transform.SetAsLastSibling();
        }

        /// <summary>The row at an index, made from the game's tag button the first time; null without a menu.</summary>
        private static Row RowAt(int index)
        {
            while (rows.Count <= index && menu != null && menu.m_tagButtonPrefab != null)
            {
                Row made = Make();
                if (rows.Count == 0)
                    NewFolderBadge.Attach(made.Button, menu);
                rows.Add(made);
            }
            return index < rows.Count ? rows[index] : null;
        }

        private static Row Make()
        {
            GameObject go = Object.Instantiate(menu.m_tagButtonPrefab, menu.m_tagListScrollContent, false);
            go.name = "OpenKeep Folder Row";
            Row row = new Row { Button = go.GetComponent<BuildUiTagButton>(), Target = go.AddComponent<FolderTarget>() };
            FolderTarget target = row.Target;
            row.Button.Selected += id => target.Activate();
            row.Icon = TabLook.Image(go.transform, "OpenKeep Icon", Color.white);
            row.Icon.preserveAspect = true;
            RectTransform icon = row.Icon.rectTransform;
            icon.anchorMin = icon.anchorMax = icon.pivot = new Vector2(0f, 0.5f);
            icon.sizeDelta = new Vector2(IconSize, IconSize);
            return row;
        }
    }
}
