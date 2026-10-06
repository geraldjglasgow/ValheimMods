using SyncedConfig;

namespace OpenKeep.Core
{
    /// <summary>
    /// Entry point of the Core module: binds the section 0 settings. The console command registers itself through
    /// a patch on <c>Terminal.InitTerminal</c> (<see cref="Command"/>), container tracking through
    /// <see cref="ContainerScan"/>'s patch on <c>Container.Awake</c>; the hotkeys learn the YAML editor's window
    /// (<see cref="Keys.Initialize"/>).
    /// </summary>
    public static class CoreModule
    {
        public static void Initialize(SyncedConfiguration synced)
        {
            CoreSettings.Initialize(synced);
            Keys.Initialize();
        }
    }
}
