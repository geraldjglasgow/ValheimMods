using EliteCreaturesReborn.Aspects;
using EliteCreaturesReborn.Traits;

namespace EliteCreaturesReborn.Runtime
{
    /// <summary>
    /// A boss's counterpart to <see cref="BehaviourInstaller"/>: attaches the per-frame behaviour its aspect calls for on
    /// EVERY machine, each self-gating its writes on live ownership, and - on the owner, on the boss's first roll only -
    /// brings in its twin or its phantom copies. A copy is born already resolved, so it never reaches that branch and
    /// never brings a twin or copies of its own. A Phantom copy is hollowed here on every machine the moment it resolves.
    /// The hit-shaped aspects (Reflective, Shielded, Elementalist, Enraged) need no component - patches handle them.
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
            }
        }

        private static void Arrive(EliteController controller, Aspect aspect)
        {
            if (aspect == Aspect.Twin)
            {
                TwinSpawner.Spawn(controller);
            }
            else if (aspect == Aspect.Phantom)
            {
                PhantomSpawner.Spawn(controller);
            }
        }
    }
}
