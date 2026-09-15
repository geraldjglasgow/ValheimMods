using EliteCreaturesReborn.Mutations;
using EliteCreaturesReborn.Traits;

namespace EliteCreaturesReborn.Runtime
{
    /// <summary>
    /// Attaches the per-frame mutation behaviours a creature's traits call for, on EVERY machine. The old build split
    /// these owner-only vs everywhere; the multiplayer rule is instead "decide on the owner, draw on every client", so
    /// each behaviour is attached everywhere and self-gates its authoritative writes on live ownership. That makes
    /// ownership changing hands free: the machine that owns the creature at any moment is the one whose gates open.
    /// The event-shaped mutations (Warding, Plated, Bloated, Splintering, Mad) need no component - patches handle them.
    /// </summary>
    public static class BehaviourInstaller
    {
        public static void Install(EliteController controller)
        {
            CreatureTraits traits = controller.Traits;
            if (traits.Has(Mutation.Cloaked))
            {
                controller.gameObject.AddComponent<CloakBehaviour>();
            }
            if (traits.Has(Mutation.Leeching))
            {
                controller.gameObject.AddComponent<LeechBehaviour>();
            }
            if (traits.Has(Mutation.Miasmic))
            {
                controller.gameObject.AddComponent<MiasmaField>();
                controller.gameObject.AddComponent<MiasmaBody>(); // permanent worn poison visual, every machine
            }
            if (traits.Has(Mutation.Devouring))
            {
                controller.gameObject.AddComponent<DevourBehaviour>();
            }
        }
    }
}
