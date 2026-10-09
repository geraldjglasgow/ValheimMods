using BepInEx.Configuration;
using SyncedConfig;

namespace EliteCreaturesPack.Ballista
{
    /// <summary>
    /// Section 28: the Bone Ballista, one switch. It only puts the ballista in the hammer and its Bone Missiles on the
    /// workbench, or takes them out: the prefabs are always registered, so ballistas already built load and still shoot.
    /// Synced and locked like every setting of the mod.
    /// </summary>
    public static class BallistaSettings
    {
        public const string Section = "28 - Bone Ballista";

        private static ConfigEntry<bool> enabled = null!;

        public static bool Enabled => enabled.Value;

        public static void Initialize(SyncedConfiguration config)
        {
            enabled = config.Bind(Section, "Enabled", true,
                "A small ballista of bones, its arms two spines, in the hammer's Misc tab beside the game's ballista "
                + "(3 spines, 25 bone fragments), and its Bone Missiles at the workbench (level 2: 5 bone "
                + "fragments and 2 feathers make 20). Hold it with the use key, aim with the mouse (90 degrees of turn), shoot "
                + "with attack; it reloads from the missiles you carry. Off: neither can be made; ballistas already built stay "
                + "and still shoot.");
        }
    }
}
