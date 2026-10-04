using EliteCreaturesReborn.Aspects;
using EliteCreaturesReborn.Traits;

namespace EliteCreaturesReborn.Runtime
{
    /// <summary>
    /// A boss's counterpart to <see cref="BehaviourInstaller"/>: attaches the per-frame behaviour each of its aspects calls for on
    /// EVERY machine, each self-gating its writes on live ownership (Phantom's splits among them), and - on the owner, on
    /// the boss's first roll only - brings in its twin or tethered partner. A copy is born already resolved, so it never
    /// reaches that branch, and never brings a twin, a partner or splits of its own. A Phantom copy is hollowed here on every machine the
    /// moment it resolves, then takes the behaviours of the other aspects it carries (a Bountiful boss's, see
    /// <see cref="Aspects.PhantomSpawner"/>).
    /// The hit-shaped aspects (Reflective, Shielded, Elementalist, Enraged) need no component - patches handle them.
    /// Adaptive, Fixated and Tethered have both: a component for their state and look, and a hook in
    /// <see cref="Scaling.AspectDamage"/>.
    /// </summary>
    public static class AspectInstaller
    {
        public static void Install(EliteController controller)
        {
            CreatureTraits traits = controller.Traits;
            if (traits.PhantomCopy)
            {
                PhantomBody.Hollow(controller.Creature);
            }
            foreach (Aspect aspect in traits.Aspects())
            {
                if (traits.PhantomCopy && aspect == Aspect.Phantom)
                {
                    continue; // a copy wears a Bountiful boss's other aspects, never Phantom's own splits
                }
                Attach(controller, aspect);
                if (controller.FreshlyResolved && controller.IsOwner())
                {
                    Arrive(controller, aspect);
                }
            }
        }

        private static void Attach(EliteController controller, Aspect aspect)
        {
            switch (aspect)
            {
                case Aspect.Mending: controller.gameObject.AddComponent<MendingBehaviour>(); break;
                case Aspect.Summoner: controller.gameObject.AddComponent<SummonerBehaviour>(); break;
                case Aspect.Twin: controller.gameObject.AddComponent<TwinLink>(); break;
                case Aspect.Phantom: controller.gameObject.AddComponent<PhantomBehaviour>(); break;
                case Aspect.Adaptive: controller.gameObject.AddComponent<AdaptiveBehaviour>(); break;
                case Aspect.Fixated: controller.gameObject.AddComponent<FixatedBehaviour>(); break;
                case Aspect.Stormbound: controller.gameObject.AddComponent<StormboundBehaviour>(); break;
                case Aspect.Gravitic: controller.gameObject.AddComponent<GraviticBehaviour>(); break;
                case Aspect.Colossal: controller.gameObject.AddComponent<ColossalBehaviour>(); break;
                case Aspect.Tethered: controller.gameObject.AddComponent<TetherLink>(); break;
                case Aspect.Portalbound: controller.gameObject.AddComponent<PortalboundBehaviour>(); break;
            }
        }

        private static void Arrive(EliteController controller, Aspect aspect)
        {
            if (aspect == Aspect.Twin)
            {
                TwinSpawner.Spawn(controller);
            }
            else if (aspect == Aspect.Tethered)
            {
                TetherSpawner.Spawn(controller);
            }
        }
    }
}
