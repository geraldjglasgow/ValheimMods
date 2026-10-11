using EliteCreaturesPack.Custom.Files;
using SyncedConfig;

namespace EliteCreaturesPack.Custom
{
    /// <summary>
    /// Custom creatures (features/custom-creatures.md): new creatures and human enemies defined by server admins in YAML,
    /// each a copy of a creature that exists, built into a real prefab on every peer when a world loads. The layout:
    /// <list type="bullet">
    /// <item><c>Definitions/</c>: the definition model, plain data, one block per area of the spec.</item>
    /// <item><c>Files/</c>: the YAML files (YamlConfig), their readers and checks, and the server's built files sent to
    /// players.</item>
    /// <item><c>Build/</c>: when and how the prefabs are built (<c>BuildTiming</c>, <c>BuildPass</c>), the step contract
    /// (<c>ICreatureStep</c>, <c>CreatureBuild</c>, <c>BuildReport</c>, <c>PrefabLookup</c>) and the current world's
    /// creatures (<c>CustomPrefabs</c>).</item>
    /// <item><c>Saves/</c>: the ZDO mark on every custom creature and the parking that keeps removed ones in the save.</item>
    /// <item>One folder per step: <c>Nature/</c> (character, progress, senses, movement, behaviour, taming, sounds),
    /// <c>Combat/</c>, <c>Look/</c>, <c>Humans/</c>, <c>Elite/</c>; and <c>Export/</c> (the admin export command),
    /// <c>Commands/</c> (the <c>ecp</c> console command), <c>Ready/</c> (the default file).</item>
    /// </list>
    /// Everything here works without Elite Creatures Reborn; only the elite step and its notice look for it.
    /// </summary>
    public static class CustomCreatures
    {
        /// <summary>From the plugin's Awake, before <c>Synced.Finish</c>: the file set and the server's article exist before
        /// any file loads or any player connects. The patches come with the plugin's PatchAll.</summary>
        public static void Install(SyncedConfiguration synced)
        {
            CreatureFiles.Register(synced);
            ServerDefinitions.Register(synced.Sync);
        }
    }
}
