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
            if (Input.GetKeyDown(KeyCode.Escape))
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
                Close(cancel: true);
                return true;
            }
            if (Input.GetMouseButtonDown(0) || InputSource.Pressed(settings.Hotkey))
                return TrySelectOrShake();
            return false;
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
