using BepInEx.Logging;

namespace Charter;

/// <summary>Log lines prefixed with "Charter [Title]:", filtered by the copy-wide <see cref="Charter.Verbosity"/>.</summary>
internal sealed class Journal
{
	private static readonly ManualLogSource Source = Logger.CreateLogSource("Charter");
	private readonly string prefix;

	public Journal(string title) => prefix = $"Charter [{title}]: ";

	/// <summary>Always written.</summary>
	public void Error(string text) => Source.LogError(prefix + text);

	/// <summary>Normal and Trace.</summary>
	public void Warning(string text)
	{
		if (Charter.Verbosity != Verbosity.Quiet)
		{
			Source.LogWarning(prefix + text);
		}
	}

	/// <summary>Normal and Trace.</summary>
	public void Info(string text)
	{
		if (Charter.Verbosity != Verbosity.Quiet)
		{
			Source.LogInfo(prefix + text);
		}
	}

	/// <summary>Whether trace lines are written; test it before building a trace line's text.</summary>
	public bool Tracing => Charter.Verbosity == Verbosity.Trace;

	/// <summary>Trace only. The text is built by the caller: on a path that runs often, use the overloads below.</summary>
	public void Trace(string text)
	{
		if (Tracing)
		{
			Source.LogInfo(prefix + text);
		}
	}

	/// <summary>
	/// Trace only, the line formatted only when it is written: the values pass through unboxed, so a trace line on a
	/// path that runs per fragment or per clause costs nothing while tracing is off.
	/// </summary>
	public void Trace<T1>(string format, T1 a)
	{
		if (Tracing)
		{
			Source.LogInfo(prefix + string.Format(format, a));
		}
	}

	public void Trace<T1, T2>(string format, T1 a, T2 b)
	{
		if (Tracing)
		{
			Source.LogInfo(prefix + string.Format(format, a, b));
		}
	}

	public void Trace<T1, T2, T3>(string format, T1 a, T2 b, T3 c)
	{
		if (Tracing)
		{
			Source.LogInfo(prefix + string.Format(format, a, b, c));
		}
	}

	public void Trace<T1, T2, T3, T4>(string format, T1 a, T2 b, T3 c, T4 d)
	{
		if (Tracing)
		{
			Source.LogInfo(prefix + string.Format(format, a, b, c, d));
		}
	}
}
