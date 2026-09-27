using HarmonyLib;

namespace GrindstoneSkills
{
    /// <summary>
    /// The birth scope of Better offspring without Elite Creatures Reborn, on the parent's owner. It is open only inside
    /// one Procreation.Procreate that gives birth (opened by <see cref="OffspringStar.Begin"/> in the prefix, closed in
    /// <see cref="BreedingCheck"/>'s finalizer). While it is open, a Character.SetLevel or ItemDrop.SetQuality called with
    /// exactly the level the game passes on from the parent gets the raised level instead: the newborn's level, or the
    /// laid egg's quality (its chick's level). The egg's own ItemDrop.Awake calls SetQuality with its prefab quality
    /// inside the same Instantiate; whatever that call becomes, the game's explicit call afterwards sets the final value,
    /// and a raised value never matches again, so nothing compounds. Every other SetLevel and SetQuality in the game
    /// passes through untouched at the cost of one bool test.
    /// </summary>
    public static class OffspringLevel
    {
        private static bool open;
        private static int passed;
        private static int raised;

        /// <summary>A value was raised since the scope last opened: the newborn or egg got its star.</summary>
        public static bool Raised { get; private set; }

        public static void Open(int passedLevel, int raisedLevel)
        {
            passed = passedLevel;
            raised = raisedLevel;
            Raised = false;
            open = raisedLevel > passedLevel;
        }

        public static void Close() => open = false;

        [HarmonyPatch(typeof(Character), nameof(Character.SetLevel))]
        private static class Newborn
        {
            [HarmonyPrefix]
            private static void Prefix(ref int level) => Raise(ref level);
        }

        [HarmonyPatch(typeof(ItemDrop), nameof(ItemDrop.SetQuality))]
        private static class Egg
        {
            [HarmonyPrefix]
            private static void Prefix(ref int quality) => Raise(ref quality);
        }

        private static void Raise(ref int value)
        {
            if (!open || value != passed)
                return;
            value = raised;
            Raised = true;
        }
    }
}
