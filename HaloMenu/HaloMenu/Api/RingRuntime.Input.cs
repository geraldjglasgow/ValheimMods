using HaloMenu.API;
using HaloMenu.Runtime;
using UnityEngine;

namespace HaloMenu.Api
{
    // Hold vs Toggle close/select/cancel handling.
    public sealed partial class RingRuntime
    {
        private bool HandleCloseInput()
        {
            // In game Esc reaches the ring through the menu (RingWindow), so it cancels the ring and never also opens
            // the menu; only without a game menu (the start screen) does the ring read Esc itself.
            if (Menu.instance == null && Input.GetKeyDown(KeyCode.Escape))
            {
                Close(cancel: true);
                return true;
            }
            return settings.ActivationMode.Value == HaloMenu.API.ActivationMode.Hold ? HandleHoldClose() : HandleToggleClose();
        }

        private bool HandleHoldClose()
        {
            if (InputSource.Held(settings.Hotkey))
                return false;
            return TrySelectOrShake();
        }

        private bool HandleToggleClose()
        {
            if (Input.GetMouseButtonDown(1))
            {
                RingWindow.SwallowUntilReleased(1);
                Close(cancel: true);
                return true;
            }
            if (Input.GetMouseButtonDown(0))
            {
                RingWindow.SwallowUntilReleased(0);
                return TrySelectOrShake();
            }
            return InputSource.Pressed(settings.Hotkey) && TrySelectOrShake();
        }

        /// <summary>A disabled highlighted entry shakes and refuses; the ring stays open. Returns true when the
        /// ring closed (selected or cancelled) this call.</summary>
        private bool TrySelectOrShake()
        {
            if (highlightedIndex.HasValue && !slots.IsEnabledAt(highlightedIndex.Value))
            {
                view.ShakeSlot(highlightedIndex.Value);
                return false;
            }
            Close(cancel: false);
            return true;
        }
    }
}
