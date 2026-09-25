using EliteCrafting.Rules;

namespace EliteCrafting.Items
{
    /// <summary>
    /// Entry point of the Items area, called once from plugin Awake after the rules, settings and words are loaded.
    /// Owns: prefab registration (43 stones incl. 16 essences + 16 reserved + 5 shards, ObjectDB/ZNetScene: <see cref="StoneRegistrationPatches"/>),
    /// economy values pushed into the stone prefabs (<see cref="StoneItemData"/>), their look on clients
    /// (<see cref="StoneVisuals"/>), workbench-upgrade carry-over of ecf_ keys (<see cref="UpgradeCarryOver"/>) and the
    /// load warning for stackable slot items (<see cref="StackableGearWarning"/>) and whole stone stacks on inventory
    /// load (<see cref="StoneStackGuard"/>).
    /// Harmony patches of this area are ordinary [HarmonyPatch] classes in this folder; the plugin's PatchAll finds them.
    /// </summary>
    public static class ItemsFeature
    {
        public static void Init()
        {
            StoneTints.Rebuild(ActiveRules.Current.Economy);
            StonePrefabs.Built += ApplyRules;
            ActiveRules.RulesChanged += ApplyRules;
            // The essence family line and the shard fuse line are localized text in the description (IMP-102).
            Text.Words.Changed += ApplyRules;
        }

        // Every peer: after the prefabs are built, and on every rules change (hot reload, the server's rules arriving).
        private static void ApplyRules() => StoneItemData.ApplyAll(ActiveRules.Current);
    }
}
