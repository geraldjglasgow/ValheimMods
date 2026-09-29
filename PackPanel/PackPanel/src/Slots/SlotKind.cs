using PackPanel.Backpacks;

namespace PackPanel.Slots
{
    /// <summary>
    /// What a slot holds. Head to Utility and <see cref="Trinket"/> are worn (<see cref="SlotRules.IsWorn"/>): an item in such
    /// a slot is the one the player wears. The others are carried: each takes only its kind of item. <see cref="Retired"/> stands for a slot an
    /// older build had (the quick slots) when a layout record is read, so the cells after it keep their places; it takes
    /// nothing, so what sat there moves into the grid. <see cref="Key"/> is a ring cell (<see cref="KeyRing"/>): each takes
    /// one key. <see cref="Tacklebox"/> holds a tacklebox and <see cref="Tackle"/> is one of that box's cells
    /// (<see cref="PackPanel.Tackle.Tacklebox"/>): each takes bait. New kinds go at the end: the layout record stores slot ids
    /// ("key1", "tacklebox1", "tackle1"), not numbers, so an older record still reads, and an older build reads a newer
    /// kind's id as retired.
    /// </summary>
    public enum SlotKind
    {
        Head,
        Chest,
        Legs,
        Back,
        Backpack,
        Utility,
        Food,
        Mead,
        Ammo,
        Purse,
        Retired,
        Key,
        Tacklebox,
        Tackle,
        Trinket,
    }
}
