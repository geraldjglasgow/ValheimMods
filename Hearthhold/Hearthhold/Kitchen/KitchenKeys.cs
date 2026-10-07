namespace Hearthhold
{
    /// <summary>
    /// The ZDO keys of the kitchen feature. A cooking station's slot keys are followed by the slot number, the way the
    /// game names its own ("slot" + i, "slotstatus" + i).
    /// </summary>
    public static class KitchenKeys
    {
        /// <summary>Float per slot: the Cooking level of whoever put the food on, 0 when unknown.</summary>
        public const string SlotLevel = "hearthhold_slot_level";

        /// <summary>Float per slot: the stars of the raw food put on.</summary>
        public const string SlotInput = "hearthhold_slot_input";

        /// <summary>Int per slot: the stars rolled when the dish turned done.</summary>
        public const string SlotStars = "hearthhold_slot_stars";

        /// <summary>Int on a fermenter: the stars of the mead base in it, which every mead of the batch carries.</summary>
        public const string BaseStars = "hearthhold_base_stars";
    }
}
