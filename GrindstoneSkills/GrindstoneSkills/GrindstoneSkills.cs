using System.Reflection;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using PatchGuard;
using SyncedConfig;

namespace GrindstoneSkills
{
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    public class GrindstoneSkills : BaseUnityPlugin
    {
        public const string PluginGuid = "com.GrindstoneSkills";
        public const string PluginName = "GrindstoneSkills";
        public const string PluginVersion = "0.13.3";

        public static ManualLogSource Log { get; private set; }
        public static SyncedConfiguration Synced { get; private set; }

        private void Awake()
        {
            Log = Logger;
            Synced = new SyncedConfiguration(this, Logger, PluginName, PluginVersion);
            Settings.Initialize(Synced);

            Harmony harmony = new Harmony(PluginGuid);
            harmony.PatchAll(Assembly.GetExecutingAssembly());
            // The mod's own skills (not in the game's enum): one entry per skill.
            CustomSkills.Initialize(SailingSkill.Skill, ForagingSkill.Skill, HusbandrySkill.Skill, DefenseSkill.Skill);

            // Writes the .cfg, hot reloads it on edit; Charter pushes reloaded values to clients.
            Synced.Finish(harmony);

            // Exceptions thrown by this mod's patches are logged under the GrindstoneSkills log source, then rethrown.
            Guard.Install(harmony, Logger, Assembly.GetExecutingAssembly());
        }
    }
}
