using EliteCreaturesReborn.Aspects;
using EliteCreaturesReborn.Traits;

namespace EliteCreaturesReborn.Runtime
{
    /// <summary>
    /// A boss's counterpart to <see cref="BehaviourInstaller"/>: attaches the per-frame behaviour its aspect calls for on
    /// EVERY machine, each self-gating its writes on live ownership (Phantom's splits among them), and - on the owner, on
    /// the boss's first roll only - brings in its twin. A copy is born already resolved, so it never reaches that branch,
    /// and returns before the behaviours, so it never brings a twin or splits of its own. A Phantom copy is hollowed here
    /// on every machine the moment it resolves.
    /// The hit-shaped aspects (Reflective, Shielded, Elementalist, Enraged) need no component - patches handle them.
    /// Adaptive and Fixated have both: a component for their state and look, and a hook in <see cref="Scaling.AspectDamage"/>.
    /// </summary>
    public static class AspectInstaller
    {
        public static void Install(EliteController controller)
        {
            CreatureTraits traits = controller.Traits;
            if (traits.PhantomCopy)
            {
                PhantomBody.Hollow(controller.Creature);
                return;
            }
            Attach(controller, traits.Aspect);
            if (controller.FreshlyResolved && controller.IsOwner())
            {
                Arrive(controller, traits.Aspect);
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
            }
        }

        private static void Arrive(EliteController controller, Aspect aspect)
        {
            if (aspect == Aspect.Twin)
            {
                TwinSpawner.Spawn(controller);
            }
        }
    }
}
