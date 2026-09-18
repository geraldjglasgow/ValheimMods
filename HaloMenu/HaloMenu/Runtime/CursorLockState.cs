using UnityEngine;

namespace HaloMenu.Runtime
{
    /// <summary>Saves ZCursor's lock state and visibility once when a ring opens, restores exactly that on close.
    /// A single global instance is enough: only one ring is ever open at a time (<see cref="RingRegistry"/>).</summary>
    public static class CursorLockState
    {
        private static CursorLockMode savedLockState;
        private static bool savedVisible;
        private static bool saved;

        public static void Suspend()
        {
            if (saved)
                return;
            savedLockState = ZCursor.LockState;
            savedVisible = ZCursor.IsVisible;
            saved = true;
        }

        public static void Restore()
        {
            if (!saved)
                return;
            saved = false;
            ZCursor.LockState = savedLockState;
            if (savedVisible)
                ZCursor.Show();
            else
                ZCursor.Hide();
        }
    }
}
