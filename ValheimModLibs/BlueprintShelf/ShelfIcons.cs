using System.Collections.Generic;
using PlateColumn;
using UnityEngine;

namespace BlueprintShelf;

/// <summary>
/// The library's own line icons, one embedded 128 px PNG each (icons/*.png, OpenKeep's folder, way up and blueprint),
/// loaded once: a folder (a player's part of the pool too), the way up, a blueprint. A host's icons go first.
/// </summary>
internal static class ShelfIcons
{
	private static readonly Dictionary<ShelfKind, Sprite?> loaded = new();

	public static Sprite? Get(ShelfKind kind)
	{
		if (loaded.TryGetValue(kind, out Sprite? known) && known != null)
		{
			return known;
		}
		string name = kind == ShelfKind.Up ? "folderup" : kind == ShelfKind.File ? "blueprint" : "folder";
		Sprite? sprite = EmbeddedSprite.Load(typeof(ShelfIcons).Assembly, "BlueprintShelf.icons." + name + ".png", "BlueprintShelf_" + name);
		loaded[kind] = sprite;
		return sprite;
	}
}
