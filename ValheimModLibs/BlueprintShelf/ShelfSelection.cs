using System;
using System.Collections.Generic;
using System.Linq;

namespace BlueprintShelf;

/// <summary>
/// The picked lines of one list, as in a file explorer: a click picks one, Ctrl + click adds or drops one, Shift + click
/// picks the range from the last one clicked (Ctrl + Shift adds the range). The way up is never picked. Kept by line key,
/// so a refill keeps what is still listed.
/// </summary>
internal sealed class ShelfSelection
{
	private readonly HashSet<string> keys = new();
	private string? anchor;

	public int Count => keys.Count;

	/// <summary>Goes up with every change, so the view repaints only then.</summary>
	public int Version { get; private set; }

	public bool Has(ShelfLine entry) => keys.Contains(entry.Key);

	public List<ShelfLine> Of(List<ShelfLine> entries) => entries.Where(Has).ToList();

	public void Clear()
	{
		keys.Clear();
		anchor = null;
		Version++;
	}

	/// <summary>Picks this line alone (a drag starting on a line that was not picked).</summary>
	public void Only(ShelfLine entry)
	{
		keys.Clear();
		if (entry.Kind != ShelfKind.Up)
		{
			keys.Add(entry.Key);
		}
		anchor = entry.Key;
		Version++;
	}

	public void Click(List<ShelfLine> entries, int index, bool ctrl, bool shift)
	{
		ShelfLine entry = entries[index];
		int from = anchor == null ? -1 : entries.FindIndex(e => e.Key == anchor);
		if (!ctrl)
		{
			keys.Clear();
		}
		if (shift && from >= 0)
		{
			AddRange(entries, from, index);
		}
		else
		{
			if (entry.Kind != ShelfKind.Up && !keys.Remove(entry.Key))
			{
				keys.Add(entry.Key);
			}
			anchor = entry.Key;
		}
		Version++;
	}

	private void AddRange(List<ShelfLine> entries, int from, int to)
	{
		for (int i = Math.Min(from, to); i <= Math.Max(from, to); i++)
		{
			if (entries[i].Kind != ShelfKind.Up)
			{
				keys.Add(entries[i].Key);
			}
		}
	}

	/// <summary>After a refill: lines no longer listed are dropped.</summary>
	public void Keep(List<ShelfLine> entries)
	{
		HashSet<string> listed = new HashSet<string>(entries.Select(e => e.Key));
		if (keys.RemoveWhere(k => !listed.Contains(k)) > 0)
		{
			Version++;
		}
		if (anchor != null && !listed.Contains(anchor))
		{
			anchor = null;
		}
	}
}
