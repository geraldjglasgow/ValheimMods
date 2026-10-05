using static EliteCrafting.Effects.EffectParamKind;
using static EliteCrafting.Effects.EffectPolarity;
using static EliteCrafting.Effects.EffectRoute;
using static EliteCrafting.Effects.EffectScope;

namespace EliteCrafting.Effects
{
    /// <summary>
    /// Phase 3, throwing (features/effects-phase3.md, "Throwing"): a one-handed weapon's secondary attack thrown like a
    /// spear (<see cref="ThrowSwing"/>, <see cref="ThrownWeapon"/>), and what a thrown weapon - a spear or such a
    /// weapon - does when it comes down (<see cref="ThrownLanding"/>: <see cref="Recall"/>, <see cref="Apportation"/>).
    /// </summary>
    internal static partial class EffectCatalog
    {
        static partial void RegisterPhase3Throwing()
        {
            Add("throwable", Hook, ItemLocal, ValueTypes.Flag, None, Raise, null, M, "per-swing clone: this one-handed weapon's secondary attack is the spear throw");
            Add("recall", Hook, ItemLocal, ValueTypes.Flag, None, Raise, null, M, "prefix Projectile.SpawnOnHit: the thrown weapon goes back to the thrower's inventory and hand");
            Add("apportation", Hook, ItemLocal, ValueTypes.Flag, None, Raise, null, M, "prefix Projectile.SpawnOnHit: a thrown hit on a creature moves the thrower beside it");
        }
    }
}
