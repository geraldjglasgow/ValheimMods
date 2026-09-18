using BepInEx.Configuration;

namespace HaloMenu.API
{
    /// <summary>
    /// One radial menu: its own hotkey, config subsection and layout. Only one ring across the whole game may be
    /// open at a time; opening a second closes whichever ring was open with a cancel. Get one from
    /// <see cref="HaloMenuAPI.CreateRing"/>, or use <see cref="HaloMenuAPI.Register"/> for the shared default ring.
    /// </summary>
    public abstract class Ring
    {
        /// <summary>The id this ring was created with ("halomenu.default" for the built-in ring).</summary>
        public abstract string Id { get; }

        /// <summary>Bound to this ring's own config subsection by default; assign a different entry to share a
        /// hotkey with something else.</summary>
        public abstract ConfigEntry<KeyboardShortcut> Hotkey { get; set; }

        public abstract ActivationMode ActivationMode { get; set; }

        /// <summary>2-16. Entries beyond this many are dropped, not wrapped to a second ring.</summary>
        public abstract int SegmentCount { get; set; }

        public abstract bool GamepadEnabled { get; set; }

        public abstract GamepadStick GamepadStick { get; set; }

        /// <summary>Adds or replaces (by Id) an entry.</summary>
        public abstract void Add(RingEntry entry);

        public abstract void Remove(string entryId);

        public abstract event RingOpeningHandler OnRingOpening;
        public abstract event RingHandler OnRingOpened;
        public abstract event HighlightChangedHandler OnHighlightChanged;
        public abstract event SelectingHandler OnSelecting;
        public abstract event SelectedHandler OnSelected;
        public abstract event RingHandler OnCancelled;
    }
}
