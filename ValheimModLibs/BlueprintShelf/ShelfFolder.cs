using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace BlueprintShelf;

/// <summary>
/// The player's own blueprints as a plain folder of files with folders, for a host that keeps no listing of its own: a
/// folder's listing is read when the window shows it and again every <see cref="RescanInterval"/> seconds, and
/// <see cref="Version"/> goes up whenever one changed or the window changed something (<see cref="Edited"/> tells the host).
/// Making, renaming, moving and deleting are <see cref="ShelfFolderEdits"/>'s. Folders starting with "." are not listed.
/// </summary>
public sealed class ShelfFolder : IShelfFiles
{
	/// <summary>A folder is looked at again (new files, changed files) at most this often, seconds.</summary>
	private const float RescanInterval = 2f;

	private sealed class Listing
	{
		public List<string> Files = new();
		public List<string> Folders = new();
		public float Next;

		public bool SameAs(Listing other) => Files.SequenceEqual(other.Files) && Folders.SequenceEqual(other.Folders);
	}

	private readonly Dictionary<string, Listing> listings = new(StringComparer.OrdinalIgnoreCase);
	private readonly Func<string, bool>? readable;
	private readonly ShelfFolderEdits edits;

	/// <param name="root">The top folder, made on first use.</param>
	/// <param name="extension">A blueprint file's extension, with the dot.</param>
	/// <param name="words">The shelf's words, for the reasons a change is refused.</param>
	/// <param name="readable">Whether a blueprint can be read (only those are shared); none: every file.</param>
	public ShelfFolder(string root, string extension, ShelfWords words, Func<string, bool>? readable = null)
	{
		Root = root;
		Extension = extension;
		this.readable = readable;
		edits = new ShelfFolderEdits(this, words);
	}

	public string Root { get; }

	public string Extension { get; }

	public int Version { get; private set; }

	/// <summary>Raised after the window made, renamed, moved, deleted or wrote something here. Main thread.</summary>
	public event Action? Edited;

	/// <summary>The directory of a folder path ("" is the top folder).</summary>
	public string FullPath(string folder) =>
		string.IsNullOrEmpty(folder) ? Root : Path.Combine(Root, folder.Replace('/', Path.DirectorySeparatorChar));

	public string FileOf(string path) => FullPath(path) + Extension;

	public bool Exists(string folder) => Directory.Exists(FullPath(folder));

	public List<string> Folders(string folder) => ListingOf(folder).Folders;

	public List<string> Files(string folder) => ListingOf(folder).Files;

	public bool Readable(string path) => readable?.Invoke(path) ?? File.Exists(FileOf(path));

	public List<string> Everything()
	{
		Directory.CreateDirectory(Root);
		return Directory.GetFiles(Root, "*" + Extension, SearchOption.AllDirectories)
			.Select(f => Relative(f.Substring(0, f.Length - Extension.Length))).OrderBy(n => n, StringComparer.OrdinalIgnoreCase).ToList();
	}

	public string? Target(string typed, string folder, out string? error) => edits.Target(typed, folder, out error);

	public bool CreateFolder(string parent, string typed, out string path, out string? error) => Done(edits.CreateFolder(parent, typed, out path, out error));

	public bool Rename(string from, bool folder, string typed, out string to, out string? error) => Done(edits.Rename(from, folder, typed, out to, out error));

	public bool Delete(string path, bool folder, out string? error) => Done(edits.Delete(path, folder, out error));

	/// <summary>Every folder is read again at its next use.</summary>
	public void Changed()
	{
		listings.Clear();
		Version++;
		Edited?.Invoke();
	}

	private bool Done(bool done)
	{
		Changed();
		return done;
	}

	private string Relative(string full) => full.Substring(Root.Length).TrimStart('\\', '/').Replace('\\', '/');

	private Listing ListingOf(string folder)
	{
		float now = UnityEngine.Time.realtimeSinceStartup;
		if (listings.TryGetValue(folder, out Listing known) && now < known.Next)
		{
			return known;
		}
		Listing read = Read(folder);
		read.Next = now + RescanInterval;
		if (known != null && known.SameAs(read))
		{
			known.Next = read.Next;
			return known;
		}
		listings[folder] = read;
		Version++;
		return read;
	}

	/// <summary>A folder's files and subfolders; a folder that went away lists as empty.</summary>
	private Listing Read(string folder)
	{
		Directory.CreateDirectory(Root);
		string dir = FullPath(folder);
		Listing listing = new Listing();
		if (!Directory.Exists(dir))
		{
			return listing;
		}
		listing.Files = Directory.GetFiles(dir, "*" + Extension).Where(f => f.EndsWith(Extension, StringComparison.OrdinalIgnoreCase))
			.Select(f => ShelfPaths.Join(folder, Path.GetFileName(f).Substring(0, Path.GetFileName(f).Length - Extension.Length)))
			.OrderBy(n => n, StringComparer.OrdinalIgnoreCase).ToList();
		listing.Folders = Directory.GetDirectories(dir).Select(Path.GetFileName).Where(n => !n.StartsWith("."))
			.Select(n => ShelfPaths.Join(folder, n)).OrderBy(n => n, StringComparer.OrdinalIgnoreCase).ToList();
		return listing;
	}

	/// <summary>Moves a file or folder through a temporary name, so a change of case alone also works on Windows.</summary>
	public static void MoveOnDisk(string source, string target, bool folder)
	{
		Directory.CreateDirectory(Path.GetDirectoryName(target));
		string step = source + ".shelf-move";
		if (folder)
		{
			Directory.Move(source, step);
			Directory.Move(step, target);
		}
		else
		{
			File.Move(source, step);
			File.Move(step, target);
		}
	}
}
