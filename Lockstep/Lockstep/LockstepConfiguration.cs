using System.Reflection;
using BepInEx.Configuration;
using PatchGuard;
using SyncedConfig;
using YamlConfig;

namespace Lockstep
{
    public enum LateJoinerMode
    {
        /// <summary>A new player is credited with every stage the world has already cleared.</summary>
        Catchup,
        /// <summary>A new player must fight every boss.</summary>
        Earn,
    }

    public static class LockstepConfiguration
    {
        public static ConfigEntry<bool> LockConfiguration { get; private set; }

        /// <summary>Players within this distance of a boss when it dies are credited even without landing a hit. 0 disables.</summary>
        public static ConfigEntry<float> CreditRadius { get; private set; }

        /// <summary>Credit every online player when a boss dies, regardless of distance.</summary>
        public static ConfigEntry<bool> CreditEveryoneOnline { get; private set; }

        /// <summary>Players who have not logged in for this many days no longer hold the group back. 0 disables.</summary>
        public static ConfigEntry<int> InactiveDays { get; private set; }

        /// <summary>Count only players who are online right now.</summary>
        public static ConfigEntry<bool> CountOnlyOnline { get; private set; }

        public static ConfigEntry<LateJoinerMode> LateJoiners { get; private set; }

        /// <summary>Block the boss spawn itself when the stage is closed, not only the altar interaction.</summary>
        public static ConfigEntry<bool> SpawnGuard { get; private set; }

        /// <summary>Name the missing players in the altar message instead of saying "the group".</summary>
        public static ConfigEntry<bool> NameMissingPlayers { get; private set; }

        public static YamlFileSet ChainSource { get; private set; }

        public static void Initialize(SyncedConfiguration config)
        {
            BindGeneral(config);
            BindCredit(config);
            BindGroup(config);
            BindGate(config);
            AddChain(config);
            RepublishOnGroupChange(config);
        }

        private static void BindGeneral(SyncedConfiguration config)
        {
            LockConfiguration = config.BindLocking("General", "Lock Configuration", true,
                "Server only. When on, every player uses the server's values for this file and cannot override them locally.");
        }

        private static void BindCredit(SyncedConfiguration config)
        {
            CreditRadius = config.Bind("Credit", "Credit Radius", 200f,
                "Players within this many meters of a boss when it dies are credited with defeating it even if they never hit it. Players who hit the boss are always credited. 0 disables the radius rule.");
            CreditEveryoneOnline = config.Bind("Credit", "Credit Everyone Online", false,
                "Credit every online player when a boss dies, regardless of distance. For groups who trust each other.");
        }

        private static void BindGroup(SyncedConfiguration config)
        {
            InactiveDays = config.Bind("Group", "Inactive Days", 14,
                "Players who have not logged in for this many days are not counted when checking whether the whole group has defeated a boss. 0 disables the timeout.");
            CountOnlyOnline = config.Bind("Group", "Count Only Online", false,
                "Only players who are online right now must have defeated the previous boss. Off means every known player counts, minus ignored and inactive ones.");
            LateJoiners = config.Bind("Group", "Late Joiners", LateJoinerMode.Catchup,
                "Catchup: a player joining for the first time is credited with every boss the world has already defeated. Earn: they must fight every boss.");
        }

        private static void BindGate(SyncedConfiguration config)
        {
            SpawnGuard = config.Bind("Gate", "Spawn Guard", true,
                "Also block the boss spawn itself when the stage is closed, in case a client with stale state or another mod uses the altar.");
            NameMissingPlayers = config.Bind("Gate", "Name Missing Players", true,
                "The altar message names the players who still need to defeat the previous boss. Off says 'the group' instead.");
        }

        private static void AddChain(SyncedConfiguration config)
        {
            ChainSource = config.AddYaml(new YamlFileSet("LockstepChain*.yml", "lockstepchain",
                () => new ChainDocument(), model => Chain.Set(((ChainDocument)model).Process()))
            {
                DefaultContent = SyncedConfiguration.EmbeddedResource(Assembly.GetExecutingAssembly(), "Lockstep.LockstepChain.yml"),
                EditorLabel = () => "Edit progression chain",
            });
        }

        /// <summary>The Group section changes who counts, so the published state must be recomputed.</summary>
        private static void RepublishOnGroupChange(SyncedConfiguration config)
        {
            config.Config.SettingChanged += (_, args) => Guard.Run("republish after config change", () =>
            {
                if (args.ChangedSetting.Definition.Section == "Group")
                    ProgressServer.Publish();
            });
        }
    }
}
