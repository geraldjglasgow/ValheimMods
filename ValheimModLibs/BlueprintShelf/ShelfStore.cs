using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using UnityEngine;

namespace BlueprintShelf;

/// <summary>
/// The server's copy of the shared pool, on its own disk: BepInEx/config/&lt;pool folder&gt;/&lt;world&gt;/, one folder per
/// player named &lt;name&gt;_&lt;character id&gt; holding their shared files in their folders (empty folders kept: players
/// organise their part), so an admin can read or tidy it by hand. The listing is kept in memory, read again after every
/// change through the shelf and at most every <see cref="Rescan"/> seconds otherwise (files changed by hand). Server only.
/// </summary>
internal sealed class ShelfStore
{
	private const float Rescan = 30f;

	private readonly Shelf shelf;
	private List<ShelfOwner>? owners;
	private string? scannedRoot;
	private float scannedAt;
	private ZPackage? listing;

	public ShelfStore(Shelf shelf) => this.shelf = shelf;

	private string Extension => shelf.Settings.Extension;

	public string Root =>
		Path.Combine(Path.Combine(BepInEx.Paths.ConfigPath, shelf.Settings.PoolFolder), ShelfPaths.Safe(ZNet.instance.GetWorldName()));

	public List<ShelfOwner> Owners
	{
		get
		{
			string root = Root;
			float now = Time.realtimeSinceStartup;
			if (owners != null && root == scannedRoot && now - scannedAt < Rescan)
			{
				return owners;
			}
			owners = Scan(root);
			scannedRoot = root;
			scannedAt = now;
			listing = null;
			return owners;
		}
	}

	/// <summary>The pool packed for the Listing RPC, made once per change.</summary>
	public ZPackage Listing
	{
		get
		{
			List<ShelfOwner> current = Owners;
			return listing ??= ShelfListing.Write(current);
		}
	}

	public ShelfOwner? Find(long id) => Owners.FirstOrDefault(o => o.Id == id);

	public bool Has(long id, string path) => Find(id)?.Paths.Contains(path, StringComparer.OrdinalIgnoreCase) ?? false;

	private void Invalidate() => owners = null;

	private List<ShelfOwner> Scan(string root)
	{
		List<ShelfOwner> found = new List<ShelfOwner>();
		if (!Directory.Exists(root))
		{
			return found;
		}
		foreach (string dir in Directory.GetDirectories(root))
		{
			ShelfOwner? owner = OwnerOf(dir);
			if (owner == null || found.Any(o => o.Id == owner.Id))
			{
				continue;
			}
			List(dir, owner);
			if (owner.Paths.Count > 0 || owner.Folders.Count > 0)
			{
				found.Add(owner);
			}
		}
		return found;
	}

	private void List(string dir, ShelfOwner owner)
	{
		int limit = shelf.Limits.NameLimit;
		owner.Paths.AddRange(Directory.GetFiles(dir, "*" + Extension, SearchOption.AllDirectories).Select(f => Relative(dir, f, true))
			.Where(p => ShelfPaths.Clean(p, limit) != null).OrderBy(p => p, StringComparer.OrdinalIgnoreCase));
		owner.Folders.AddRange(Directory.GetDirectories(dir, "*", SearchOption.AllDirectories).Select(d => Relative(dir, d, false))
			.Where(p => ShelfPaths.Clean(p, limit) != null).OrderBy(p => p, StringComparer.OrdinalIgnoreCase));
	}

	/// <summary>A player's folder "name_id": the id after the last "_"; null for any other folder.</summary>
	private static ShelfOwner? OwnerOf(string dir)
	{
		string name = Path.GetFileName(dir);
		int cut = name.LastIndexOf('_');
		if (cut <= 0 || !long.TryParse(name.Substring(cut + 1), NumberStyles.Integer, CultureInfo.InvariantCulture, out long id) || id == 0L)
		{
			return null;
		}
		return new ShelfOwner { Id = id, Name = name.Substring(0, cut) };
	}

	private string Relative(string dir, string full, bool file) =>
		full.Substring(dir.Length + 1, full.Length - dir.Length - 1 - (file ? Extension.Length : 0)).Replace('\\', '/');

	/// <summary>The player's folder, or null when they never shared anything here.</summary>
	private string? FolderOf(long id)
	{
		string root = Root;
		return Directory.Exists(root) ? Directory.GetDirectories(root).FirstOrDefault(d => OwnerOf(d)?.Id == id) : null;
	}

	/// <summary>The player's folder, made under their name when they have none yet (no name: none made).</summary>
	private string? PartOf(long id, string? name) =>
		FolderOf(id) ?? (name != null ? Path.Combine(Root, ShelfPaths.Safe(name) + "_" + id.ToString(CultureInfo.InvariantCulture)) : null);

	private static string Under(string dir, string path) =>
		path.Length == 0 ? dir : Path.Combine(dir, path.Replace('/', Path.DirectorySeparatorChar));

	private string FileOf(string dir, string path) => Under(dir, path) + Extension;

	/// <summary>Writes (or replaces) one shared file of a player; their folder is made under their name on first use.</summary>
	public void Save(long id, string name, string path, byte[] data)
	{
		string file = FileOf(PartOf(id, name)!, path);
		Directory.CreateDirectory(Path.GetDirectoryName(file));
		File.WriteAllBytes(file, data);
		Invalidate();
	}

	/// <summary>A shared file, or null when it is not there.</summary>
	public byte[]? Read(long id, string path)
	{
		string? dir = FolderOf(id);
		string? file = dir != null ? FileOf(dir, path) : null;
		return file != null && File.Exists(file) ? File.ReadAllBytes(file) : null;
	}

	/// <summary>
	/// Removes one file, or a folder with everything in it ("" is all of the player's): how many files went (0 for an empty
	/// folder), or -1 when it was not there.
	/// </summary>
	public int Delete(long id, string path, bool folder)
	{
		string? dir = FolderOf(id);
		string? target = dir == null ? null : folder ? Under(dir, path) : FileOf(dir, path);
		int removed = -1;
		if (folder && Directory.Exists(target))
		{
			removed = Directory.GetFiles(target, "*" + Extension, SearchOption.AllDirectories).Length;
			Directory.Delete(target, true);
		}
		else if (!folder && File.Exists(target))
		{
			File.Delete(target);
			removed = 1;
		}
		Invalidate();
		return removed;
	}

	/// <summary>Renames or moves a file or folder within one player's part; null when done, else the reason's word.</summary>
	public string? Move(long id, string from, string to, bool folder)
	{
		string? dir = FolderOf(id);
		string? source = dir == null ? null : folder ? Under(dir, from) : FileOf(dir, from);
		if (dir == null || source == null || !(folder ? Directory.Exists(source) : File.Exists(source)))
		{
			return ShelfWord.Gone;
		}
		bool caseOnly = ShelfPaths.Same(from, to);
		if (folder && !caseOnly && ShelfPaths.Within(to, from))
		{
			return ShelfWord.IntoItself;
		}
		string target = folder ? Under(dir, to) : FileOf(dir, to);
		if (!caseOnly && (Directory.Exists(target) || File.Exists(target)))
		{
			return ShelfWord.Exists;
		}
		ShelfFolder.MoveOnDisk(source, target, folder);
		Invalidate();
		return null;
	}

	/// <summary>
	/// Makes an empty folder in a player's part; their own part is made under <paramref name="name"/> when they have none
	/// yet (null: only an existing part). Null when made, else the reason's word.
	/// </summary>
	public string? MakeFolder(long id, string? name, string path)
	{
		string? dir = PartOf(id, name);
		if (dir == null)
		{
			return ShelfWord.Gone;
		}
		string target = Under(dir, path);
		if (Directory.Exists(target) || File.Exists(target + Extension))
		{
			return ShelfWord.Exists;
		}
		Directory.CreateDirectory(target);
		Invalidate();
		return null;
	}
}
