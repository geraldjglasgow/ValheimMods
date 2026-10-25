using System;
using System.Collections.Generic;

namespace BlueprintShelf;

/// <summary>The kinds of line in a shelf's lists, each with its icon.</summary>
public enum ShelfKind
{
	Up,
	Folder,
	File,
	Owner,
}

/// <summary>One line of a list: the way up, a folder, a blueprint, or (the pool's top list) a player's part.</summary>
internal sealed class ShelfLine
{
	public ShelfKind Kind;

	/// <summary>The folder's or blueprint's path from the top of the player's files or of the player's part; the way up: the folder it opens.</summary>
	public string Path = "";

	/// <summary>The player whose part of the pool it is (0 in the player's files, and for the way up to the list of players).</summary>
	public long Owner;

	/// <summary>What the line shows (it may carry rich text).</summary>
	public string Label = "";

	/// <summary>The plain name, for messages.</summary>
	public string Name = "";

	/// <summary>Names the line across refills (the selection).</summary>
	public string Key => (int)Kind + "|" + Owner + "|" + Path;

	/// <summary>It opens and takes drops: everything but a blueprint.</summary>
	public bool Holds => Kind != ShelfKind.File;

	public static ShelfLine Up(long owner, string path) =>
		new ShelfLine { Kind = ShelfKind.Up, Owner = owner, Path = path, Label = "..", Name = ".." };

	public static ShelfLine Of(ShelfKind kind, long owner, string path, Func<string, string> title)
	{
		string name = title(ShelfPaths.Leaf(path));
		return new ShelfLine { Kind = kind, Owner = owner, Path = path, Label = name, Name = name };
	}
}

/// <summary>
/// One side of the window (the player's files, or the shared pool): where it is, what it lists, its buttons, and what
/// opening, renaming, removing and dropping do there. The view (<see cref="PaneView"/>) draws it.
/// </summary>
internal interface IShelfPane
{
	string Header { get; }
	string Location { get; }
	List<ShelfLine> Entries { get; }
	ShelfSelection Selection { get; }

	/// <summary>Goes up whenever <see cref="Entries"/> changed.</summary>
	int Version { get; }

	List<ShelfButton> Actions { get; }

	/// <summary>Per frame while the window is open: reads its source again and refills when it changed.</summary>
	void Tick();

	/// <summary>A double click on a folder, a player or the way up.</summary>
	void Open(ShelfLine entry);

	/// <summary>The rename key or a right click on a line.</summary>
	void Rename(ShelfLine entry);

	/// <summary>The Delete key: the selection.</summary>
	void Remove();

	/// <summary>Lines from <paramref name="from"/> may be dropped on <paramref name="onto"/> (null: the folder shown).</summary>
	bool Accepts(IShelfPane from, ShelfLine? onto);

	void Drop(IShelfPane from, List<ShelfLine> dragged, ShelfLine? onto);
}
