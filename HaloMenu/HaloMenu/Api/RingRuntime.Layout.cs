using HaloMenu.Layout;
using HaloMenu.Runtime;
using UnityEngine;

namespace HaloMenu.Api
{
    // Layout caching: rebuilt on a Layout/UIScale setting change or a screen-scale change, checked only when the
    // ring is about to open, never per frame and never merely because it opened with nothing to rebuild.
    public sealed partial class RingRuntime
    {
        private void EnsureLayout()
        {
            float gameUiScale = GameUiScale.Factor();
            bool scaleChanged = !Mathf.Approximately(gameUiScale, lastGameUiScale);
            if (!layoutDirty && !scaleChanged && layout != null)
                return;
            layoutDirty = false;
            lastGameUiScale = gameUiScale;
            float screenScale = Geometry.ScreenScale(settings.UIScale.Value, gameUiScale);
            layout = RingLayout.Build(settings, screenScale);
            RebuildPoolIfSegmentCountChanged();
            view.ApplyLayout(layout);
        }

        private void RebuildPoolIfSegmentCountChanged()
        {
            if (layout.SegmentCount == pooledSegmentCount)
                return;
            view.Rebuild(layout.SegmentCount);
            pooledSegmentCount = layout.SegmentCount;
        }

        private void SubscribeLayoutInvalidation()
        {
            settings.SegmentCount.SettingChanged += MarkLayoutDirty;
            settings.InnerRadius.SettingChanged += MarkLayoutDirty;
            settings.OuterRadius.SettingChanged += MarkLayoutDirty;
            settings.GapDegrees.SettingChanged += MarkLayoutDirty;
            settings.StartAngleOffset.SettingChanged += MarkLayoutDirty;
            settings.DeadZoneRadius.SettingChanged += MarkLayoutDirty;
            settings.IconPadding.SettingChanged += MarkLayoutDirty;
            settings.MaxIconSize.SettingChanged += MarkLayoutDirty;
            settings.UIScale.SettingChanged += MarkLayoutDirty;
        }

        private void MarkLayoutDirty(object sender, System.EventArgs e) => layoutDirty = true;
    }
}
