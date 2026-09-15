using HarmonyLib;

namespace OpenKeep.Stacks
{
    /// <summary>
    /// Applies the stack and weight values as soon as the item database exists: ObjectDB.Awake in the game scene,
    /// ObjectDB.CopyOtherDB at the main menu. Low priority so items other mods add in their own postfixes are seen.
    /// The documentation is written once the scene is there as well.
    /// </summary>
    public static class DatabaseReady
    {
        public static void OnDatabase()
        {
            StackValues.ApplyAll();
            Documentation.WriteIfReady();
        }

        [HarmonyPatch(typeof(ObjectDB), nameof(ObjectDB.Awake))]
        private static class AwakePatch
        {
            [HarmonyPostfix]
            [HarmonyPriority(Priority.Low)]
            private static void Postfix() => OnDatabase();
        }

        [HarmonyPatch(typeof(ObjectDB), nameof(ObjectDB.CopyOtherDB))]
        private static class CopyPatch
        {
            [HarmonyPostfix]
            [HarmonyPriority(Priority.Low)]
            private static void Postfix() => OnDatabase();
        }
    }
}
