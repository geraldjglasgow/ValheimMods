using System.Collections.Generic;
using EliteCreaturesPack.Core;
using EliteCreaturesPack.Custom.Definitions;
using EliteCreaturesPack.Custom.Files;
using EliteCreaturesPack.Custom.Saves;

namespace EliteCreaturesPack.Custom.Build
{
    /// <summary>
    /// When the custom creatures are built, once per world on every peer, before any of the world's creatures is made.
    /// <list type="bullet">
    /// <item><b>Server, host, single player</b> (the game's server flag, set before the world's scene loads): from the
    /// local files, at the end of <c>ZoneSystem.Start</c>, when every Awake of the scene has run (the game's, the other
    /// mods' and this mod's own creatures are registered, ObjectDB is the world's) and before the first
    /// <c>ZNetScene.Update</c> makes any object. The files are loaded by then: at the main menu, or on a dedicated
    /// server in <c>ZNet.Awake</c>'s postfix (an Awake of the same scene). Files that would not load at all build
    /// nothing. The files it built from are then published to every player (<see cref="ServerDefinitions"/>).</item>
    /// <item><b>A joining client</b> never builds from its own files: it waits for the server's, which arrive with
    /// Charter's first push after the game accepts it, and builds on arrival. Until then a ZDO of a custom prefab is left
    /// uncreated and tried again on the next pass (a client never destroys an unknown prefab's ZDO: only the server
    /// does, <c>ZNetScene.CreateObjectsSorted</c>), so it appears as soon as its prefab exists.</item>
    /// </list>
    /// A file changed during play (the five-second reload) applies the next time a world is loaded; the log says so.
    /// </summary>
    internal static class BuildTiming
    {
        private static ZNetScene? scene;
        private static bool ready;
        private static bool built;

        /// <summary>The local files' model when this world was built (built from or not), to tell an edit from a re-apply.</summary>
        private static CreatureFile? filesAtBuild;
        private static Dictionary<string, string>? serverFiles;

        /// <summary>This peer has built the current world's custom creatures (from whatever definitions it had).</summary>
        public static bool Settled => built && scene != null && scene == ZNetScene.instance;

        /// <summary>The game's own server flag, set by the start screen before the world's scene loads.</summary>
        public static bool IsServerSide => ZNet.m_isServer;

        /// <summary>A world's scene woke: a new session, nothing built for it yet.</summary>
        public static void SceneWoke(ZNetScene woke)
        {
            scene = woke;
            ready = false;
            built = false;
            filesAtBuild = null;
            serverFiles = null;
            ParkedCreatures.Reset();
        }

        /// <summary>Every Awake of the scene has run (ZoneSystem.Start).</summary>
        public static void SceneReady()
        {
            ready = true;
            TryBuild();
        }

        /// <summary>
        /// The YAML file set applied a model: at load, on a reload, or (on a bound client) the server's current files.
        /// Builds read the files themselves; this only tells the server's admin that an edit waits for the next world.
        /// </summary>
        public static void FilesApplied(CreatureFile model)
        {
            if (IsServerSide && Settled && model != filesAtBuild)
            {
                Log.Info("Custom creature files changed: the change applies the next time a world is loaded.");
            }
        }

        /// <summary>A client: the files the server built its creatures from, with the first push of the connection.</summary>
        public static void ServerFilesArrived(Dictionary<string, string> files)
        {
            if (IsServerSide)
            {
                return;
            }
            if (Settled)
            {
                Log.Info("The server's custom creature files changed: they apply the next time you join.");
                return;
            }
            serverFiles = files;
            TryBuild();
        }

        private static void TryBuild()
        {
            ZNetScene current = ZNetScene.instance;
            if (!ready || current == null || current != scene || built)
            {
                return;
            }
            if (IsServerSide)
            {
                BuildLocal(current);
            }
            else if (serverFiles != null)
            {
                BuildFromServer(current, serverFiles);
            }
        }

        /// <summary>Server side: from the local files, or nothing when switched off or when no file could be read.</summary>
        private static void BuildLocal(ZNetScene current)
        {
            built = true;
            filesAtBuild = CreatureFiles.Current;
            CreatureFile? model = CustomSettings.On ? filesAtBuild : null;
            IReadOnlyList<CreatureDefinition> definitions = model?.Creatures ?? new List<CreatureDefinition>();
            SafeCall.Run("custom creatures build", Run, current, definitions, "this world's files");
            ServerDefinitions.Publish(model != null ? CreatureFiles.Files : new Dictionary<string, string>());
        }

        private static void BuildFromServer(ZNetScene current, Dictionary<string, string> files)
        {
            built = true;
            CreatureFile? model = files.Count > 0 ? CreatureFiles.Parse(files) : null;
            IReadOnlyList<CreatureDefinition> definitions = model?.Creatures ?? new List<CreatureDefinition>();
            SafeCall.Run("custom creatures build", Run, current, definitions, "the server's files");
        }

        private static void Run(ZNetScene current, IReadOnlyList<CreatureDefinition> definitions, string source) =>
            BuildPass.Run(current, definitions, source);
    }
}
