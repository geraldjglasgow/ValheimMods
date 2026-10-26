using EliteCreaturesReborn.Loot;
using HarmonyLib;

namespace EliteCreaturesReborn.Patches
{
    /// <summary>
    /// The owner, as the game sets up a dying creature's corpse: the level other mods' drops read is kept on it (see
    /// <see cref="LootLevel"/>). Runs inside the game's death, so a failure is reported and swallowed.
    /// </summary>
    [HarmonyPatch(typeof(Ragdoll), "Setup")]
    public static class LootLevelCorpsePatch
    {
        private static void Postfix(Ragdoll __instance, CharacterDrop characterDrop) =>
            SafeCall.Run("Ragdoll.Setup (loot level)", static (corpse, drop) => LootLevel.Mark(corpse, drop), __instance,
                characterDrop);
    }

    /// <summary>
    /// The corpse's owner, as it spawns its loot: the kept level becomes its game level before every other mod's postfix
    /// reads it. A postfix, so a prefix reading the level (another mod's own star odds) sees the game's, as it does for a
    /// creature that drops at once.
    /// </summary>
    [HarmonyPatch(typeof(Ragdoll), "SpawnLoot")]
    public static class LootLevelCorpseLootPatch
    {
        [HarmonyPriority(Priority.First)]
        private static void Postfix(Ragdoll __instance) =>
            SafeCall.Run("Ragdoll.SpawnLoot (loot level)", static corpse => LootLevel.Raise(corpse), __instance);
    }

    /// <summary>
    /// The owner, after a creature with no corpse loot has dropped its loot at once: its level raised before every other
    /// mod's postfix, and the game's put back after them all (the finalizer runs last, also when one threw).
    /// </summary>
    [HarmonyPatch(typeof(CharacterDrop), "OnDeath")]
    public static class LootLevelDeathPatch
    {
        [HarmonyPriority(Priority.First)]
        private static void Postfix(CharacterDrop __instance) =>
            SafeCall.Run("CharacterDrop.OnDeath (loot level)", static drop => LootLevel.Show(drop), __instance);

        private static void Finalizer() => LootLevel.Hide();
    }
}
