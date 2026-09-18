namespace HaloMenu.API
{
    /// <summary>How a ring's hotkey opens and closes it.</summary>
    public enum ActivationMode
    {
        /// <summary>Hold the hotkey to keep the ring open; release to select whatever is highlighted, or cancel.</summary>
        Hold,

        /// <summary>Press to open; press again or left-click to select; right-click or Escape to cancel.</summary>
        Toggle,
    }
}
