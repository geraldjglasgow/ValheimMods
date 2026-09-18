using System.Collections.Generic;
using HaloMenu.API;
using HaloMenu.Config;
using HaloMenu.Layout;
using HaloMenu.Rendering;
using HaloMenu.Runtime;
using UnityEngine;

namespace HaloMenu.Api
{
    /// <summary>
    /// The engine behind one ring: entries, the FSM (Closed/Open), input, layout caching and the view. Implements
    /// <see cref="HaloMenu.API.Ring"/> directly (see RingRuntime.ApiSurface.cs) - there is no separate wrapper
    /// type, so an event handler that receives "the Ring" gets this same object.
    /// </summary>
    public sealed partial class RingRuntime
    {
        private readonly string id;
        private readonly RingSettings settings;
        private readonly RingView view;
        private readonly HysteresisSelector selector = new HysteresisSelector();
        private readonly List<RingEntry> entries = new List<RingEntry>();

        private RingState state = RingState.Closed;
        private RingLayout layout;
        private SlotAssignment slots;
        private int? highlightedIndex;
        private int pooledSegmentCount = -1;
        private bool layoutDirty = true;
        private float lastGameUiScale = -1f;

        public RingRuntime(string id, RingSettings settings)
        {
            this.id = id;
            this.settings = settings;
            view = new RingView(id);
            SubscribeLayoutInvalidation();
            EnsureLayout();
        }

        public override void Add(RingEntry entry)
        {
            int existing = entries.FindIndex(e => e.Id == entry.Id);
            if (existing >= 0)
            {
                HaloLog.Warning($"Ring '{id}': entry '{entry.Id}' registered again, replacing the prior one.");
                entries[existing] = entry;
            }
            else
            {
                entries.Add(entry);
            }
        }

        public override void Remove(string entryId) => entries.RemoveAll(e => e.Id == entryId);

        /// <summary>Called once a frame by the HaloMenuDriver for every ring, open or closed.</summary>
        public void TickInput(float deltaTime)
        {
            if (state == RingState.Closed)
            {
                if (InputSource.Pressed(settings.Hotkey) && !BlockingUiWatcher.IsBlocked())
                    Open();
                return;
            }
            if (BlockingUiWatcher.IsBlocked())
            {
                Close(cancel: true);
                return;
            }
            if (HandleCloseInput())
                return;
            UpdateSelection();
            UpdateView(deltaTime);
        }

        /// <summary>RingRegistry closes every other ring, as a cancel, before a new one opens.</summary>
        public void CancelFromOutside() => Close(cancel: true);

        private void Open()
        {
            RingRegistry.NotifyOpening(this);
            if (!RaiseOpening())
            {
                RingRegistry.NotifyClosed(this);
                return;
            }
            state = RingState.Open;
            EnsureLayout();
            slots = SlotAssignment.Build(entries, layout.SegmentCount);
            selector.Reset();
            highlightedIndex = null;
            CursorLockState.Suspend();
            view.ApplySlots(slots);
            view.Show();
            OnRingOpened?.Invoke(this);
        }

        private void Close(bool cancel)
        {
            if (state == RingState.Closed)
                return;
            state = RingState.Closed;
            view.Hide();
            CursorLockState.Restore();
            RingRegistry.NotifyClosed(this);
            if (cancel)
            {
                OnCancelled?.Invoke(this);
                return;
            }
            FinishSelection();
        }

        private void FinishSelection()
        {
            RingEntry entry = highlightedIndex.HasValue ? slots.EntryAt(highlightedIndex.Value) : null;
            if (entry == null)
            {
                OnCancelled?.Invoke(this);
                return;
            }
            if (!RaiseSelecting(entry))
            {
                OnCancelled?.Invoke(this);
                return;
            }
            OnSelected?.Invoke(this, entry);
            entry.OnSelect?.Invoke();
        }

        private void UpdateSelection()
        {
            Vector2 offset = InputSource.SelectionOffset(settings.GamepadEnabled.Value, settings.GamepadStick.Value, layout.OuterRadius);
            int? raw = selector.Update(offset, layout.SegmentCount, layout.StartAngleOffset, layout.DeadZoneRadius, Geometry.HysteresisDegrees);
            int? newHighlight = raw.HasValue && slots.IsFilled(raw.Value) ? raw : null;
            if (newHighlight == highlightedIndex)
                return;
            highlightedIndex = newHighlight;
            if (highlightedIndex.HasValue)
                OnHighlightChanged?.Invoke(this, highlightedIndex.Value);
        }

        private void UpdateView(float deltaTime)
        {
            HoverVisualConfig cfg = ReadVisualConfig();
            view.Tick(highlightedIndex, slots, cfg, settings.ShowCenterLabel.Value, deltaTime);
        }

        private HoverVisualConfig ReadVisualConfig()
        {
            Color baseColor = ParseColor(settings.SegmentColor.Value, new Color(0.08f, 0.08f, 0.09f, 0.8f));
            Color highlightColor = ParseColor(settings.HighlightColor.Value, new Color(0.83f, 0.68f, 0.21f, 1f));
            return new HoverVisualConfig(settings.HoverScale.Value, settings.AnimationDuration.Value, baseColor, highlightColor);
        }

        private static Color ParseColor(string hex, Color fallback) =>
            ColorUtility.TryParseHtmlString(hex, out Color c) ? c : fallback;
    }
}
