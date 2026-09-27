using OpenKeep.Core;
using SyncedConfig;

namespace OpenKeep.Homestead
{
    /// <summary>
    /// Torches lit only at night: binds the keys of section 8 and the words. The switch runs in the fire's own tick
    /// (<see cref="TorchPatch"/>) and reads the settings there, so a change applies within two seconds without a handler;
    /// the keep-lit key is polled in <see cref="TorchKeyPatch"/> and handled by the fire's owner (<see cref="TorchKeep"/>).
    /// </summary>
    public static class TorchFeature
    {
        /// <summary>The hover line of a torch this mod put out for the day.</summary>
        public const string NightfallWord = "$ok_torch_nightfall";

        /// <summary>The hover actions of Torch Switch Key: on a scheduled torch, and on a torch kept lit.</summary>
        public const string KeepLitWord = "$ok_torch_keeplit";
        public const string NightOnlyWord = "$ok_torch_nightonly";

        /// <summary>The centre message after the key: kept lit, or back on the schedule.</summary>
        public const string KeptWord = "$ok_torch_kept";
        public const string ScheduledWord = "$ok_torch_scheduled";

        public static void Initialize(SyncedConfiguration synced)
        {
            TorchSettings.Bind(synced);
            Language.Add("ok_torch_nightfall", "Lights at nightfall");
            Language.Add("ok_torch_keeplit", "Keep lit");
            Language.Add("ok_torch_nightonly", "Light at night only");
            Language.Add("ok_torch_kept", "kept lit day and night");
            Language.Add("ok_torch_scheduled", "lit at night only");
        }
    }
}
