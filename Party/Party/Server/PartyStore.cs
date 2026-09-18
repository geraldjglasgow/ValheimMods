using System;
using System.Globalization;
using System.IO;
using BepInEx;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Party.Server
{
    /// <summary>One YAML file per world holding every party. Survives a server restart, not just a disconnect.</summary>
    public sealed class PartyStore
    {
        public const string TimeFormat = "yyyy-MM-dd HH:mm";

        public string FilePath { get; }
        public PartyFile Data { get; private set; } = new PartyFile();

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

        public PartyStore(string worldName)
        {
            string safeName = string.Join("_", worldName.Split(Path.GetInvalidFileNameChars()));
            FilePath = Path.Combine(Paths.ConfigPath, $"{PluginInfo.PluginName}.{safeName}.parties.yml");
        }

        public static string Now => DateTime.UtcNow.ToString(TimeFormat, CultureInfo.InvariantCulture);

        public bool Load()
        {
            if (!File.Exists(FilePath))
                return false;
            string text = File.ReadAllText(FilePath);
            Data = Parse(text) ?? new PartyFile();
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
            "# Party roster. Written by the server; parties survive a restart, members keep their spot while offline.\n" +
            "# Edit by hand only to fix a stuck state; the mod's own commands are the normal way to change this.\n";

        private static PartyFile Parse(string text)
        {
            try
            {
                return deserializer.Deserialize<PartyFile>(text);
            }
            catch (Exception e)
            {
                PartyPlugin.Log.LogError($"Party file could not be read, keeping the previous state: {e.Message}");
                return null;
            }
        }

        /// <summary>Polls every five seconds for admin edits (no FileSystemWatcher - see ConfigReload).</summary>
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
                PartyPlugin.Log.LogError($"Watching the party file failed: {e.Message}");
            }
        }

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
                PartyFile parsed = Parse(text);
                if (parsed == null)
                    return;
                Data = parsed;
                lastWrittenText = text;
                PartyPlugin.Log.LogInfo("Party file was edited, reloaded it.");
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
    }
}
