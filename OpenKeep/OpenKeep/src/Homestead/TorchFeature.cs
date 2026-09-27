using OpenKeep.Core;
using SyncedConfig;

namespace OpenKeep.Homestead
{
    /// <summary>
    /// Torches lit only at night: binds the keys of section 8 and the hover word. The switch runs in the fire's own tick
    /// (<see cref="TorchPatch"/>) and reads the settings there, so a change applies within two seconds without a handler.
    /// </summary>
    public static class TorchFeature
    {
        /// <summary>The hover line of a torch this mod put out for the day.</summary>
        public const string NightfallWord = "$ok_torch_nightfall";

        public static void Initialize(SyncedConfiguration synced)
        {
            TorchSettings.Bind(synced);
            Language.Add("ok_torch_nightfall", "Lights at nightfall");
        }
    }
}
