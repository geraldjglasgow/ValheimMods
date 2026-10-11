using System;
using System.Collections.Generic;
using System.IO;
using YamlDotNet.RepresentationModel;
using RepresentationNode = YamlDotNet.RepresentationModel.YamlNode;

namespace YamlConfig;

/// <summary>
/// Where each value of a file sits: the line (1-based) of every path a <see cref="YamlNode"/> can have, such as
/// <c>rules[3].limits.Count</c>. The untyped graph the nodes wrap carries no positions, so a model that asks for lines
/// (<see cref="YamlModel.TrackLines"/>) has its files read a second time through YamlDotNet's representation model,
/// which does. A map entry is placed on its key's line, a list item on the item's first line. A text that model cannot
/// read gives an empty index: messages then carry no line, and nothing else changes.
/// </summary>
internal static class YamlLineIndex
{
	public static Dictionary<string, int> Build(string text)
	{
		Dictionary<string, int> lines = new(StringComparer.Ordinal);
		try
		{
			YamlStream stream = new();
			stream.Load(new StringReader(text.TrimStart('﻿')));
			if (stream.Documents.Count > 0)
			{
				Walk(stream.Documents[0].RootNode, "", lines);
			}
		}
		catch (Exception)
		{
			lines.Clear();
		}
		return lines;
	}

	private static void Walk(RepresentationNode node, string path, Dictionary<string, int> lines)
	{
		if (node is YamlMappingNode map)
		{
			foreach (KeyValuePair<RepresentationNode, RepresentationNode> pair in map.Children)
			{
				string key = (pair.Key as YamlScalarNode)?.Value ?? pair.Key.ToString();
				string child = path.Length == 0 ? key : path + "." + key;
				Note(lines, child, pair.Key.Start.Line);
				Walk(pair.Value, child, lines);
			}
		}
		else if (node is YamlSequenceNode list)
		{
			for (int i = 0; i < list.Children.Count; i++)
			{
				string child = $"{path}[{i}]";
				Note(lines, child, list.Children[i].Start.Line);
				Walk(list.Children[i], child, lines);
			}
		}
	}

	private static void Note(Dictionary<string, int> lines, string path, long line)
	{
		if (!lines.ContainsKey(path))
		{
			lines[path] = (int)line;
		}
	}
}
