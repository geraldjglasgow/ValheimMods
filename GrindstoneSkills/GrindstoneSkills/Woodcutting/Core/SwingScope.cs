using HarmonyLib;

namespace GrindstoneSkills
{
    /// <summary>
    /// The local player's melee swing that is running right now. Attack.DoMeleeAttack (every axe and battleaxe attack
    /// is Horizontal or Vertical, so melee) runs on the attacker's client: it sends each hit, drains the weapon's
    /// durability once, then raises each skill it used once (Woodcutting when it hit any wood). A prefix opens the scope
    /// for the local player, <see cref="WoodHit"/> records the wood each hit landed on, and a finalizer closes it.
    /// <para>The Woodcutting raise inside the scope is the "this swing hit wood" moment: its factor is scaled by
    /// <see cref="WoodXp.SwingScale"/> and the swing perks run (<see cref="SwingPerks.OnSwingHitWood"/>).</para>
    /// </summary>
    public static class SwingScope
    {
        public static bool Active { get; private set; }

        /// <summary>The running attack; null outside a scope.</summary>
        public static Attack Attack { get; private set; }

        /// <summary>The hardest wood this swing hit so far (<see cref="WoodTarget.HarderThan"/>); null when none.</summary>
        public static WoodTarget Hardest { get; private set; }

        [HarmonyPatch(typeof(Attack), nameof(Attack.DoMeleeAttack))]
        private static class Melee
        {
            [HarmonyPrefix]
            private static void Prefix(Attack __instance, out bool __state) => __state = Begin(__instance);

            [HarmonyFinalizer]
            private static void Finalizer(bool __state) => End(__state);
        }

        [HarmonyPatch(typeof(Skills), nameof(Skills.RaiseSkill))]
        private static class Raise
        {
            [HarmonyPrefix]
            private static void Prefix(Skills __instance, Skills.SkillType skillType, ref float factor)
            {
                Player player = Player.m_localPlayer;
                if (skillType != WoodSkill.Skill || !Active || !WoodSkill.Active || player == null || __instance.m_player != player)
                    return;
                WoodTarget hardest = Hardest;
                Attack attack = Attack;
                factor *= WoodGuard.Run("swing experience", () => WoodXp.SwingScale(hardest), 1f);
                WoodGuard.Run("swing perks", () => SwingPerks.OnSwingHitWood(attack));
            }
        }

        /// <summary>
        /// Raises the local player's Woodcutting by an amount that is already final, bypassing an open scope. Credits
        /// can arrive inside a swing: when the swinging client owns the tree, the fell and the routed credit run
        /// synchronously within Attack.DoMeleeAttack, and would otherwise be scaled again and run the swing perks.
        /// </summary>
        public static void RaiseUnscoped(Player player, float amount)
        {
            if (player == null || amount <= 0f)
                return;
            bool wasActive = Active;
            Active = false;
            try
            {
                player.RaiseSkill(WoodSkill.Skill, amount);
            }
            finally
            {
                Active = wasActive;
            }
        }

        /// <summary>Notes a wood target a hit of this swing landed on; ignored outside a scope.</summary>
        public static void Record(UnityEngine.Component target)
        {
            if (!Active)
                return;
            WoodTarget wood = WoodTarget.Of(target);
            if (wood != null && wood.HarderThan(Hardest))
                Hardest = wood;
        }

        private static bool Begin(Attack attack)
        {
            if (Active || attack == null || attack.m_character == null || attack.m_character != Player.m_localPlayer)
                return false;
            Active = true;
            Attack = attack;
            Hardest = null;
            return true;
        }

        private static void End(bool opened)
        {
            if (!opened)
                return;
            Active = false;
            Attack = null;
            Hardest = null;
        }
    }
}
