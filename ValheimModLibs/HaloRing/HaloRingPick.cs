namespace HaloRing;

/// <summary>How a release ended.</summary>
public enum RingOutcome
{
	/// <summary>The ring closed on the hovered item.</summary>
	Picked,

	/// <summary>The hovered item is disabled: it shook and the ring is still open.</summary>
	Refused,

	/// <summary>The ring closed with nothing hovered (the centre), or was not open.</summary>
	Cancelled,
}

/// <summary>What <see cref="HaloRingMenu.Release"/> came to: the outcome, and the item and segment it was on.</summary>
public readonly struct HaloRingPick
{
	public readonly RingOutcome Outcome;
	public readonly HaloRingItem? Item;
	public readonly int Index;

	public HaloRingPick(RingOutcome outcome, HaloRingItem? item, int index)
	{
		Outcome = outcome;
		Item = item;
		Index = index;
	}

	public static HaloRingPick None => new HaloRingPick(RingOutcome.Cancelled, null, -1);
}
