namespace HaloMenu.API
{
    /// <summary>Return false to block the ring from opening.</summary>
    public delegate bool RingOpeningHandler(Ring ring);

    public delegate void RingHandler(Ring ring);

    public delegate void HighlightChangedHandler(Ring ring, int index);

    /// <summary>Return false to cancel this selection: the ring still closes, but as a cancel, and OnSelect never fires.</summary>
    public delegate bool SelectingHandler(Ring ring, RingEntry entry);

    public delegate void SelectedHandler(Ring ring, RingEntry entry);
}
