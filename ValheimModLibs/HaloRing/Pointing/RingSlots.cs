using System;
using System.Collections.Generic;

namespace HaloRing.Pointing;

/// <summary>
/// The item in each of an open ring's segments, fixed as it opens: the caller's list in segment order (a null is an
/// empty segment), cut at the segment count. Whether an item is enabled is asked again every frame.
/// </summary>
internal sealed class RingSlots
{
	private readonly HaloRingItem?[] slots;
	private readonly Action<string>? report;

	private RingSlots(HaloRingItem?[] slots, Action<string>? report)
	{
		this.slots = slots;
		this.report = report;
	}

	public static RingSlots Build(IReadOnlyList<HaloRingItem?> items, int segmentCount, Action<string>? report)
	{
		var slots = new HaloRingItem?[segmentCount];
		for (int i = 0; i < segmentCount && i < items.Count; i++)
			slots[i] = items[i];
		return new RingSlots(slots, report);
	}

	public bool IsFilled(int index) => index >= 0 && index < slots.Length && slots[index] != null;

	public HaloRingItem? At(int index) => IsFilled(index) ? slots[index] : null;

	public bool IsEnabledAt(int index)
	{
		HaloRingItem? item = At(index);
		return item != null && Enabled(item);
	}

	private bool Enabled(HaloRingItem item)
	{
		if (item.IsEnabled == null)
			return item.Enabled;
		try
		{
			return item.IsEnabled();
		}
		catch (Exception ex)
		{
			report?.Invoke($"A ring entry predicate threw, treating as True: {ex}");
			return true;
		}
	}
}
