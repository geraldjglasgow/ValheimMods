using System.Collections.Generic;
using System.Linq;

namespace YamlConfig;

/// <summary>
/// Reporting: errors and warnings at a node's path (and line, when the model tracks lines), and the keys nobody asked
/// for. Split from <see cref="YamlNode"/>'s tree walking the way the typed readers are.
/// </summary>
public sealed partial class YamlNode
{
	/// <summary>Records an error at this node's path. The file set is rejected when a model has errors.</summary>
	public void Error(string message) => model.AddError(Prefixed(message));

	/// <summary>Records a warning at this node's path. Warnings are logged, the file set is still applied.</summary>
	public void Warn(string message) => model.AddWarning(Prefixed(message));

	/// <summary>
	/// Warns about keys nobody asked for, in this map and recursively in every map below it that a model visited
	/// through <see cref="Get"/> or <see cref="Entries"/>. Maps that were never visited are left alone, and so are maps
	/// already reported by <see cref="ErrorUnknownKeys"/>.
	/// </summary>
	public void WarnUnknownKeys() => ReportUnknownKeys(asError: false);

	/// <summary>
	/// Like <see cref="WarnUnknownKeys"/>, but each unknown key is an error at its own path ("unknown key"), for a model
	/// that refuses an entry with a misspelt key (inside <see cref="YamlModel.CollectErrors"/> it refuses only that entry).
	/// Keys reported here are not warned about again.
	/// </summary>
	public void ErrorUnknownKeys() => ReportUnknownKeys(asError: true);

	private void ReportUnknownKeys(bool asError)
	{
		if (Kind == YamlNodeKind.Map && visited)
		{
			if (!unknownReported)
			{
				unknownReported = true;
				ReportUnknownHere(asError);
			}
			foreach (KeyValuePair<string, YamlNode> entry in MapEntries)
			{
				entry.Value.ReportUnknownKeys(asError);
			}
		}
		else if (Kind == YamlNodeKind.List && listItems is not null)
		{
			listItems.ForEach(item => item.ReportUnknownKeys(asError));
		}
	}

	private void ReportUnknownHere(bool asError)
	{
		List<KeyValuePair<string, YamlNode>> unknown = MapEntries.Where(e => !AskedKeys.Contains(e.Key)).ToList();
		if (unknown.Count == 0)
		{
			return;
		}
		if (!asError)
		{
			Warn("unknown keys " + string.Join(", ", unknown.Select(e => e.Key)));
			return;
		}
		foreach (KeyValuePair<string, YamlNode> entry in unknown)
		{
			entry.Value.Error("unknown key");
		}
	}

	/// <summary>The path, and the line when the model tracks lines, before a message: <c>a.b (line 4): message</c>.</summary>
	private string Prefixed(string message)
	{
		int line = model.TrackLines ? Line : 0;
		string where = line > 0 ? (string.IsNullOrEmpty(Path) ? "" : Path + " ") + "(line " + line + ")" : Path;
		return string.IsNullOrEmpty(where) ? message : where + ": " + message;
	}
}
