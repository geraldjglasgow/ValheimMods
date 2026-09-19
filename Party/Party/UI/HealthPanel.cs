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
    /// event system) instead of OnGUI. Ticked once a frame from <see cref="Client.PartyTicker"/>.
    /// </summary>
    public static class HealthPanel
    {
        public static bool EditMode { get; private set; }

        private static GameObject canvasRoot;
        private static RectTransform panelRect;
        private static CanvasGroup canvasGroup;
        private static TMP_Text titleText;
        private static readonly List<PartyRowView> rows = new List<PartyRowView>();

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

        public static bool CanDrag() => EditMode || Cursor.lockState == CursorLockMode.None;

        public static void PersistPosition(Vector2 anchoredPosition)
        {
            PartyConfig.PanelX.Value = anchoredPosition.x;
            PartyConfig.PanelY.Value = -anchoredPosition.y;
        }

        public static void Tick()
        {
            bool shouldShow = PartyClientState.InParty || EditMode;
            EnsureBuilt();
            canvasRoot.SetActive(shouldShow);
            if (!shouldShow)
                return;
            ApplyLayout();
            UpdateContent();
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
            canvasRoot.GetComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;

            GameObject panelGo = new GameObject("Panel", typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
            panelGo.transform.SetParent(canvasRoot.transform, false);
            panelRect = panelGo.GetComponent<RectTransform>();
            panelRect.anchorMin = panelRect.anchorMax = panelRect.pivot = new Vector2(0f, 1f);
            canvasGroup = panelGo.GetComponent<CanvasGroup>();

            Image background = panelGo.GetComponent<Image>();
            background.sprite = RoundedSprite.Get();
            background.type = Image.Type.Sliced;
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
        }

        private static string Title() => PartyClientState.Name.Length > 0 ? PartyClientState.Name : "Party";

        /// <summary>First-run default: upper-middle of the screen, clear of the hotbar, independent of resolution.</summary>
        private static void EnsurePosition()
        {
            if (PartyConfig.PanelY.Value < 0f)
                PartyConfig.PanelY.Value = Screen.height * 0.35f;
        }

        private static void ApplyLayout()
        {
            EnsurePosition();
            float padding = PartyConfig.PanelPadding.Value;
            float width = PartyConfig.BarWidth.Value + padding * 2f;
            float titleHeight = PartyConfig.TitleFontSize.Value + padding;
            int rowCount = RowCount();
            float height = titleHeight + rowCount * (HealthPanelLayout.RowHeight() + PartyConfig.RowSpacing.Value) + padding;

            panelRect.anchoredPosition = new Vector2(PartyConfig.PanelX.Value, -PartyConfig.PanelY.Value);
            panelRect.sizeDelta = new Vector2(width, height);
            panelRect.localScale = Vector3.one * Mathf.Max(0.5f, PartyConfig.PanelScale.Value);
            canvasGroup.alpha = PartyConfig.PanelOpacity.Value;

            RectTransform titleRect = titleText.GetComponent<RectTransform>();
            titleRect.anchoredPosition = new Vector2(padding, -padding * 0.5f);
            titleRect.sizeDelta = new Vector2(width - padding * 2f, titleHeight);
            titleText.fontSize = PartyConfig.TitleFontSize.Value;
            titleText.text = CanDrag() ? Title() + " (drag me)" : Title();
        }

        private static int RowCount()
        {
            int otherCount = System.Math.Max(0, PartyClientState.Members.Count - 1);
            return (PartyConfig.ShowOwnRow.Value ? 1 : 0) + otherCount;
        }

        private static void UpdateContent()
        {
            float padding = PartyConfig.PanelPadding.Value;
            float width = PartyConfig.BarWidth.Value;
            float titleHeight = PartyConfig.TitleFontSize.Value + padding;
            EnsureRowCount(RowCount(), width);

            int index = 0;
            float rowStep = HealthPanelLayout.RowHeight() + PartyConfig.RowSpacing.Value;
            if (PartyConfig.ShowOwnRow.Value)
                PlaceRow(index++, SelfRow(), null, padding, titleHeight, rowStep);
            Vector3 localPos = Player.m_localPlayer != null ? Player.m_localPlayer.transform.position : Vector3.zero;
            foreach (PartyMemberView member in PartyClientState.Members)
            {
                if (member.Id == Identity.LocalPlayerId)
                    continue;
                float? distance = member.PositionValid ? Vector3.Distance(localPos, member.Position) : (float?)null;
                PlaceRow(index++, member, distance, padding, titleHeight, rowStep);
            }
        }

        private static void PlaceRow(int index, PartyMemberView member, float? distance, float padding, float titleHeight, float rowStep)
        {
            PartyRowView row = rows[index];
            RectTransform rect = row.Root.GetComponent<RectTransform>();
            rect.anchoredPosition = new Vector2(padding, -(titleHeight + index * rowStep));
            row.Apply(member, distance);
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

        private static PartyMemberView SelfRow()
        {
            Player local = Player.m_localPlayer;
            return new PartyMemberView
            {
                Id = Identity.LocalPlayerId,
                Name = Identity.LocalPlayerName,
                Online = true,
                Health = local != null && local.GetMaxHealth() > 0 ? local.GetHealth() / local.GetMaxHealth() : 1f,
                Stamina = local != null && local.GetMaxStamina() > 0 ? local.GetStamina() / local.GetMaxStamina() : 1f,
                Eitr = local != null && local.GetMaxEitr() > 0 ? local.GetEitr() / local.GetMaxEitr() : 1f,
            };
        }
    }
}
