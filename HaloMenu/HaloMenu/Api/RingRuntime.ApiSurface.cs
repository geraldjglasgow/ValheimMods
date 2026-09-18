using BepInEx.Configuration;
using HaloMenu.API;

namespace HaloMenu.Api
{
    // The HaloMenu.API.Ring surface this type implements. Split from RingRuntime.cs (the engine) to keep each
    // file focused; both halves are the same class.
    public sealed partial class RingRuntime : Ring
    {
        public override string Id => id;

        public override ConfigEntry<KeyboardShortcut> Hotkey
        {
            get => settings.Hotkey;
            set => settings.Hotkey = value;
        }

        public override ActivationMode ActivationMode
        {
            get => settings.ActivationMode.Value;
            set => settings.ActivationMode.Value = value;
        }

        public override int SegmentCount
        {
            get => settings.SegmentCount.Value;
            set => settings.SegmentCount.Value = value;
        }

        public override bool GamepadEnabled
        {
            get => settings.GamepadEnabled.Value;
            set => settings.GamepadEnabled.Value = value;
        }

        public override GamepadStick GamepadStick
        {
            get => settings.GamepadStick.Value;
            set => settings.GamepadStick.Value = value;
        }

        public override event RingOpeningHandler OnRingOpening;
        public override event RingHandler OnRingOpened;
        public override event HighlightChangedHandler OnHighlightChanged;
        public override event SelectingHandler OnSelecting;
        public override event SelectedHandler OnSelected;
        public override event RingHandler OnCancelled;

        /// <summary>False from any subscriber blocks the open.</summary>
        private bool RaiseOpening()
        {
            if (OnRingOpening == null)
                return true;
            foreach (RingOpeningHandler handler in OnRingOpening.GetInvocationList())
            {
                if (!InvokeGuarded(handler, this))
                    return false;
            }
            return true;
        }

        /// <summary>False from any subscriber turns this selection into a cancel.</summary>
        private bool RaiseSelecting(RingEntry entry)
        {
            if (OnSelecting == null)
                return true;
            foreach (SelectingHandler handler in OnSelecting.GetInvocationList())
            {
                if (!InvokeGuarded(handler, this, entry))
                    return false;
            }
            return true;
        }

        private static bool InvokeGuarded(RingOpeningHandler handler, Ring ring)
        {
            try { return handler(ring); }
            catch (System.Exception ex) { HaloLog.Error($"OnRingOpening handler threw: {ex}"); return true; }
        }

        private static bool InvokeGuarded(SelectingHandler handler, Ring ring, RingEntry entry)
        {
            try { return handler(ring, entry); }
            catch (System.Exception ex) { HaloLog.Error($"OnSelecting handler threw: {ex}"); return true; }
        }
    }
}
