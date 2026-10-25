using System;
using System.Collections.Generic;
using System.Linq;

namespace BlueprintShelf;

/// <summary>The game's own yes / no box (UnifiedPopup) before a delete or a remove, and the picked names it lists.</summary>
internal static class ShelfConfirm
{
	public static void Ask(Shelf shelf, string header, string text, Action yes)
	{
		if (!UnifiedPopup.IsAvailable())
		{
			return;
		}
		UnifiedPopup.Push(new YesNoPopup(header, text, () =>
		{
			UnifiedPopup.Pop();
			shelf.Run("confirm", yes);
		}, UnifiedPopup.Pop, localizeText: false));
	}

	/// <summary>"Barn, Houses, Hut" or "Barn, Houses, Hut (+4)".</summary>
	public static string Names(List<ShelfLine> picked)
	{
		string first = string.Join(", ", picked.Take(3).Select(e => e.Name));
		return picked.Count > 3 ? first + " (+" + (picked.Count - 3) + ")" : first;
	}
}
