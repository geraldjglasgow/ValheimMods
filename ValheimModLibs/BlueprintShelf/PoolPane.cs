using System.Collections.Generic;
using System.Linq;

namespace BlueprintShelf;

/// <summary>
/// The window's right side: the world's shared pool as a file explorer. At the top, everyone who shared (the local player
/// first, always listed so they can make folders before sharing); inside a player's part, the way up, their folders (empty
/// ones too), their blueprints. Take a copy (or a drag onto the player's side) copies the picks into the folder the
/// player's side shows (a folder with what is inside it, a player's whole part under their name). The sharer or an admin
/// makes folders, renames, removes (asked first) and moves by dragging onto a folder (<see cref="PoolEdits"/>); files
/// dragged in from the player's side are shared into their own part only. The server checks every request again.
/// </summary>
internal sealed class PoolPane : IShelfPane
{
	private readonly Shelf shelf;
	private readonly PoolEdits edits;
	private int seenPool = -1;
	private long seenOwner = -1L;
	private string? seenFolder;

	public PoolPane(Shelf shelf)
	{
		this.shelf = shelf;
		edits = new PoolEdits(shelf, this);
		Actions = new List<ShelfButton>
		{
			new(ShelfWord.Take, () => Selection.Count > 0 && !shelf.Take.Busy, () => TakeInto(Selection.Of(Entries), shelf.Window.Own.Folder)),
			new(ShelfWord.NewFolder, () => MayChange(Owner), edits.NewFolder),
			new(ShelfWord.Rename, () => Selection.Count == 1 && MayChange(Owner), () => Rename(Selection.Of(Entries)[0])),
			new(ShelfWord.Remove, () => Selection.Count > 0 && Selection.Of(Entries).All(e => MayChange(OwnerOf(e))), Remove),
		};
	}

	/// <summary>The player whose part is shown; 0 for the list of players.</summary>
	public long Owner { get; private set; }

	/// <summary>The folder shown in that part ("" its top).</summary>
	public string Folder { get; private set; } = "";

	/// <summary>Where shared files go: the folder shown when it is in the player's own part, else the top of it.</summary>
	public string ShareFolder => Owner != 0L && Owner == Me ? Folder : "";

	public static long Me => Player.m_localPlayer != null ? Player.m_localPlayer.GetPlayerID() : 0L;

	private static bool Admin => ZNet.instance != null && ZNet.instance.LocalPlayerIsAdminOrHost();

	/// <summary>The local player may change this part (theirs, or they are an admin); the server checks again.</summary>
	public static bool MayChange(long owner) => owner != 0L && (owner == Me || Admin);

	public string Header => shelf.Words.Get(ShelfWord.Pool);

	public string Location => Owner == 0L ? shelf.Words.Get(ShelfWord.Players) : PaneView.Trail(shelf, NameOf(Owner), Folder);

	public List<ShelfLine> Entries { get; } = new();

	public ShelfSelection Selection { get; } = new();

	public int Version { get; private set; }

	public List<ShelfButton> Actions { get; }

	public long OwnerOf(ShelfLine entry) => entry.Kind == ShelfKind.Owner ? entry.Owner : Owner;

	/// <summary>The listed players, the local player always among them (an empty part until they share).</summary>
	private List<ShelfOwner> Parts()
	{
		List<ShelfOwner> parts = new List<ShelfOwner>(shelf.Pool.Owners);
		long me = Me;
		if (me != 0L && parts.All(o => o.Id != me))
		{
			parts.Add(new ShelfOwner { Id = me, Name = ShelfPaths.Safe(Player.m_localPlayer.GetPlayerName()) });
		}
		return parts.OrderBy(o => o.Id == me ? 0 : 1).ThenBy(o => o.Name, System.StringComparer.OrdinalIgnoreCase).ToList();
	}

	private string NameOf(long owner) => Parts().FirstOrDefault(o => o.Id == owner)?.Name ?? "?";

	public void Tick()
	{
		if (shelf.Pool.Version == seenPool && Owner == seenOwner && Folder == seenFolder)
		{
			return;
		}
		seenPool = shelf.Pool.Version;
		Entries.Clear();
		ShelfOwner? part = Owner == 0L ? null : Parts().FirstOrDefault(o => o.Id == Owner);
		if (part == null)
		{
			Owner = 0L;
			Folder = "";
			Entries.AddRange(shelf.Pool.Loaded ? Parts().Select(Line) : Enumerable.Empty<ShelfLine>());
		}
		else
		{
			Fill(part);
		}
		seenOwner = Owner;
		seenFolder = Folder;
		Selection.Keep(Entries);
		Version++;
	}

	private ShelfLine Line(ShelfOwner owner)
	{
		string you = owner.Id == Me ? " " + shelf.Words.Get(ShelfWord.You) : "";
		return new ShelfLine
		{
			Kind = ShelfKind.Owner, Owner = owner.Id, Name = owner.Name,
			Label = $"<b>{owner.Name}</b>{you}  <color=#b4b4b4>{owner.Paths.Count}</color>",
		};
	}

	/// <summary>One folder of a player's part: the way up (to the list of players from its top), its folders, its files.</summary>
	private void Fill(ShelfOwner part)
	{
		if (!part.Paths.Any(p => ShelfPaths.Within(p, Folder)) && !part.Folders.Any(f => ShelfPaths.Within(f, Folder)))
		{
			Folder = "";
		}
		Entries.Add(Folder.Length > 0 ? ShelfLine.Up(Owner, ShelfPaths.Parent(Folder)) : ShelfLine.Up(0L, ""));
		Entries.AddRange(Children(part, Folder).Select(f => ShelfLine.Of(ShelfKind.Folder, Owner, f, shelf.Settings.Title)));
		Entries.AddRange(part.Paths.Where(p => ShelfPaths.Same(ShelfPaths.Parent(p), Folder))
			.Select(p => ShelfLine.Of(ShelfKind.File, Owner, p, shelf.Settings.Title)));
	}

	/// <summary>The folders right inside one: those the server listed and those its files lie in.</summary>
	private static IEnumerable<string> Children(ShelfOwner part, string folder)
	{
		SortedSet<string> found = new SortedSet<string>(System.StringComparer.OrdinalIgnoreCase);
		foreach (string path in part.Folders.Concat(part.Paths.Select(ShelfPaths.Parent)))
		{
			if (path.Length == 0 || ShelfPaths.Same(path, folder) || !ShelfPaths.Within(path, folder))
			{
				continue;
			}
			found.Add(ShelfPaths.Join(folder, ShelfPaths.Below(path, folder).Split('/')[0]));
		}
		return found;
	}

	public void Open(ShelfLine entry)
	{
		if (!entry.Holds)
		{
			return;
		}
		Owner = entry.Kind == ShelfKind.Owner || entry.Kind == ShelfKind.Up ? entry.Owner : Owner;
		Folder = entry.Kind == ShelfKind.Owner ? "" : entry.Path;
		Selection.Clear();
	}

	public void Rename(ShelfLine entry)
	{
		if ((entry.Kind == ShelfKind.Folder || entry.Kind == ShelfKind.File) && MayChange(Owner))
		{
			edits.Rename(entry, Owner);
		}
	}

	public void Remove() => edits.Remove(Selection.Of(Entries).Where(e => MayChange(OwnerOf(e))).ToList());

	public bool Accepts(IShelfPane from, ShelfLine? onto)
	{
		if (from is OwnPane)
		{
			return Owner == 0L ? onto == null || (onto.Kind == ShelfKind.Owner && onto.Owner == Me) : Owner == Me && (onto == null || onto.Holds);
		}
		return from == this && onto != null && onto.Holds && onto.Kind != ShelfKind.Owner && onto.Owner != 0L && MayChange(Owner);
	}

	public void Drop(IShelfPane from, List<ShelfLine> dragged, ShelfLine? onto)
	{
		if (from is OwnPane own)
		{
			shelf.Share.Send(own.SharePairs(dragged, Owner == Me && onto != null && onto.Owner != 0L ? onto.Path : ShareFolder));
		}
		else if (onto != null)
		{
			edits.MoveInto(dragged, Owner, onto.Path);
		}
	}

	/// <summary>Copies lines into a folder of the player's own: a file by name, a folder with its contents, a player's part under their name.</summary>
	public void TakeInto(List<ShelfLine> picked, string into)
	{
		foreach (ShelfLine entry in picked)
		{
			ShelfOwner? part = shelf.Pool.Find(OwnerOf(entry));
			if (part != null)
			{
				shelf.Take.Take(part.Id, TakePairs(part, entry, into), into);
			}
		}
	}

	private static IEnumerable<(string, string)> TakePairs(ShelfOwner part, ShelfLine entry, string into)
	{
		if (entry.Kind == ShelfKind.File)
		{
			return new[] { (entry.Path, ShelfPaths.Join(into, ShelfPaths.Leaf(entry.Path))) };
		}
		if (entry.Kind == ShelfKind.Owner)
		{
			return part.Paths.Select(p => (p, ShelfPaths.Join(into, ShelfPaths.Join(ShelfPaths.Safe(part.Name), p))));
		}
		string parent = ShelfPaths.Parent(entry.Path);
		return part.Paths.Where(p => p != entry.Path && ShelfPaths.Within(p, entry.Path))
			.Select(p => (p, ShelfPaths.Join(into, ShelfPaths.Below(p, parent))));
	}
}
