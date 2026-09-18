using System.Collections.Generic;
using HaloMenu.Layout;
using HaloMenu.Runtime;
using UnityEngine;
using UnityEngine.UI;

namespace HaloMenu.Rendering
{
    /// <summary>
    /// One ring's on-screen presence: a dedicated Canvas, a pooled row of <see cref="SegmentView"/> and the center
    /// label. The GameObject hierarchy is built once (<see cref="Rebuild"/>, only on a segment count change) and
    /// activated/deactivated on open/close - never Instantiate or Destroy on open or close.
    /// </summary>
    public sealed class RingView
    {
        private const int SortingOrder = 100; // above Hud, below modal dialogs; not verified in a live session, see PLAN.md

        private readonly GameObject canvasObject;
        private readonly RectTransform root;
        private readonly Text centerLabel;
        private readonly List<SegmentView> segments = new List<SegmentView>();

        public RingView(string ringId)
        {
            canvasObject = new GameObject($"HaloMenu Ring [{ringId}]");
            Object.DontDestroyOnLoad(canvasObject);
            Canvas canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = SortingOrder;
            CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
            scaler.scaleFactor = 1f;
            root = (RectTransform)canvasObject.transform;
            centerLabel = BuildCenterLabel(root);
            canvasObject.SetActive(false);
        }

        public void Show() => canvasObject.SetActive(true);

        public void Hide() => canvasObject.SetActive(false);

        public void Rebuild(int segmentCount)
        {
            foreach (SegmentView old in segments)
                Object.Destroy(old.gameObject);
            segments.Clear();
            for (int i = 0; i < segmentCount; i++)
                segments.Add(SegmentView.Create(root));
        }

        public void ApplyLayout(RingLayout layout)
        {
            for (int i = 0; i < segments.Count && i < layout.Segments.Length; i++)
                segments[i].SetGeometry(layout.Segments[i], layout.InnerRadius, layout.OuterRadius);
        }

        public void ApplySlots(SlotAssignment slots)
        {
            for (int i = 0; i < segments.Count; i++)
                segments[i].SetContent(slots.IconAt(i), slots.IsFilled(i));
        }

        public void Tick(int? highlightedIndex, SlotAssignment slots, HoverVisualConfig cfg, bool showCenterLabel, float deltaTime)
        {
            for (int i = 0; i < segments.Count; i++)
            {
                bool highlighted = highlightedIndex == i && slots.IsFilled(i);
                segments[i].Tick(highlighted, slots.IsEnabledAt(i), cfg, deltaTime);
            }
            UpdateCenterLabel(highlightedIndex, slots, showCenterLabel);
        }

        public void ShakeSlot(int index)
        {
            if (index >= 0 && index < segments.Count)
                segments[index].TriggerShake();
        }

        private void UpdateCenterLabel(int? highlightedIndex, SlotAssignment slots, bool showCenterLabel)
        {
            string label = showCenterLabel && highlightedIndex.HasValue ? slots.LabelAt(highlightedIndex.Value) : null;
            centerLabel.gameObject.SetActive(!string.IsNullOrEmpty(label));
            centerLabel.text = label ?? string.Empty;
        }

        private static Text BuildCenterLabel(Transform parent)
        {
            GameObject go = new GameObject("CenterLabel", typeof(RectTransform));
            RectTransform rect = (RectTransform)go.transform;
            rect.SetParent(parent, worldPositionStays: false);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(300f, 60f);
            Text text = go.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.alignment = TextAnchor.MiddleCenter;
            text.fontSize = 20;
            text.color = Color.white;
            text.raycastTarget = false;
            return text;
        }
    }
}
