using System;
using System.IO;
using System.Linq;

namespace BlueprintShelf;

/// <summary>
/// Making, renaming, moving and deleting in a <see cref="ShelfFolder"/>, never touching what a file holds. A name is one
/// file or folder name; a typed name with "/" is a path from the top ("ships/karve" moves a file into the folder ships,
/// made when missing; "/karve" moves it to the top), never with "." or ".." parts. Every refusal comes back as a message
/// for the player.
/// </summary>
internal sealed class ShelfFolderEdits
{
	private readonly ShelfFolder folder;
	private readonly ShelfWords words;

	public ShelfFolderEdits(ShelfFolder folder, ShelfWords words)
	{
		this.folder = folder;
		this.words = words;
	}

	/// <summary>Null when <paramref name="name"/> can be one file or folder name, else why not.</summary>
	private string? CheckName(string name)
	{
		bool bad = string.IsNullOrWhiteSpace(name) || name != name.Trim() || name == "." || name == ".."
			|| name.EndsWith(".") || name.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0;
		return bad ? words.Format(ShelfWord.BadName, name) : null;
	}

	/// <summary>
	/// The path a typed name means: a plain name stays in <paramref name="parent"/>, a name with "/" is a path from the top
	/// (spaces around a "/" dropped). Null (with the reason) when a part is not a valid name.
	/// </summary>
	public string? Target(string typed, string parent, out string? error)
	{
		string trimmed = typed.Trim();
		bool fromTop = trimmed.Contains("/");
		string[] parts = trimmed.Trim('/').Split('/').Select(part => part.Trim()).ToArray();
		error = parts.Select(CheckName).FirstOrDefault(e => e != null);
		if (error != null)
		{
			return null;
		}
		string path = string.Join("/", parts);
		return fromTop ? path : ShelfPaths.Join(parent, path);
	}

	public bool CreateFolder(string parent, string typed, out string path, out string? error)
	{
		path = Target(typed, parent, out error) ?? "";
		if (error != null)
		{
			return false;
		}
		string full = folder.FullPath(path);
		if (Directory.Exists(full))
		{
			error = words.Format(ShelfWord.Exists, path);
			return false;
		}
		return Run(() => Directory.CreateDirectory(full), out error);
	}

	/// <summary>Renames or moves a file or folder to what was typed; false with the reason (none for an unchanged name).</summary>
	public bool Rename(string from, bool isFolder, string typed, out string to, out string? error)
	{
		to = Target(typed, ShelfPaths.Parent(from), out error) ?? "";
		if (error != null || to == from)
		{
			return false;
		}
		error = Refusal(from, to, isFolder);
		if (error != null)
		{
			return false;
		}
		string source = isFolder ? folder.FullPath(from) : folder.FileOf(from);
		string target = isFolder ? folder.FullPath(to) : folder.FileOf(to);
		return Run(() => ShelfFolder.MoveOnDisk(source, target, isFolder), out error);
	}

	/// <summary>Deletes a file, or a folder with everything in it (never the top folder); false with the system's reason.</summary>
	public bool Delete(string path, bool isFolder, out string? error)
	{
		error = null;
		if (string.IsNullOrEmpty(path))
		{
			return false;
		}
		return isFolder
			? Run(() => Directory.Delete(folder.FullPath(path), true), out error)
			: Run(() => File.Delete(folder.FileOf(path)), out error);
	}

	/// <summary>Why a move cannot be made: the target exists (a change of case only is allowed) or a folder would go into itself.</summary>
	private string? Refusal(string from, string to, bool isFolder)
	{
		if (isFolder && to.StartsWith(from + "/", StringComparison.OrdinalIgnoreCase))
		{
			return words.Format(ShelfWord.IntoItself, from);
		}
		bool caseOnly = ShelfPaths.Same(from, to);
		bool taken = isFolder ? Directory.Exists(folder.FullPath(to)) || File.Exists(folder.FileOf(to)) : File.Exists(folder.FileOf(to));
		return taken && !caseOnly ? words.Format(ShelfWord.Exists, to) : null;
	}

	/// <summary>Runs a file operation; false with the system's reason when it throws.</summary>
	private static bool Run(Action action, out string? error)
	{
		error = null;
		try
		{
			action();
			return true;
		}
		catch (Exception e) when (e is IOException || e is UnauthorizedAccessException || e is ArgumentException || e is NotSupportedException)
		{
			error = e.Message;
			return false;
		}
	}
}
