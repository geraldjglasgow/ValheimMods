using HarmonyLib;

namespace GrindstoneSkills
{
    /// <summary>
    /// A tamed animal dying, on the creature's ZDO owner. Character.OnDeath does the whole death there: it sets up the
    /// ragdoll, whose Setup generates the drop list and stores it on the ragdoll's ZDO (the ragdoll spawns it a few
    /// seconds later), then calls the death callbacks, where CharacterDrop.OnDeath generates and spawns the drops at
    /// once for a creature without a dropping ragdoll. A prefix opens a <see cref="ButcherContext"/> for the whole call
    /// while Husbandry is on, so both drop paths see it; a finalizer puts back the one before (none). The killer is the
    /// attacker of the game's last hit, which RPC_Damage records on the owner for every hit that lands on a living
    /// creature, the killing one included. A player killer gets the butchering experience by
    /// <see cref="HusbandryCredit"/>, since only the killer's client can raise the killer's skill.
    /// Non-owners also run the start of OnDeath (a death animation's event); they never open a context.
    /// </summary>
    public static class Butchering
    {
        /// <summary>The death being handled right now; null outside Character.OnDeath of a tamed animal on its owner.</summary>
        public static ButcherContext Open { get; private set; }

        [HarmonyPatch(typeof(Character), nameof(Character.OnDeath))]
        private static class Death
        {
            [HarmonyPrefix]
            private static void Prefix(Character __instance, out ButcherContext __state)
            {
                __state = Open;
                Open = HusbandrySkill.Active ? HookGuard.Run("butchering", () => Begin(__instance), null) : null;
            }

            [HarmonyFinalizer]
            private static void Finalizer(ButcherContext __state) => Open = __state;
        }

        private static ButcherContext Begin(Character creature)
        {
            if (!IsOwnedTame(creature))
                return null;
            Player killer = creature.m_lastHit != null ? creature.m_lastHit.GetAttacker() as Player : null;
            int level = creature.GetLevel();
            ButcherContext context = new ButcherContext
            {
                Creature = creature,
                Prefab = Herd.PrefabName(creature),
                Level = level,
                Killer = killer,
                KillerLevel = killer != null ? HusbandrySkill.Of(killer) : 0f,
                PrimeStars = PrimeCutDrops.StarsFor(level),
            };
            if (killer != null)
                HusbandryCredit.Send(killer.GetPlayerID(), HusbandryCredit.Kind.Butchering, context.Prefab);
            return context;
        }

        private static bool IsOwnedTame(Character creature) =>
            creature != null && !creature.IsPlayer() && creature.m_nview != null && creature.m_nview.IsValid()
            && creature.m_nview.IsOwner() && creature.IsTamed();
    }
}
