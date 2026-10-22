using SyncedConfig;

namespace EliteEquipment.Casters
{
    /// <summary>
    /// Entry point of Casters (section 4, features/casters.md): binds the switch (the words are in
    /// translations/English.yml, the robes' boots join EliteEquipment's from <see cref="CasterBoots"/>), and switches the
    /// recipes and Eikthyr's spell drop on or off when the switch changes (a server's value arriving included).
    /// Every patch is a Harmony attribute class.
    /// </summary>
    public static class CastersModule
    {
        public static void Initialize(SyncedConfiguration synced)
        {
            CastersSettings.Initialize(synced);
            CastersSettings.Enabled.SettingChanged += (_, _) => CasterSafe.Run("casters switch", Switched);
        }

        private static void Switched()
        {
            CasterRecipes.Refresh();
            EikthyrDrop.Refresh(ZNetScene.instance);
        }
    }
}
