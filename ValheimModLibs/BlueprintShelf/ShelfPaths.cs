using System;
using System.IO;
using System.Linq;
using System.Text;

namespace BlueprintShelf;

/// <summary>
/// Paths of blueprints and folders: "houses/barn" from the top, no extension. In the shared pool every part must be a name
/// that is a valid file name on Windows and Linux alike (the server may run on either), the whole at most the name limit.
/// </summary>
public static class ShelfPaths
{
	private static readonly char[] Invalid = "<>:\"/\\|?*".ToCharArray();

	public static string Join(string folder, string name) => string.IsNullOrEmpty(folder) ? name : folder + "/" + name;

	/// <summary>The folder a path sits in ("" for the top).</summary>
	public static string Parent(string path)
	{
		int slash = path.LastIndexOf('/');
		return slash < 0 ? "" : path.Substring(0, slash);
	}

	/// <summary>The last part of a path: the file or folder name.</summary>
	public static string Leaf(string path) => path.Substring(path.LastIndexOf('/') + 1);

	public static bool ValidPart(string part) =>
		!string.IsNullOrWhiteSpace(part) && part == part.Trim() && part != "." && part != ".." && !part.EndsWith(".")
		&& part.IndexOfAny(Invalid) < 0 && part.IndexOfAny(Path.GetInvalidFileNameChars()) < 0 && !part.Any(char.IsControl);

	/// <summary>The path when every part is valid, else null; "" (the top) only when <paramref name="top"/> allows it.</summary>
	public static string? Clean(string? path, int limit, bool top = false)
	{
		if (path == null || path.Length > limit)
		{
			return null;
		}
		if (path.Length == 0)
		{
			return top ? "" : null;
		}
		return path.Split('/').All(ValidPart) ? path : null;
	}

	/// <summary>A usable file or folder name from any text (a player's name): bad characters become "_".</summary>
	public static string Safe(string? name)
	{
		StringBuilder text = new StringBuilder();
		foreach (char c in (name ?? "").Trim())
		{
			text.Append(c == '/' || Array.IndexOf(Invalid, c) >= 0 || char.IsControl(c) || Path.GetInvalidFileNameChars().Contains(c) ? '_' : c);
		}
		string safe = text.ToString().Trim('.', ' ');
		return safe.Length > 0 ? safe : "_";
	}

	/// <summary>The part of a path below a folder ("houses/old/barn" below "houses" is "old/barn"; below "" all of it).</summary>
	public static string Below(string path, string folder) => folder.Length == 0 ? path : path.Substring(folder.Length + 1);

	/// <summary>The path is the folder itself or lies inside it ("" holds everything).</summary>
	public static bool Within(string path, string folder) =>
		folder.Length == 0 || string.Equals(path, folder, StringComparison.OrdinalIgnoreCase)
		|| path.StartsWith(folder + "/", StringComparison.OrdinalIgnoreCase);

	public static bool Same(string a, string b) => string.Equals(a, b, StringComparison.OrdinalIgnoreCase);
}
