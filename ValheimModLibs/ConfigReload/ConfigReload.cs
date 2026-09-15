using System;
using System.IO;
using System.Threading;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;

namespace ConfigReload;

/// <summary>
/// Two things almost every BepInEx mod gets wrong: the .cfg file is not written until a value changes, and editing it
/// requires a restart. <see cref="Setup"/> writes the file immediately and reloads it whenever it changes on disk.
/// SettingChanged events fire as usual, so Charter and any other listeners pick the new values up.
/// </summary>
/// <remarks>
/// Changes are detected by polling the file write time and size every five seconds on a timer thread, not with a
/// FileSystemWatcher. On Linux servers Mono's watcher raised change events for the mod's own reads of the file,
/// which turned every reload into the trigger of the next one and kept the main thread busy forever. Polling
/// cannot feed itself: reading the file changes neither its write time nor its size. The main thread is only
/// involved when the text really differs from the last one seen, and it reloads only when the file's values differ
/// from what the ConfigFile holds (<see cref="LoadedValues"/>): the mod's own saves, BepInEx's save after a late
/// Bind and Charter's amendments rewrite the file with the loaded values and are never reported as a change.
/// </remarks>
public static class ConfigReloader
{
	/// <summary>
	/// Call once at the end of your plugin's Awake, after all Config.Bind calls.
	/// </summary>
	/// <param name="config">The plugin's ConfigFile (<c>Config</c> in a BaseUnityPlugin).</param>
	/// <param name="log">Optional logger for reload messages and errors.</param>
	/// <param name="saveNow">Write the file right away so it exists after the first run.</param>
	/// <returns>The poller, keep the reference alive or dispose it to stop watching.</returns>
	public static IDisposable Setup(ConfigFile config, ManualLogSource? log = null, bool saveNow = true)
	{
		if (saveNow)
		{
			config.Save();
		}
		// The first snapshot is taken after the save above, so the startup write is never the first "change".
		return new Poller(config, log);
	}

	private sealed class Poller : IDisposable
	{
		private const int IntervalMilliseconds = 5000;

		private readonly ConfigFile config;
		private readonly ManualLogSource? log;
		private readonly string fileName;
		private readonly Timer timer;
		private DateTime lastWriteTime;
		private long lastLength;
		private string lastContent;
		private int reloading;

		public Poller(ConfigFile config, ManualLogSource? log)
		{
			this.config = config;
			this.log = log;
			fileName = Path.GetFileName(config.ConfigFilePath);
			Snapshot(out lastWriteTime, out lastLength);
			lastContent = ReadOrNull(config.ConfigFilePath) ?? "";
			timer = new Timer(_ => Poll(), null, IntervalMilliseconds, IntervalMilliseconds);
		}

		// Timer thread: cheap checks only, nothing touches the game or the ConfigFile here.
		private void Poll()
		{
			try
			{
				if (!SnapshotChanged())
				{
					return;
				}
				string? content = ReadOrNull(config.ConfigFilePath);
				if (content is null || content == lastContent)
				{
					// Still being written, or rewritten with the same text.
					return;
				}
				lastContent = content;
				ScheduleCheck();
			}
			catch (Exception e)
			{
				log?.LogError($"Watching {fileName} failed: {e.Message}");
			}
		}

		// True when write time or size differ from the previous poll; remembers the new values either way.
		private bool SnapshotChanged()
		{
			Snapshot(out DateTime writeTime, out long length);
			if (writeTime == lastWriteTime && length == lastLength)
			{
				return false;
			}
			lastWriteTime = writeTime;
			lastLength = length;
			return true;
		}

		private void ScheduleCheck()
		{
			if (Interlocked.CompareExchange(ref reloading, 1, 0) == 0)
			{
				// ISynchronizeInvoke keeps this project free of a UnityEngine reference; BepInEx runs it on the main thread.
				ThreadingHelper.SynchronizingObject.BeginInvoke(new Action(CheckAndReload), null);
			}
		}

		// Main thread: compare the file's values with the loaded ones (the entries may be bound concurrently
		// elsewhere, so this cannot run on the timer thread), reload only when they differ, so SettingChanged
		// handlers run where mods expect them.
		private void CheckAndReload()
		{
			try
			{
				if (LoadedValues.Matches(config, lastContent))
				{
					return;
				}
				log?.LogInfo($"{fileName} changed on disk, reloading");
				config.Reload();
			}
			catch (Exception ex)
			{
				log?.LogError($"Failed to reload {fileName}, please check it for typos and formatting: {ex.Message}");
			}
			finally
			{
				Resnapshot();
				Interlocked.Exchange(ref reloading, 0);
			}
		}

		// After a check or reload the file as it is now is the baseline, whatever was written meanwhile.
		private void Resnapshot()
		{
			Snapshot(out lastWriteTime, out lastLength);
			lastContent = ReadOrNull(config.ConfigFilePath) ?? lastContent;
		}

		private void Snapshot(out DateTime writeTime, out long length)
		{
			FileInfo info = new(config.ConfigFilePath);
			if (info.Exists)
			{
				writeTime = info.LastWriteTimeUtc;
				length = info.Length;
			}
			else
			{
				writeTime = DateTime.MinValue;
				length = -1;
			}
		}

		private static string? ReadOrNull(string path)
		{
			try
			{
				return File.Exists(path) ? File.ReadAllText(path) : null;
			}
			catch (IOException)
			{
				return null;
			}
		}

		public void Dispose() => timer.Dispose();
	}
}
