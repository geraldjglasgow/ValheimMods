using SyncedConfig;

namespace OpenKeep.Homestead
{
    /// <summary>
    /// Pets eat from chests: binds the keys of section 8. Eating runs in the creature's own AI update
    /// (<see cref="PetEating"/>) and reads the settings there, so a change applies at the next search without a handler;
    /// the feature shows no words.
    /// </summary>
    public static class PetFeature
    {
        public static void Initialize(SyncedConfiguration synced)
        {
            PetSettings.Bind(synced);
        }
    }
}
