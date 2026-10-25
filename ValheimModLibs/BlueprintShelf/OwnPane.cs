using System.Collections.Generic;
using System.Linq;

namespace BlueprintShelf;

/// <summary>
/// The window's left side: the player's own blueprints (<see cref="IShelfFiles"/>) as a file explorer: one folder at a
/// time, the way up first, then its folders, then its blueprints. New folder, Rename, Delete (asked first), drags onto a
/// folder move, Share sends the picks (a folder: everything in it, its inner folders kept) into the folder the pool side
/// shows when that is the player's own part, else to the top of it; Share all sends every blueprint to its own path.
/// Lines dropped from the pool are taken as copies into the folder dropped on. Every change goes through the host's files,
/// so whatever else of the host lists them follows.
/// </summary>
internal sealed class OwnPane : IShelfPane
{
	private readonly Shelf shelf;
	private int seenFiles = -1;
	private string? seenFolder;

	public OwnPane(Shelf shelf)
	{
		this.shelf = shelf;
		Actions = new List<ShelfButton>
		{
			new(ShelfWord.NewFolder, () => true, NewFolder),
			new(ShelfWord.Rename, () => Selection.Count == 1, () => Rename(Selection.Of(Entries)[0])),
			new(ShelfWord.Delete, () => Selection.Count > 0, Remove),
			new(ShelfWord.Share, () => Selection.Count > 0 && !shelf.Share.Busy, ShareSelected),
			new(ShelfWord.ShareAll, () => !shelf.Share.Busy, () => shelf.Share.Send(shelf.Share.AllOwn())),
		};
	}

	private IShelfFiles Files => shelf.Files;
	private ShelfWords Words => shelf.Words;

	/// <summary>The folder shown ("" the top), kept while the game runs.</summary>
	public string Folder { get; private set; } = "";

	public string Header => Words.Get(ShelfWord.Yours);

	public string Location => PaneView.Trail(shelf, Words.Get(ShelfWord.Home), Folder);

	public List<ShelfLine> Entries { get; } = new();

	public ShelfSelection Selection { get; } = new();

	public int Version { get; private set; }

	public List<ShelfButton> Actions { get; }

	/// <summary>The picked lines of one kind, as paths.</summary>
	public List<string> Picked(ShelfKind kind) => Selection.Of(Entries).Where(e => e.Kind == kind).Select(e => e.Path).ToList();

	public void Tick()
	{
		if (Folder.Length > 0 && !Files.Exists(Folder))
		{
			Folder = "";
		}
		List<string> folders = Files.Folders(Folder);
		List<string> names = Files.Files(Folder);
		if (Files.Version == seenFiles && Folder == seenFolder)
		{
			return;
		}
		seenFiles = Files.Version;
		seenFolder = Folder;
		Entries.Clear();
		if (Folder.Length > 0)
		{
			Entries.Add(ShelfLine.Up(0L, ShelfPaths.Parent(Folder)));
		}
		Entries.AddRange(folders.Select(f => ShelfLine.Of(ShelfKind.Folder, 0L, f, shelf.Settings.Title)));
		Entries.AddRange(names.Select(n => ShelfLine.Of(ShelfKind.File, 0L, n, shelf.Settings.Title)));
		Selection.Keep(Entries);
		Version++;
	}

	public void Open(ShelfLine entry)
	{
		if (!entry.Holds)
		{
			return;
		}
		Folder = entry.Path;
		Selection.Clear();
	}

	private void NewFolder() =>
		ShelfPrompt.Ask(Words.Get(ShelfWord.NewFolderTopic), "", shelf.Limits.NameLimit, name => shelf.Run("new folder", () => MakeFolder(name)));

	private void MakeFolder(string name)
	{
		if (Files.CreateFolder(Folder, name, out string path, out string? error))
		{
			shelf.Say(Words.Format(ShelfWord.FolderMade, path));
		}
		else
		{
			shelf.Say(error ?? "");
		}
	}

	public void Rename(ShelfLine entry)
	{
		if (entry.Kind != ShelfKind.Folder && entry.Kind != ShelfKind.File)
		{
			return;
		}
		ShelfPrompt.Ask(Words.Get(ShelfWord.RenameTopic), ShelfPaths.Leaf(entry.Path), shelf.Limits.NameLimit,
			typed => shelf.Run("rename", () => Move(entry, typed, false)));
	}

	/// <summary>Renames or moves one line to what was typed (a path from the top with "/"); says why not at the top left.</summary>
	private bool Move(ShelfLine entry, string typed, bool quiet)
	{
		if (Files.Rename(entry.Path, entry.Kind == ShelfKind.Folder, typed, out string to, out string? error))
		{
			if (!quiet)
			{
				shelf.Say(Words.Format(ShelfWord.Renamed, entry.Path, to));
			}
			return true;
		}
		if (error != null)
		{
			shelf.Say(Words.Format(ShelfWord.MoveRefused, entry.Label, error));
		}
		return false;
	}

	public void Remove()
	{
		List<ShelfLine> picked = Selection.Of(Entries);
		if (picked.Count > 0)
		{
			shelf.Confirm(Words.Get(ShelfWord.Delete), Words.Format(ShelfWord.DeleteAsk, ShelfConfirm.Names(picked)), () => Delete(picked));
		}
	}

	private void Delete(List<ShelfLine> picked)
	{
		int deleted = 0;
		foreach (ShelfLine entry in picked)
		{
			if (Files.Delete(entry.Path, entry.Kind == ShelfKind.Folder, out string? error))
			{
				deleted++;
			}
			else if (error != null)
			{
				shelf.Say(error);
			}
		}
		Selection.Clear();
		if (deleted > 0)
		{
			shelf.Say(Words.Format(ShelfWord.Deleted, ShelfConfirm.Names(picked)));
		}
	}

	public bool Accepts(IShelfPane from, ShelfLine? onto) => onto == null || onto.Holds;

	public void Drop(IShelfPane from, List<ShelfLine> dragged, ShelfLine? onto)
	{
		string into = onto?.Path ?? Folder;
		if (from is PoolPane pool)
		{
			pool.TakeInto(dragged, into);
		}
		else
		{
			MoveInto(dragged, into);
		}
	}

	/// <summary>Moves the lines into a folder one by one; a refused one says why and the rest still move.</summary>
	private void MoveInto(List<ShelfLine> dragged, string into)
	{
		int moved = 0;
		foreach (ShelfLine entry in dragged.Where(e => e.Kind == ShelfKind.Folder || e.Kind == ShelfKind.File))
		{
			if (ShelfPaths.Parent(entry.Path) == into || entry.Path == into)
			{
				continue;
			}
			if (Move(entry, "/" + ShelfPaths.Join(into, ShelfPaths.Leaf(entry.Path)), true))
			{
				moved++;
			}
		}
		Selection.Clear();
		if (moved > 0)
		{
			shelf.Say(Words.Format(ShelfWord.Moved, moved, shelf.Shown(into)));
		}
	}

	private void ShareSelected()
	{
		List<(string, string)> files = SharePairs(Selection.Of(Entries), shelf.Window.Pool.ShareFolder);
		if (files.Count == 0)
		{
			shelf.Say(Words.Get(ShelfWord.Nothing));
		}
		shelf.Share.Send(files);
	}

	/// <summary>
	/// The player's blueprints and where each goes in their part: a blueprint into <paramref name="into"/> under its name,
	/// a folder's blueprints into <paramref name="into"/> with the folder and its inner folders kept.
	/// </summary>
	public List<(string, string)> SharePairs(List<ShelfLine> picked, string into)
	{
		List<(string, string)> files = new List<(string, string)>();
		List<string>? all = null;
		foreach (ShelfLine entry in picked)
		{
			if (entry.Kind == ShelfKind.File)
			{
				files.Add((entry.Path, ShelfPaths.Join(into, ShelfPaths.Leaf(entry.Path))));
			}
			if (entry.Kind != ShelfKind.Folder)
			{
				continue;
			}
			all ??= Files.Everything();
			string parent = ShelfPaths.Parent(entry.Path);
			foreach (string path in all.Where(p => p != entry.Path && ShelfPaths.Within(p, entry.Path)))
			{
				files.Add((path, ShelfPaths.Join(into, ShelfPaths.Below(path, parent))));
			}
		}
		return files;
	}
}
