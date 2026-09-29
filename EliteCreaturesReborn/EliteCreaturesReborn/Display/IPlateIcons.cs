using UnityEngine;

namespace EliteCreaturesReborn.Display
{
    /// <summary>
    /// A row of icons a mutation draws on a creature's nameplate - <see cref="PouchIcons"/> for Thieving,
    /// <see cref="MealIcons"/> for Devouring - so the HUD patch adds either the same way, once per nameplate.
    /// </summary>
    internal interface IPlateIcons
    {
        /// <summary>Binds the row to its creature and the nameplate's health bar, right after it is added.</summary>
        void Init(Character character, RectTransform healthBar);
    }
}
