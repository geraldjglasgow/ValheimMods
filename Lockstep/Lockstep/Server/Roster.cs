using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using BepInEx;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Lockstep
{
    public sealed class RosterEntry
    {
        /// <summary>Persistent player ID from the character file.</summary>
        public long Id { get; set; }
        public string Name { get; set; } = "";
        /// <summary>UTC, <see cref="Roster.TimeFormat"/>.</summary>
        public string LastSeen { get; set; } = "";
        /// <summary>Ignored players never hold the group back.</summary>
        public bool Ignored { get; set; }
        /// <summary>Global keys of the stages this player has cleared.</summary>
        public List<string> Cleared { get; set; } = new List<string>();

        public DateTime LastSeenUtc =>
            DateTime.TryParseExact(LastSeen, Roster.TimeFormat, CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out DateTime time)
                ? time
                : DateTime.MinValue;
    }

    public sealed class RosterFile
    {
        /// <summary>Stage keys the world already had when the roster was created; those stages count as cleared by everyone.</summary>
        public List<string> InstalledKeys { get; set; } = new List<string>();
        public List<RosterEntry> Players { get; set; } = new List<RosterEntry>();
    }

    /// <summary>
    /// The server's record of players and their cleared stages: one YAML file per world in the config folder,
    /// written by the server, hot reloaded when an admin edits it.
    /// </summary>
    public sealed class Roster
    {
        public const string TimeFormat = "yyyy-MM-dd HH:mm";

        public string FilePath { get; }
        public RosterFile Data { get; private set; } = new RosterFile();

        /// <summary>Raised after an external edit was loaded.</summary>
        public event Action Changed;

        private string lastWrittenText = "";
        private System.Threading.Timer poller;
        private DateTime lastWriteTime;
        private long lastLength = -1;
        private int reloading;

        private static readonly ISerializer serializer = new SerializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance).Build();

        private static readonly IDeserializer deserializer = new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance).IgnoreUnmatchedProperties().Build();

        public Roster(string worldName)
        {
            string safeName = string.Join("_", worldName.Split(Path.GetInvalidFileNameChars()));
            FilePath = Path.Combine(Paths.ConfigPath, $"{Lockstep.PluginName}.{safeName}.roster.yml");
        }

        public static string Now => DateTime.UtcNow.ToString(TimeFormat, CultureInfo.InvariantCulture);

        /// <summary>Loads the file. Returns false when there is none yet.</summary>
        public bool Load()
        {
            if (!File.Exists(FilePath))
                return false;
            string text = File.ReadAllText(FilePath);
            Data = Parse(text) ?? new RosterFile();
            lastWrittenText = text;
            return true;
        }

        public void Save()
        {
            string text = Header() + serializer.Serialize(Data);
            lastWrittenText = text;
            File.WriteAllText(FilePath, text);
        }

        private static string Header() =>
            "# Lockstep roster. Written by the server, hot reloaded when edited.\n" +
            "#   ignored: true   the player never holds the group back\n" +
            "#   cleared         global keys of the bosses the player has been credited with\n" +
            "#   installedKeys   bosses the world had already defeated when Lockstep was installed\n" +
            "# Console: lockstep status | grant | revoke | ignore | unignore | forget\n";

        private static RosterFile Parse(string text)
        {
            try
            {
                return deserializer.Deserialize<RosterFile>(text);
            }
            catch (Exception e)
            {
                Lockstep.Log.LogError($"Roster file could not be read, keeping the previous roster: {e.Message}");
                return null;
            }
        }

        /// <summary>
        /// Polls the file every five seconds for admin edits. No FileSystemWatcher: on Linux servers Mono raised
        /// change events for the mod's own reads, which looped forever (see ConfigReload in ValheimModLibs).
        /// </summary>
        public void Watch()
        {
            if (poller != null)
                return;
            Snapshot(out lastWriteTime, out lastLength);
            poller = new System.Threading.Timer(_ => Poll(), null, 5000, 5000);
        }

        public void Dispose()
        {
            poller?.Dispose();
            poller = null;
        }

        // Timer thread: only file metadata and text, the roster itself is replaced on the main thread.
        private void Poll()
        {
            try
            {
                PollOnce();
            }
            catch (IOException)
            {
                // Still being written, the next poll reads it.
            }
            catch (Exception e)
            {
                Lockstep.Log.LogError($"Watching the roster file failed: {e.Message}");
            }
        }

        /// <summary>Reads the file when its metadata changed and hands new text to the main thread.</summary>
        private void PollOnce()
        {
            Snapshot(out DateTime writeTime, out long length);
            if (writeTime == lastWriteTime && length == lastLength)
                return;
            lastWriteTime = writeTime;
            lastLength = length;
            if (!File.Exists(FilePath))
                return;
            string text = File.ReadAllText(FilePath);
            if (text == lastWrittenText)
                return;
            if (System.Threading.Interlocked.CompareExchange(ref reloading, 1, 0) == 0)
                ThreadingHelper.SynchronizingObject.BeginInvoke(new Action(() => Reload(text)), null);
        }

        private void Reload(string text)
        {
            try
            {
                RosterFile parsed = Parse(text);
                if (parsed == null)
                    return;
                Data = parsed;
                lastWrittenText = text;
                Lockstep.Log.LogInfo("Roster file was edited, reloaded it.");
                Changed?.Invoke();
            }
            finally
            {
                System.Threading.Interlocked.Exchange(ref reloading, 0);
            }
        }

        private void Snapshot(out DateTime writeTime, out long length)
        {
            FileInfo info = new FileInfo(FilePath);
            writeTime = info.Exists ? info.LastWriteTimeUtc : DateTime.MinValue;
            length = info.Exists ? info.Length : -1;
        }

        public RosterEntry Find(long id) => Data.Players.FirstOrDefault(p => p.Id == id);

        /// <summary>Finds by player ID or by name (case insensitive), for console commands.</summary>
        public RosterEntry Find(string nameOrId)
        {
            if (long.TryParse(nameOrId, out long id))
                return Find(id);
            return Data.Players.FirstOrDefault(p => string.Equals(p.Name, nameOrId, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>Adds or updates a player and stamps last seen.</summary>
        public RosterEntry Touch(long id, string name, out bool isNew)
        {
            RosterEntry entry = Find(id);
            isNew = entry == null;
            if (isNew)
            {
                entry = new RosterEntry { Id = id };
                Data.Players.Add(entry);
            }
            if (!string.IsNullOrEmpty(name))
                entry.Name = name;
            entry.LastSeen = Now;
            return entry;
        }
    }
}
