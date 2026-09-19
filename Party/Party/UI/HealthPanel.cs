using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Party.Client;

namespace Party.UI
{
    /// <summary>
    /// The party health panel, built as a real Canvas (Valheim's own font, native rounded sprites, drag via the
    /// event system). Scaled against a 1920x1080 reference so the layout settings mean the same thing at any
    /// resolution. Ticked once a frame from <see cref="Client.PartyTicker"/>. Mouse input passes straight through
    /// it except in edit mode, so it can never steal clicks from the map or inventory.
    /// </summary>
    public static class HealthPanel
    {
        public static bool EditMode { get; private set; }

        private static GameObject canvasRoot;
        private static RectTransform panelRect;
        private static CanvasGroup canvasGroup;
        private static TMP_Text titleText;
        private static readonly List<PartyRowView> rows = new List<PartyRowView>();
        private static string lastRowSignature = "";

        public static void ToggleEditMode(bool on)
        {
            EditMode = on;
            if (!on)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }

        /// <summary>Re-asserts the free cursor every frame - the game's own controller re-locks it otherwise.</summary>
        public static void EnforceCursor()
        {
            if (!EditMode)
                return;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        public static bool CanDrag() => EditMode;

        /// <summary>Clamped so the panel stays reachable and the saved Y can never hit the "unset" sentinel (-1).</summary>
        public static void PersistPosition(Vector2 anchoredPosition)
        {
            Rect area = ((RectTransform)canvasRoot.transform).rect;
            PartyConfig.PanelX.Value = Mathf.Clamp(anchoredPosition.x, 0f, area.width - 60f);
            PartyConfig.PanelY.Value = Mathf.Clamp(-anchoredPosition.y, 0f, area.height - 60f);
        }

        public static void Tick()
        {
            bool shouldShow = PartyClientState.InParty || EditMode;
            EnsureBuilt();
            canvasRoot.SetActive(shouldShow);
            if (!shouldShow)
                return;
            EnsureRowsMatchSettings();
            List<(PartyMemberView member, float? distance)> content = RowContents();
            ApplyLayout(content.Count);
            UpdateRows(content);
        }

        private static void EnsureBuilt()
        {
            if (canvasRoot != null)
                return;
            EnsureEventSystem();
            BuildCanvas();
        }

        private static void EnsureEventSystem()
        {
            if (Object.FindFirstObjectByType<EventSystem>() != null)
                return;
            GameObject es = new GameObject("PartyEventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            Object.DontDestroyOnLoad(es);
        }

        private static void BuildCanvas()
        {
            canvasRoot = new GameObject("PartyCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            Object.DontDestroyOnLoad(canvasRoot);
            Canvas canvas = canvasRoot.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 500;
            CanvasScaler scaler = canvasRoot.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 1f;

            GameObject panelGo = new GameObject("Panel", typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
            panelGo.transform.SetParent(canvasRoot.transform, false);
            panelRect = panelGo.GetComponent<RectTransform>();
            panelRect.anchorMin = panelRect.anchorMax = panelRect.pivot = new Vector2(0f, 1f);
            canvasGroup = panelGo.GetComponent<CanvasGroup>();

            Image background = panelGo.GetComponent<Image>();
            RoundedSprite.Apply(background, 10f);
            background.color = new Color(0.05f, 0.05f, 0.05f, 0.75f);
            panelGo.AddComponent<PartyDragHandler>();

            BuildTitle(panelGo.transform);
        }

        private static void BuildTitle(Transform parent)
        {
            GameObject titleGo = new GameObject("Title", typeof(RectTransform));
            titleGo.transform.SetParent(parent, false);
            RectTransform rect = titleGo.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0f, 1f);
            titleText = titleGo.AddComponent<TextMeshProUGUI>();
            titleText.font = PartyFont.Get();
            titleText.color = Color.white;
            titleText.fontStyle = FontStyles.Bold;
            titleText.textWrappingMode = TextWrappingModes.NoWrap;
            titleText.overflowMode = TextOverflowModes.Ellipsis;
        }

        private static string Title() => PartyClientState.Name.Length > 0 ? PartyClientState.Name : "Party";

        /// <summary>First-run default: upper-middle of the screen, clear of the hotbar, in canvas units.</summary>
        private static void EnsurePosition()
        {
            if (PartyConfig.PanelY.Value < 0f)
                PartyConfig.PanelY.Value = ((RectTransform)canvasRoot.transform).rect.height * 0.35f;
        }

        private static void ApplyLayout(int rowCount)
        {
            EnsurePosition();
            float padding = PartyConfig.PanelPadding.Value;
            float width = PartyConfig.BarWidth.Value + padding * 2f;
            float titleHeight = PartyConfig.TitleFontSize.Value + padding;
            float height = titleHeight + rowCount * (HealthPanelLayout.RowHeight() + PartyConfig.RowSpacing.Value) + padding;

            if (!PartyDragHandler.Dragging)
                panelRect.anchoredPosition = new Vector2(PartyConfig.PanelX.Value, -PartyConfig.PanelY.Value);
            panelRect.sizeDelta = new Vector2(width, height);
            panelRect.localScale = Vector3.one * Mathf.Max(0.5f, PartyConfig.PanelScale.Value);
            canvasGroup.alpha = PartyConfig.PanelOpacity.Value;
            canvasGroup.blocksRaycasts = EditMode;
            ApplyTitle(width, padding, titleHeight);
        }

        private static void ApplyTitle(float width, float padding, float titleHeight)
        {
            RectTransform titleRect = titleText.GetComponent<RectTransform>();
            titleRect.anchoredPosition = new Vector2(padding, -padding * 0.5f);
            titleRect.sizeDelta = new Vector2(width - padding * 2f, titleHeight);
            titleText.fontSize = PartyConfig.TitleFontSize.Value;
            titleText.text = EditMode ? Title() + "  (drag me, Esc when done)" : Title();
        }

        /// <summary>Rows bake sizes at construction; when a layout setting changes they are rebuilt, not patched.</summary>
        private static void EnsureRowsMatchSettings()
        {
            string signature = $"{PartyConfig.FontSize.Value}|{PartyConfig.BarWidth.Value}|{PartyConfig.BarHeight.Value}|" +
                               $"{PartyConfig.ShowStamina.Value}|{PartyConfig.ShowEitr.Value}";
            if (signature == lastRowSignature)
                return;
            lastRowSignature = signature;
            foreach (PartyRowView row in rows)
                Object.Destroy(row.Root);
            rows.Clear();
        }

        /// <summary>
        /// Who gets a row this frame. The local player's row is the live Player when there is one; while dead
        /// (no Player object) their roster entry stands in, so the counts always match what is placed.
        /// </summary>
        private static List<(PartyMemberView member, float? distance)> RowContents()
        {
            List<(PartyMemberView, float?)> content = new List<(PartyMemberView, float?)>();
            Player local = Player.m_localPlayer;
            long selfId = Identity.LocalPlayerId;
            if (PartyConfig.ShowOwnRow.Value && local != null)
                content.Add((SelfRow(local), null));
            foreach (PartyMemberView member in PartyClientState.Members)
            {
                if (member.Id == selfId && !(PartyConfig.ShowOwnRow.Value && local == null))
                    continue;
                float? distance = member.PositionValid && local != null
                    ? Vector3.Distance(local.transform.position, member.Position)
                    : (float?)null;
                content.Add((member, distance));
            }
            return content;
        }

        private static void UpdateRows(List<(PartyMemberView member, float? distance)> content)
        {
            EnsureRowCount(content.Count, PartyConfig.BarWidth.Value);
            float padding = PartyConfig.PanelPadding.Value;
            float titleHeight = PartyConfig.TitleFontSize.Value + padding;
            float rowStep = HealthPanelLayout.RowHeight() + PartyConfig.RowSpacing.Value;
            for (int i = 0; i < content.Count; i++)
            {
                RectTransform rect = rows[i].Root.GetComponent<RectTransform>();
                rect.anchoredPosition = new Vector2(padding, -(titleHeight + i * rowStep));
                rows[i].Apply(content[i].member, content[i].distance);
            }
        }

        private static void EnsureRowCount(int needed, float width)
        {
            while (rows.Count < needed)
                rows.Add(new PartyRowView(panelRect, width));
            while (rows.Count > needed)
            {
                Object.Destroy(rows[rows.Count - 1].Root);
                rows.RemoveAt(rows.Count - 1);
            }
        }

        private static PartyMemberView SelfRow(Player local)
        {
            return new PartyMemberView
            {
                Id = Identity.LocalPlayerId,
                Name = Identity.LocalPlayerName,
                Online = true,
                Health = Fraction(local.GetHealth(), local.GetMaxHealth()),
                Stamina = Fraction(local.GetStamina(), local.GetMaxStamina()),
                Eitr = Fraction(local.GetEitr(), local.GetMaxEitr()),
            };
        }

        private static float Fraction(float value, float max) => max > 0f ? Mathf.Clamp01(value / max) : 0f;
    }
}
