using System;

namespace OpenKeep.Recipes
{
    /// <summary>The crafting panel's recipe categories, one button each in <see cref="CategoryBar"/>, in this order.</summary>
    [Flags]
    public enum RecipeCategory
    {
        None = 0,
        Ammo = 1,
        Weapons = 2,
        Bows = 4,
        Armour = 8,
        Shields = 16,
        Tools = 32,
        Food = 64,
        Materials = 128,
    }
}
